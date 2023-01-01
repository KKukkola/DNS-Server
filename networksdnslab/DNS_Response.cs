using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace networksdnslab
{
    public class DNS_Response
    {
        private DNS_Header rheader = null;
        private DNS_Question rquestion = null;
        private List<DNS_RR> answers = new List<DNS_RR>();
        private List<DNS_RR> authoritys = new List<DNS_RR>();
        private List<DNS_RR> additionals = new List<DNS_RR>();

        public DNS_Header Header { get { return rheader; } }
        public DNS_Question Question { get { return rquestion; } }
        public List<DNS_RR> Answers { get { return answers; } }
        public List<DNS_RR> Authoritys { get { return authoritys; } }
        public List<DNS_RR> Additionals { get { return additionals; } }

        public DNS_Response(byte[] receivedData)
        {
            rheader = new DNS_Header(receivedData);
            rquestion = new DNS_Question(receivedData);

            int offset = 12 + rquestion.CalcLength();

            for (int i = 0; i < rheader.AnswerRRs; i++)
            {
                DNS_RR rr = new DNS_RR(receivedData, offset);
                answers.Add(rr);
                offset += rr.CalcLength();
            }

            for (int i = 0; i < rheader.AuthorityRRs; i++)
            {
                DNS_RR rr = new DNS_RR(receivedData, offset);
                authoritys.Add(rr);
                offset += rr.CalcLength();
            }

            for (int i = 0; i < rheader.AdditionalRRs; i++)
            {
                DNS_RR rr = new DNS_RR(receivedData, offset);
                additionals.Add(rr);
                offset += rr.CalcLength();
            }
        }

        public DNS_RR GetRandomA()
        {
            if (answers.Count == 0)
                return null;
            Random random = new Random();
            int r = random.Next(0, answers.Count);
            return Answers[r];
        }

        // Look through additional records for a matching host
        public string GetResolvedNS(string qname)
        {
            foreach (DNS_RR auth_rr in authoritys)
            {
                if (qname.EndsWith(auth_rr.Name)) // filter for valid authorities
                {
                    foreach (DNS_RR add_rr in additionals) // resolve the authority's ip using the additionals
                    {
                        if (add_rr.Type == 1 && add_rr.Name == auth_rr.Rdata)
                        {
                            return add_rr.Rdata;
                        }
                    }
                }
            }

            return null;
        }

        public string GetUnresolvedNS(string qname)
        {
            if (authoritys.Count == 0)
                return null;
            return authoritys[0].Rdata;
        }

        public override string ToString()
        {
            string str = String.Format("\n**RESPONSE********************\n{0}", rheader);
            str += "\nAnswers\n";
            Answers.ForEach((DNS_RR rr) => { str += String.Format("\t {0}\n", rr); });
            str += "Authoritys\n";
            Authoritys.ForEach((DNS_RR rr) => { str += String.Format("\t {0}\n", rr); });
            str += "Additionals\n";
            Additionals.ForEach((DNS_RR rr) => { str += String.Format("\t {0}\n", rr); });
            return str;
        }
    }
}
