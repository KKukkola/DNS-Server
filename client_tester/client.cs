using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace client_tester
{
    class client
    {
        static void Main(string[] args)
        {
            Console.WriteLine("**client started");

            string s = "";

            while ( (s = Console.ReadLine()) != "exit" )
            {
                // Send to the server

                using (var client = new TcpClient())
                {
                    client.Connect("localhost", 33031);
                    NetworkStream ns = client.GetStream();
                    var bytes = Encoding.ASCII.GetBytes(s);
                    ns.Write(bytes, 0, bytes.Length);
                    ns.ReadTimeout = -1;

                    // Read from the server 

                    while (true)
                    {
                        Byte[] read = new byte[client.ReceiveBufferSize];
                        int bytesRead = ns.Read(read, 0, client.ReceiveBufferSize);
                        if (bytesRead == 0) break;

                        string msg = Encoding.ASCII.GetString(read, 0, bytesRead);
                        Console.Write(msg);
                    }
                    Console.WriteLine("[[connection closed]]");
                }

            }
        }

        private static bool ClientIsConnected(Socket socket)
        {
            return !(socket.Poll(1000, SelectMode.SelectRead) && socket.Available == 0);
        }

    }
}
