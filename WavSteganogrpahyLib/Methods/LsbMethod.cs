using WavSteganographyLib.Base;
using WavSteganographyLib.Form;
using WavSteganographyLib.Interface;
using WavSteganographyLib.Properties;
using WavSteganographyLib.Utils;

namespace WavSteganographyLib.Methods
{
    public class LsbMethod : IMethod
    {
        public MethodsType Type => MethodsType.Lsb;

        public short[] Embed(short[] samples, StenagraphyData data)
        {
            if (data.Size * 8 > samples.Length)
            {
                throw new ArgumentException(Resources.Exception_DataBiggerSamples);
            }

            var result = (short[])samples.Clone();

            var bits = BitUtils.ToBits(data.ToBytes());
            
            for (int i = 0; i < bits.Length; i++)
            {
                result[i] = (short)((result[i] & ~1) | (bits[i] ? 1 : 0));
            }

            return result;
        }

        public StenagraphyData Extract(short[] samples)
        {
            var headerBits = ExtractBits(samples, 0, Header.SIZE * 8);
            var header = Header.FromBytes(BitUtils.ToBytes(headerBits));
            var dataBytes = BitUtils.ToBytes(ExtractBits(samples, Header.SIZE * 8, (int)header.DataSize * 8));
            var allBytes = new byte[Header.SIZE + header.DataSize];

            Array.Copy(header.ToBytes(), 0, allBytes, 0, Header.SIZE);
            
            Array.Copy(dataBytes, 0, allBytes, Header.SIZE, dataBytes.Length);

            return StenagraphyData.FromBytes(allBytes);
        }

        private static bool[] ExtractBits(short[] samples, int sampleOffset, int bitsCount)
        {
            if (sampleOffset + bitsCount > samples.Length)
            {
                throw new ArgumentException(Resources.Expection_SamplesSmallerExpectionData);
            }

            var bits = new bool[bitsCount];

            for (var i = 0; i < bitsCount; i++)
            {
                bits[i] = (samples[sampleOffset + i] & 1) == 1;
            }

            return bits;
        }
    }
}
