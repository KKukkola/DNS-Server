using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace networksdnslab
{
    public static class funcs
    {
        public static ushort Swap(ushort x)
        {
            return (ushort)((ushort)((x & 0xff) << 8) | ((x >> 8) & 0xff));
        }

        public static byte[] formatHostName(string host)
        {
            List<byte> retBytes = new List<byte>();

            foreach (string s in host.Split('.'))
            {
                retBytes.Add((byte)s.Length);
                foreach (byte b in System.Text.ASCIIEncoding.Default.GetBytes(s))
                {
                    retBytes.Add(b);
                }
            }

            retBytes.Add(0x00);

            return retBytes.ToArray(); //hostnameLength.Concat(hostname).Concat(hostdomainLength).Concat(hostdomain).Concat(hostend).ToArray();
        }

        // does not calculate the name length
        public static string unformatHostName(byte[] b, int offset)
        {
            // Check if this is actually a label/offset
            uint num = BitConverter.ToUInt16(new byte[2] { b[offset + 1], b[offset + 0] }, 0);
            if ((0xc000 & num) == 0xc000)
            {
                return unformatHostName(b, decodeOffset(b, offset));
            }

            string name = "";
            while (b[offset] != 0)
            {
                int endOffset = b[offset] + offset;
                while (offset < endOffset)
                {
                    offset++;
                    name += (char)(b[offset]);
                }
                offset++;

                if (b[offset] != 0)
                {
                    name += ".";
                    if ((0xc0 & b[offset]) == 0xc0) // if the end of this is a pointer
                        offset = decodeOffset(b, offset); 
                }

            }

            return name;
        }

        // also calculates the namelength
        public static string unformatHostName(byte[] b, int offset, ref int namelength, bool firstPass = true)
        {
            // Check if this is actually a label/offset
            uint num = BitConverter.ToUInt16(new byte[2] { b[offset + 1], b[offset + 0] }, 0);
            if ((0xc000 & num) == 0xc000)
            {
                if (firstPass) namelength += 2;
                return unformatHostName(b, decodeOffset(b, offset), ref namelength, false);
            }

            string name = "";
            while (b[offset] != 0)
            {
                int endOffset = b[offset] + offset;
                while (offset < endOffset)
                {
                    offset++;
                    name += (char)(b[offset]);
                    if (firstPass) namelength += 1;
                }
                offset++;

                if (b[offset] != 0)
                {
                    name += ".";
                    if (firstPass) namelength += 1;
                    if ((0xc0 & b[offset]) == 0xc0) // if the end of this is a pointer
                    {
                        if (firstPass) namelength += 2;
                        return name + unformatHostName(b, offset, ref namelength, false);
                    }
                }
                
            }

            return name;
        }

        public static int decodeOffset(byte[] b, int offset)
        {
            ushort num = BitConverter.ToUInt16(new byte[2] { b[offset + 1], b[offset + 0] }, 0);
            return 0x3fff & num;
        }

        public static uint toUInt32(byte[] b, int offset)
        {
            return (uint)((0xffff & b[offset]) | (0xffff & b[offset + 1]) | (0xffff & b[offset + 2]) | (0xffff & b[offset + 3])); 
        }

        public static string BytesToIPv6Address(byte[] bytes, int offset)
        {
            int len = 16; // precondition
            var str = new StringBuilder();
            for (var i = offset; i < offset+len; i += 2)
            {
                var segment = (ushort)bytes[i] << 8 | bytes[i + 1];
                str.AppendFormat("{0:X}", segment);
                if (i + 2 != len)
                {
                    str.Append(':');
                }
            }

            return str.ToString();
        }

    }
}
