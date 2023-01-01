using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace networksdnslab
{
    public class DNS_RR
    {
        private string name;
        private ushort type;
        private ushort clas;
        private uint ttl;
        private ushort rdlength;
        private string rdata;

        private int namelength;

        public string Name { get { return name; } }
        public ushort Type { get { return type; } }
        public ushort Class { get { return clas; } }
        public uint Ttl { get { return ttl; } }
        public ushort Rdlength { get { return rdlength; } }
        public string Rdata { get { return rdata; } } // A : 32 bit internet address of the host

        public DNS_RR(byte[] b, int offset)
        {
            namelength = 0;
            name = funcs.unformatHostName(b, offset, ref namelength); // nameOffset 

            offset += namelength; // +2 to be after the name offset
            type = BitConverter.ToUInt16(new byte[2] { b[offset + 1], b[offset] }, 0); // swap bytes
            clas = BitConverter.ToUInt16(new byte[2] { b[offset + 3], b[offset + 2] }, 0); // swap bytes
            ttl = (uint)((0xffff & b[offset + 4]) | (0xffff & b[offset + 5]) | (0xffff & b[offset + 6]) | (0xffff & b[offset + 7])); // bit converter didnt want to function correctly

            rdlength = BitConverter.ToUInt16(new byte[2] { b[offset + 9], b[offset + 8] }, 0); // swap bytes

            switch (type)
            {
                case 1: // A - Alias - a 32 bit internet address
                    rdata = String.Join(".", new ushort[4] { b[offset + 10], b[offset + 11], b[offset + 12], b[offset + 13] });
                    break;
                case 2: // NS - name Server - The DNS server address for a domain
                    rdata = funcs.unformatHostName(b, offset + 10);
                    break;
                case 5: // CNAME - Canonical Name - Maps names to names.
                    rdata = funcs.unformatHostName(b, offset + 10);
                    break;
                case 15: // MX - Mail eXchange - the host of the mail server for a domain
                    rdata = "NOT IMPLEMENTED MX"; // 2bytes for priority + label sequence
                    break;
                case 28: // AAAA - IPv6 alias
                    rdata = funcs.BytesToIPv6Address(b, offset + 10); 
                    break;
                default:
                    rdata = "UNKNOWN";
                    break;
            }
        }

        public int CalcLength()
        {
            return namelength + 2 + 2 + 4 + 2 + Rdlength;
        }

        public override string ToString()
        {
            return String.Format("{0} {1} {2}", name, type, rdata);
        }
    }
}
