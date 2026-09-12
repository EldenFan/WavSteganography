using WavStefanogrpahyLib.Base;
using WavSteganographyLib.Form;
using WavSteganographyLib.Interface;

namespace WavSteganographyLib.Methods
{
    public class LsbMethod : IMethod
    {
        public MethodsType Type => MethodsType.Lsb;

        public short[] Embed(short[] samples, StenagraphyData data)
        {
            var bytes = data.ToBytes();

            if (bytes.Length * 8 > samples.Length)
            {
                throw new ArgumentException("Длина файла меньше, чем информация для стеганографии");
            }

            var result = (short[])samples.Clone();

            var samplesIndex = 0;
            foreach (var byteData in bytes)
            {
                for (var j = 7;  j >= 0; j--)
                {
                    var bit = (byteData >> j) & 1;
                    result[samplesIndex] = (short)((result[samplesIndex] & ~1) | bit);
                    samplesIndex++;
                }
            }

            return result;
        }

        public StenagraphyData Extract(short[] samples)
        {
            var headerBytes = ExtractBytes(samples, 0, Header.SIZE);
            var header = Header.FromBytes(headerBytes);
            var dataBytes = ExtractBytes(samples, Header.SIZE * 8, (int)header.DataSize);
            var allBytes = new byte[Header.SIZE + header.DataSize];

            Array.Copy(headerBytes, 0, allBytes, 0, Header.SIZE);
            
            Array.Copy(dataBytes, 0, allBytes, Header.SIZE, dataBytes.Length);

            return StenagraphyData.FromBytes(allBytes);
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
