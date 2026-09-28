using WavSteganographyLib.Form;

namespace WavSteganographyLib.Utils
{
    public static class ReadHeader
    {
        public static Header Read(short[] samples)
        {
            var headerBytes = ExtractBytes(samples, 0, Header.SIZE);

            return Header.FromBytes(headerBytes);
        }

        private static byte[] ExtractBytes(short[] samples, int sampleOffset, int byteCount)
        {
            if (sampleOffset + byteCount * 8 > samples.Length)
            {
                throw new ArgumentException("В аудиоданных недостаточно информации");
            }

            var result = new byte[byteCount];

            for (var i = 0; i < byteCount; i++)
            {
                byte value = 0;

                for (var bitIndex = 7; bitIndex >= 0; bitIndex--)
                {
                    var bit = samples[sampleOffset++] & 1;

                    value |= (byte)(bit << bitIndex);
                }

                result[i] = value;
            }

            return result;
        }
    }
}
