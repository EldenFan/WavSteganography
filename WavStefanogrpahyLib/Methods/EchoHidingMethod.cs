using System.Numerics;
using WavSteganographyLib.Base;
using WavSteganographyLib.Form;
using WavSteganographyLib.Interface;
using WavSteganographyLib.Properties;
using WavSteganographyLib.Utils;

namespace WavSteganographyLib.Methods
{
    public class EchoHidingMethod : IMethod
    {
        private const int BLOCKSIZE = 2048;

        private const int DELAY0 = 40;
        private const int DELAY1 = 120;

        private const double DECAY = 0.4;

        private const int CEPSTRUM_RADIUS = 5;

        private static readonly int HeaderEncodedBitCount = HammingCode.GetEncodedLength(Header.SIZE * 8);

        private static readonly double[] hammingWindow = BuildHammingWindow();

        public MethodsType Type => MethodsType.EchoHiding;

        public short[] Embed(short[] samples, StenagraphyData data)
        {
            var bytes = data.ToBytes();
            var rawBits = BitUtils.ToBits(bytes);

            var encodedBits = HammingCode.Encode(rawBits);

            var blockCount = samples.Length / BLOCKSIZE;

            if (blockCount < encodedBits.Length)
            {
                throw new Exception(Resources.Exception_DataBiggerSamples);
            }

            var result = new short[samples.Length];

            Array.Copy(samples, result, samples.Length);

            for (var blockIndex = 0; blockIndex < encodedBits.Length; blockIndex++)
            {
                var delay = encodedBits[blockIndex] ? DELAY1 : DELAY0;
                var offset = blockIndex * BLOCKSIZE;

                EmbedEchoInBlock(samples, result, offset, delay);
            }

            return result;
        }

        public StenagraphyData Extract(short[] samples)
        {
            var encodedHeaderBits = ExtractBits(samples, 0, HeaderEncodedBitCount);

            var headerBits = HammingCode.Decode(encodedHeaderBits, Header.SIZE * 8);

            var header = Header.FromBytes(BitUtils.ToBytes(headerBits));

            var dataBitCount = checked((int)header.DataSize * 8);
            var dataEncodedBitCount = HammingCode.GetEncodedLength(dataBitCount);

            var encodedDataBits = ExtractBits(samples, HeaderEncodedBitCount, dataEncodedBitCount);

            var dataBits = HammingCode.Decode(encodedDataBits, dataBitCount);

            var allBits = new bool[headerBits.Length + dataBits.Length];

            Array.Copy(headerBits, allBits, headerBits.Length);
            Array.Copy(dataBits, 0, allBits, headerBits.Length, dataBits.Length);

            return StenagraphyData.FromBytes(BitUtils.ToBytes(allBits));
        }

        private static void EmbedEchoInBlock(short[] source, short[] destination, int offset, int delay)
        {
            for (var n = 0; n < BLOCKSIZE; n++)
            {
                var sourceValue = source[offset + n];

                var echo = n >= delay ? DECAY * source[offset + n - delay] : 0.0;

                var value = sourceValue + echo;

                destination[offset + n] = (short)Math.Clamp(Math.Round(value), short.MinValue, short.MaxValue);
            }
        }

        private static double[] BuildHammingWindow()
        {
            var window = new double[BLOCKSIZE];

            for (var i = 0; i < BLOCKSIZE; i++)
            {
                window[i] = 0.54 - 0.46 * Math.Cos(2 * Math.PI * i / (BLOCKSIZE - 1));
            }

            return window;
        }

        private static bool[] ExtractBits(short[] samples, int blockStartIndex, int blockCount)
        {
            if (blockCount <= 0)
            {
                return [];
            }

            var totalBlocks = samples.Length / BLOCKSIZE;

            if (blockStartIndex + blockCount > totalBlocks)
            {
                throw new ArgumentException(Resources.Expection_SamplesSmallerExpectionData);
            }

            var bits = new bool[blockCount];

            for (var i = 0; i < blockCount; i++)
            {
                bits[i] = ExtractBitFromBlock(samples, blockStartIndex + i);
            }

            return bits;
        }

        private static bool ExtractBitFromBlock(short[] samples, int blockIndex)
        {
            var offset = blockIndex * BLOCKSIZE;

            var spectrum = new Complex[BLOCKSIZE];

            for (var n = 0; n < BLOCKSIZE; n++)
            {
                var windowed = samples[offset + n] * hammingWindow[n];

                spectrum[n] = new Complex(windowed, 0);
            }

            var freq = FFT.Forward(spectrum);

            var logMagnitude = new Complex[BLOCKSIZE];

            for (var k = 0; k < BLOCKSIZE; k++)
            {
                logMagnitude[k] = new Complex(Math.Log(freq[k].Magnitude + 1e-6), 0);
            }

            var cepstrum = FFT.Inverse(logMagnitude);

            var strength0 = PeakStrength(cepstrum, DELAY0, CEPSTRUM_RADIUS);

            var strength1 = PeakStrength(cepstrum, DELAY1, CEPSTRUM_RADIUS);

            return strength1 > strength0;
        }

        private static double PeakStrength(Complex[] cepstrum, int delay, int radius)
        {
            var sum = 0.0;
            var count = 0;

            for (var i = delay - radius; i <= delay + radius; i++)
            {
                if (i < 0 || i >= cepstrum.Length)
                {
                    continue;
                }

                sum += Math.Abs(cepstrum[i].Real);
                count++;
            }

            return count > 0 ? sum / count : 0.0;
        }
    }
}