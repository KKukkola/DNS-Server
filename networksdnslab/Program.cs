using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Net.Sockets;
using System.Net;
using System.Threading;
using System.Collections;

namespace networksdnslab
{
    class Program
    {
        static Random random = new Random();

        static string root = "198.41.0.4";
        static int NSPort = 33031;
        static bool EnableClient = true; // client only works if port is 33031

        static bool isRecursive = true;
        static bool verbose = false;

        static void Main(string[] args)
        {
            // Startup
            Console.WriteLine("** Enter Server Startup Information");
            Console.Write("** Specify root: ");
            root = Console.ReadLine();
            Console.Write("** Specify port: ");
            NSPort = Convert.ToInt32(Console.ReadLine());

            Console.WriteLine("\n** Server Started, root = {0}, listening on port: {1}", root, NSPort);
            Console.WriteLine("isRecursive = {0}\nverbose = {1}\n", isRecursive, verbose);

            // Create and run a client
            if (EnableClient)
            {
                using (var process1 = new Process())
                {
                    process1.StartInfo.FileName = @"..\..\..\client_tester\bin\Debug\client_tester.exe";
                    process1.Start();
                }
            }

            // Listen for tcp connections
            try
            {
                var listener = new TcpListener(IPAddress.Parse("127.0.0.1"), NSPort);
                listener.Start();
                while (true)
                {
                    TcpClient client = listener.AcceptTcpClient();
                    var childThread = new Thread(() => { ServiceClient(client); });
                    childThread.Start();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Server Exception: " + e.Message);
            }

        }

        public static void ServiceClient(TcpClient client)
        {
            NetworkStream ns = client.GetStream();

            if (client.ReceiveBufferSize <= 0)
            {
                Console.WriteLine("client.ReceiveBufferSize was not > 0");
                return;
            }

            // Read from Client
            Byte[] bytes = new byte[client.ReceiveBufferSize];
            int bytesRead = ns.Read(bytes, 0, client.ReceiveBufferSize);
            string msg = Encoding.ASCII.GetString(bytes, 0, bytesRead);
            Console.WriteLine("***************************************************\n**Got Message: " + msg);

            string[] cmds = msg.Split(' ');
            switch (cmds.First())
            {
                case "root":
                    Client_Root(ns, cmds);
                    break;
                case "dump":
                    Client_Dump(ns, cmds);
                    break;
                case "verbose":
                    Client_Verbose(ns, cmds);
                    break;
                case "normal":
                    Client_Normal(ns, cmds);
                    break;
                case "recursive":
                    Client_Recursive(ns, cmds);
                    break;
                case "nonrecursive":
                    Client_NonRecursive(ns, cmds);
                    break;
                default:
                    Client_ResolveQuery(ns, msg);
                    break;
            }

            ns.Close();
            client.Close();
            Console.WriteLine("**Connection Closed!");
        }

        // Change the program's root
        public static void Client_Root(NetworkStream ns, string[] cmds)
        {
            if (cmds.Length != 2)
            {
                TcpMsg(ns, "> root <new root> expected\n");
            }
            else
            {
                root = cmds[1];
                DNS_Data.ClearCache();
                Console.WriteLine("> cache cleared, root changed to: " + root);
                TcpMsg(ns, "> Root successfully changed to: " + root + " (cache data cleared)\n");
            }
        }

        // Dump the contents of cached data to the client
        public static void Client_Dump(NetworkStream ns, string[] cmds)
        {
            TcpMsg(ns, "> dump:\n");
            TcpMsg(ns, "> stored_answers (resolved names):\n");
            foreach (DictionaryEntry entry in DNS_Data.stored_answers)
            {
                string retString = (string)entry.Key + "\n";
                foreach (string addr in (List<string>)entry.Value)
                    retString += "\t" + addr + "\n";
                TcpMsg(ns, retString);
            }
            TcpMsg(ns, "> stored_ns (other resolvers):\n");
            foreach (DictionaryEntry entry in DNS_Data.stored_ns)
            {
                string retString = (string)entry.Key + "\n";
                foreach (string addr in (List<string>)entry.Value)
                    retString += "\t" + addr + "\n";
                TcpMsg(ns, retString);
            }
        }

        // Switch to recursive mode
        public static void Client_Recursive(NetworkStream ns, string [] cmds)
        {
            isRecursive = true;
            Console.WriteLine("**Recursion Enabled");
            TcpMsg(ns, "> RecursionDesired = true\n");
        }

        // Switch to NonRecursive mode
        public static void Client_NonRecursive(NetworkStream ns, string[] cmds)
        {
            isRecursive = false;
            Console.WriteLine("**Recursion Disabled");
            TcpMsg(ns, "> RecursionDesired = false\n");
        }

        // Switch to verbose mode
        public static void Client_Verbose(NetworkStream ns, string[] cmds)
        {
            verbose = true;
            Console.WriteLine("**Verbose Enabled");
            TcpMsg(ns, "> verbose = true\n");
        }

        // Switch to normal mode
        public static void Client_Normal(NetworkStream ns, string[] cmds)
        {
            verbose = false;
            Console.WriteLine("**Verbose Disabled");
            TcpMsg(ns, "> verbose = false\n");
        }

        // Resolve a client's request
        public static void Client_ResolveQuery(NetworkStream ns, string qname)
        {
            List<string> foundAliases = new List<string>();
            bool stillQuerying = true;
            do
            {
                // 1. See if the answer is in local information
                // If so, return it to the client
                List<string> storedAnswers = DNS_Data.HasAnswer(qname);
                if (storedAnswers != null)
                {
                    Console.WriteLine("Answer for {0} is found in the cache!: {1}\n", qname, storedAnswers.Count);

                    if (verbose)
                        TcpMsg(ns, String.Format("FOUND IN CACHE: {0} = {1}\n", qname, storedAnswers[0]));

                    IPAddress ip;
                    if (IPAddress.TryParse(storedAnswers[0], out ip) == false)
                    { // It is a CNAME, log it and try again with it
                        foundAliases.Add(qname);
                        qname = storedAnswers[0];
                    }
                    else // It is an address, we are done
                    {
                        TcpMsg(ns, String.Format("{0,-8} {1}\n\n", "Name:", qname));
                        TcpMsg(ns, String.Format("{0,-10}", "Addr:"));
                        foreach (string addr in storedAnswers)
                            TcpMsg(ns, String.Format(addr + "\n{0,-10}", ""));
                        TcpMsg(ns, "\n");
                        if (foundAliases.Count > 0)
                        {
                            TcpMsg(ns, String.Format("{0,-10}", "Aliases:"));
                            foreach (string alias in foundAliases)
                                TcpMsg(ns, String.Format(alias + "\n{0,-10}", ""));
                            TcpMsg(ns, "\n");
                        }

                        stillQuerying = false;
                        break;
                    }
                }
                else
                {
                    ushort requestID = (ushort)random.Next();
                    int flags = (isRecursive ? 0x0100 : 0x0000);
                    DNS_Header header = new DNS_Header(requestID, (ushort)flags, 0x0001, 0x0000, 0x0000, 0x0000);
                    DNS_Question question = new DNS_Question(qname, 0x0001, 0x0001);

                    bool foundCname = false;

                    do
                    {
                        // 2. Find the best servers to ask
                        List<string> newRoots = DNS_Data.BetterRoot(qname);
                        if (newRoots == null)
                        {
                            newRoots = new List<string>();
                            newRoots.Add(root);
                        }
                        
                        // 3. Send the servers queries until one returns a response
                        DNS_Response response = null;
                        for (int i = 0; i < newRoots.Count; i++)
                        {
                            if (response != null && response.Answers.Count > 0)
                                break;
                            if (!stillQuerying)
                                break;

                            Console.WriteLine("**Attempting Query At.. {0} For.. {1}", newRoots[i], qname);
                            if (verbose)
                                TcpMsg(ns, String.Format("QUERYING {0} FOR {1}\n", newRoots[i], qname));
                            response = new DNS_Response(SendQuery(newRoots[i], header, question)); //RecursiveLookup(newRoots[i], header, question);
                            if (response == null)
                            {
                                TcpMsg(ns, String.Format("Failed: UDP.Recieve Timeout"));
                                return;
                            }
                            Console.WriteLine(response);

                            // 4. Analyze the response.

                            int replyCode = response.Header.RCODE;
                            if (replyCode == 3)
                            {
                                TcpMsg(ns, "Failed: No such name\n");
                                return;
                            } else if (replyCode == 2)
                            {
                                TcpMsg(ns, "Failed: Server failure\n");
                                return;
                            } else if (replyCode != 0)
                            {
                                TcpMsg(ns, String.Format("Failed: err code: {0}", replyCode));
                                return;
                            }
                            // a. If question is answered or contains a name error
                            // cache the data and return it back to the client
                            if (response.Answers.Count > 0 || response.Header.RCODE == 3)
                            {
                                // c. If the response shows a CNAME which is not the answer
                                // itself, cache it, change SNAME to it and go to step 1
                                foreach (DNS_RR rr in response.Answers)
                                {
                                    if (rr.Type == 5)
                                    {
                                        foundCname = true;
                                        foundAliases.Add(qname);
                                        DNS_Data.AddAnswer(rr.Name, rr.Rdata);
                                        qname = rr.Rdata;
                                        break;
                                    }
                                }

                                if (!foundCname)
                                {
                                    foreach (DNS_RR rr in response.Answers) // cache the data
                                        DNS_Data.AddAnswer(rr.Name, rr.Rdata);

                                    // Return it to the client in a formatted manner
                                    TcpMsg(ns, String.Format("{0,-8} {1}\n\n", "Name:", response.Question.Name));
                                    TcpMsg(ns, String.Format("{0,-10}", "Addr:"));
                                    foreach (DNS_RR rr in response.Answers) // return it back to the client
                                        TcpMsg(ns, String.Format(rr.Rdata + "\n{0,-10}", ""));
                                    TcpMsg(ns, "\n");
                                    if (foundAliases.Count > 0)
                                    {
                                        TcpMsg(ns, String.Format("{0,-10}","Aliases:"));
                                        foreach (string alias in foundAliases)
                                            TcpMsg(ns, String.Format(alias + "\n{0,-10}", ""));
                                        TcpMsg(ns, "\n");
                                    }

                                    stillQuerying = false;
                                    break;
                                }
                            }

                            if (foundCname) break;

                            // b. If response has a better delegation to other servers,
                            // cache the information and go to step 2.
                            if (response.Authoritys.Count > 0)
                            {
                                foreach (DNS_RR auth_rr in response.Authoritys) // cache the better delegation
                                {
                                    if (auth_rr.Type == 6)
                                    {
                                        TcpMsg(ns, "Failed: Encountered start of SOA\n");
                                        return;
                                    }
                                    DNS_Data.AddAuthority(auth_rr.Name, auth_rr.Rdata);
                                }
                                break; // go to step two
                            }

                            // d. If server failure or bizarre content, delete from SLIST 
                            // and go back to step 3.
                            // auto
                        }
                    } while (stillQuerying && !foundCname); // goes back to step two
                }
            } while (stillQuerying); // goes back to step one

        }

        // Send a Header+Question to a server
        public static byte[] SendQuery(string root, DNS_Header header, DNS_Question question)
        {
            // Connect
            UdpClient udpClient = new UdpClient();
            udpClient.Client.ReceiveTimeout = 3000; // 3 sec timeout

            IPAddress ipaddress = null;
            try {
                IPAddress[] addresses = Dns.GetHostAddresses(root);
                ipaddress = addresses.First(a => a.AddressFamily == AddressFamily.InterNetwork);
            } catch (Exception e)
            {
                IPHostEntry ipHostEntry = Dns.GetHostEntry(root); // in order to use either ip or domain names
                ipaddress = ipHostEntry.AddressList.First(a => a.AddressFamily == AddressFamily.InterNetwork);
            }
            IPEndPoint ep = new IPEndPoint(ipaddress, 53);
            udpClient.Connect(ep);

            // Send + Send again on failure
            byte[] dnsQueryString = header.toBytes().Concat(question.toBytes()).ToArray();
            udpClient.Send(dnsQueryString, dnsQueryString.Length);
            try
            {
                byte[] receivedData = udpClient.Receive(ref ep);
                return receivedData;
            } catch (Exception e)
            {
                // timeout occurred, try send again
                udpClient.Send(dnsQueryString, dnsQueryString.Length);
                try
                {
                    byte[] receivedData = udpClient.Receive(ref ep);
                    return receivedData;
                } catch (Exception exc)
                {
                    // failed again, return null
                    return null;
                }

            }

        }

        // Print each byte of a byte array
        public static void printBytes(byte[] arr)
        {
            foreach (byte item in arr)
            {
                Console.Write(item.ToString("x2"));
                Console.Write(" ");
            }
            Console.WriteLine("");
        }

        // Encode and send a message on a network stream
        public static void TcpMsg(NetworkStream ns, string msg)
        {
            Byte[] message = Encoding.ASCII.GetBytes(msg);
            ns.Write(message, 0, message.Length);
        }
    }
}
