using System;

namespace WavSteganographyLib.Utils
{
    public static class HammingCode
    {
        private const int DataBitsPerBlock = 4;
        private const int EncodedBitsPerBlock = 7;

        public static bool[] Encode(bool[] dataBits)
        {
            var paddedLength = RoundUp(dataBits.Length, DataBitsPerBlock);
            var padded = new bool[paddedLength];
            Array.Copy(dataBits, padded, dataBits.Length);

            var blockCount = paddedLength / DataBitsPerBlock;
            var encoded = new bool[blockCount * EncodedBitsPerBlock];

            for (var i = 0; i < blockCount; i++)
            {
                var d1 = padded[i * DataBitsPerBlock + 0];
                var d2 = padded[i * DataBitsPerBlock + 1];
                var d3 = padded[i * DataBitsPerBlock + 2];
                var d4 = padded[i * DataBitsPerBlock + 3];

                var p1 = d1 ^ d2 ^ d4;
                var p2 = d1 ^ d3 ^ d4;
                var p3 = d2 ^ d3 ^ d4;

                var baseIndex = i * EncodedBitsPerBlock;

                encoded[baseIndex + 0] = p1;
                encoded[baseIndex + 1] = p2;
                encoded[baseIndex + 2] = d1;
                encoded[baseIndex + 3] = p3;
                encoded[baseIndex + 4] = d2;
                encoded[baseIndex + 5] = d3;
                encoded[baseIndex + 6] = d4;
            }

            return encoded;
        }

        public static bool[] Decode(bool[] encodedBits, int originalBitCount)
        {
            if (encodedBits.Length % EncodedBitsPerBlock != 0)
            {
                throw new ArgumentException("Длина закодированных бит должна быть кратна 7");
            }

            var blockCount = encodedBits.Length / EncodedBitsPerBlock;
            var decoded = new bool[blockCount * DataBitsPerBlock];

            var block = new bool[EncodedBitsPerBlock];

            for (var i = 0; i < blockCount; i++)
            {
                var baseIndex = i * EncodedBitsPerBlock;

                Array.Copy(encodedBits, baseIndex, block, 0, EncodedBitsPerBlock);

                CorrectSingleBitError(block);

                decoded[i * DataBitsPerBlock + 0] = block[2];
                decoded[i * DataBitsPerBlock + 1] = block[4];
                decoded[i * DataBitsPerBlock + 2] = block[5]; 
                decoded[i * DataBitsPerBlock + 3] = block[6]; 
            }

            if (originalBitCount > decoded.Length)
            {
                throw new ArgumentException("originalBitCount больше, чем было закодировано данных");
            }

            var result = new bool[originalBitCount];
            Array.Copy(decoded, result, originalBitCount);
            return result;
        }

        public static int GetEncodedLength(int dataBitCount)
        {
            var blockCount = RoundUp(dataBitCount, DataBitsPerBlock) / DataBitsPerBlock;
            return blockCount * EncodedBitsPerBlock;
        }

        private static void CorrectSingleBitError(bool[] block)
        {
            var c1 = block[0] ^ block[2] ^ block[4] ^ block[6];
            var c2 = block[1] ^ block[2] ^ block[5] ^ block[6];
            var c3 = block[3] ^ block[4] ^ block[5] ^ block[6];

            var errorPosition = (c1 ? 1 : 0) + (c2 ? 2 : 0) + (c3 ? 4 : 0);

            if (errorPosition != 0)
            {
                block[errorPosition - 1] = !block[errorPosition - 1];
            }
        }

        private static int RoundUp(int value, int multiple)
        {
            var remainder = value % multiple;
            return remainder == 0 ? value : value + (multiple - remainder);
        }
    }
}