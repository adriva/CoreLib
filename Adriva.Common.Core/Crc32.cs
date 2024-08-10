using System.Text;

namespace Adriva.Common.Core
{
    public static class Crc32
    {
        public const uint DefaultPolynomial = 0xedb88320u;
        public const uint DefaultSeed = 0xffffffffu;

        private static uint[] DefaultTable;

        static Crc32()
        {
            Crc32.DefaultTable = new uint[256];
            for (var i = 0; i < 256; i++)
            {
                var entry = (uint)i;
                for (var j = 0; j < 8; j++)
                    if ((entry & 1) == 1)
                        entry = (entry >> 1) ^ Crc32.DefaultPolynomial;
                    else
                        entry = entry >> 1;
                Crc32.DefaultTable[i] = entry;
            }
        }

        public static uint Compute(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return 0;
            var bytes = Encoding.UTF8.GetBytes(text);
            return Crc32.Compute(bytes);
        }

        public static uint Compute(byte[] data)
        {
            var hash = Crc32.DefaultSeed;
            for (var i = 0; i < data.Length; i++)
                hash = (hash >> 8) ^ Crc32.DefaultTable[data[i] ^ hash & 0xff];
            return ~hash;
        }
    }
}