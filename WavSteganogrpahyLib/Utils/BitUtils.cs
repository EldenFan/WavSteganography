using System;
namespace WavSteganographyLib.Utils
{
    public static class BitUtils
    {
        public static bool[] ToBits(byte[] bytes)
        {
            var bits = new bool[bytes.Length * 8];

            var index = 0;
            foreach (var b in bytes)
            {
                for (var j = 7; j >= 0; j--)
                {
                    bits[index++] = ((b >> j) & 1) == 1;
                }
            }

            return bits;
        }

        public static byte[] ToBytes(bool[] bits)
        {
            if (bits.Length % 8 != 0)
            {
                throw new ArgumentException("Количество бит должно быть кратно 8");
            }

            var bytes = new byte[bits.Length / 8];

            for (var i = 0; i < bytes.Length; i++)
            {
                byte value = 0;

                for (var j = 0; j < 8; j++)
                {
                    value = (byte)((value << 1) | (bits[i * 8 + j] ? 1 : 0));
                }

                bytes[i] = value;
            }

            return bytes;
        }
    }
}
