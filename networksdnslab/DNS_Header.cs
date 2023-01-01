using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace networksdnslab
{
    public class DNS_Header
    {
        private ushort id;
        private ushort flags;
        private ushort qd; // num questions
        private ushort an; // answer rrs
        private ushort ns; // authority rrs
        private ushort ar; // additional rrs

        public int Id { get { return id; } }
        // flags
        public int IsResponse { get { return 0x0001 & (flags >> 15); } }
        public int Opcode { get { return 0x000f & (flags >> 11); } }
        public int AuthoritativeAnswer { get { return 0x0001 & (flags >> 10); } }
        public int Truncation { get { return 0x0001 & (flags >> 9); } }
        public int RecursionDesired { get { return 0x0001 & (flags >> 8); } }
        public int RecursionAvailable { get { return 0x0001 & (flags >> 7); } }
        public int Z { get { return 0x0007 & (flags >> 4); } }
        public int RCODE { get { return 0x000f & flags; } }
        // 
        public int Questions { get { return qd; } }
        public int AnswerRRs { get { return an; } }
        public int AuthorityRRs { get { return ns; } }
        public int AdditionalRRs { get { return ar; } }

        public DNS_Header(ushort Id, ushort Flags, ushort Qd, ushort An, ushort Ns, ushort Ar)
        {
            id = Id;
            flags = Flags;
            qd = Qd;
            an = An;
            ns = Ns;
            ar = Ar;
        }

        public DNS_Header(Byte[] b)
        {
            using (MemoryStream m = new MemoryStream(b))
            {
                using (BinaryReader reader = new BinaryReader(m))
                {
                    id = funcs.Swap(reader.ReadUInt16());
                    flags = funcs.Swap(reader.ReadUInt16());
                    qd = funcs.Swap(reader.ReadUInt16());
                    an = funcs.Swap(reader.ReadUInt16());
                    ns = funcs.Swap(reader.ReadUInt16());
                    ar = funcs.Swap(reader.ReadUInt16());
                }
            }
        }

        public byte[] toBytes(bool toBigEndian = true)
        {
            byte[] b_id = BitConverter.GetBytes(id);
            byte[] b_flags = BitConverter.GetBytes(flags);
            byte[] b_qd = BitConverter.GetBytes(qd);
            byte[] b_an = BitConverter.GetBytes(an);
            byte[] b_ns = BitConverter.GetBytes(ns);
            byte[] b_ar = BitConverter.GetBytes(ar);

            byte[] header = b_id.Concat(b_flags).Concat(b_qd).Concat(b_an).Concat(b_ns).Concat(b_ar).ToArray();

            // Swap the byte ordering if we're little endian
            if (BitConverter.IsLittleEndian && toBigEndian)
            {
                for (int i = 0; i < header.Length; i += 2)
                {
                    var temp = header[i];
                    header[i] = header[i + 1];
                    header[i + 1] = temp;
                }
            }

            return header;
        }

        public override string ToString()
        {
            string str = String.Format("**HEADER: opcode: {0} rcode: {1} id: {2}", Opcode, RCODE, id );
            return str;
        }
    }
}
