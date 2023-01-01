using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace networksdnslab
{
    class DNS_Data
    {
        public static Hashtable stored_answers = new Hashtable();
        public static Hashtable stored_ns = new Hashtable();

        // Clear the cached data
        public static void ClearCache()
        {
            stored_answers.Clear();
            stored_ns.Clear();
        }

        // Store an answer
        public static void AddAnswer(string name, string id)
        {
            List<string> answers = (List<string>)stored_answers[name];
            if (answers == null)
            {   
                stored_answers[name] = new List<string>();
                ((List<string>)stored_answers[name]).Add(id);
            }
            else
            {  
                string result = answers.Find(x => x == id);
                if (result == null)
                    answers.Add(id);
            }
        }

        // Store an Authority/ns
        public static void AddAuthority(string domain, string host)
        {
            List<string> answers = (List<string>)stored_ns[domain];
            if (answers == null)
            {
                stored_ns[domain] = new List<string>();
                ((List<string>)stored_ns[domain]).Add(host);
            }
            else
            {
                string result = answers.Find(x => x == host);
                if (result == null)
                    answers.Add(host);
            }
        }

        // Retrieve an answer or null
        public static List<string> HasAnswer(string qname)
        {
            return (List<string>)stored_answers[qname];
        }

        // Find a better server to begin with rather than start from the root
        public static List<string> BetterRoot(string root)
        {
            string[] parts = root.Split('.');
            for (int i = 0; i < parts.Length; i++)
            {
                string term = String.Join(".", new ArraySegment<string>(parts, i, parts.Length-i));
                if (stored_ns[term] != null)
                    return (List<string>)stored_ns[term];
            }
            return null;
        }
    }
}
