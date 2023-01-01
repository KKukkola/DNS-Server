using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace networksdnslab
{
    public class DNS_Question
    {
        private string qname;
        private ushort qtype;
        private ushort qclass;

        public string Name { get { return qname; } set { qname = value; } }
        public ushort Qtype { get { return qtype; } }
        public ushort Qclass { get { return qclass; } }

        public DNS_Question(string Name, ushort Qtype, ushort Qclass)
        {
            qname = Name;
            qtype = Qtype;
            qclass = Qclass;
        }

        public DNS_Question(byte[] b)
        {
            qname = funcs.unformatHostName(b, 12); // 12 because const header size

            int offset = 12 + qname.Length + 2;
            qtype = BitConverter.ToUInt16(new byte[2] { b[offset + 1], b[offset] }, 0); // swap bits
            qclass = BitConverter.ToUInt16(new byte[2] { b[offset + 3], b[offset + 2] }, 0); // swap bits
        }

        public byte[] toBytes(bool toBigEndian = true)
        {
            byte[] hostname = funcs.formatHostName(qname); // is already big endian
            byte[] b_qtype = BitConverter.GetBytes(qtype);
            byte[] b_qclass = BitConverter.GetBytes(qclass);

            byte[] questionEnd = b_qtype.Concat(b_qclass).ToArray();

            // Swap the byte ordering of type and class if we're little endian
            if (BitConverter.IsLittleEndian && toBigEndian)
            {
                for (int i = 0; i < questionEnd.Length; i += 2)
                {
                    var temp = questionEnd[i];
                    questionEnd[i] = questionEnd[i + 1];
                    questionEnd[i + 1] = temp;
                }
            }

            return hostname.Concat(questionEnd).ToArray();
        }

        public int CalcLength()
        {
            return qname.Length + 2 + 4; // +2 for start and end byte. +4 for qtype and qclass.
        }
    }
}
