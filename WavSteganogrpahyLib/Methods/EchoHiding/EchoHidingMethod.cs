using System.Numerics;
using WavSteganographyLib.Base;
using WavSteganographyLib.Form;
using WavSteganographyLib.Interface;
using WavSteganographyLib.Properties;
using WavSteganographyLib.Utils;

namespace WavSteganographyLib.Methods.EchoHiding
{
    public class EchoHidingMethod(EchoHidingVariant variant = EchoHidingVariant.Hamming) : IMethod
    {
        private const int BLOCKSIZE = 2048;

        private const int DELAY0 = 40;
        private const int DELAY1 = 120;

        private const double DECAY = 0.4;

        private const int CEPSTRUM_RADIUS = 5;

        private const int REPEAT_COUNT = 5;

        private static readonly double[] hammingWindow = BuildHammingWindow();

        private static readonly int HeaderEncodedBitCountHamming = HammingCode.GetEncodedLength(Header.SIZE * 8);

        private readonly EchoHidingVariant variant = variant;

        public MethodsType Type => MethodsType.EchoHiding;

        private bool UseWindow => variant != EchoHidingVariant.Naive;

        private bool UseEcc => variant == EchoHidingVariant.Hamming;

        private int RepeatCount => variant is EchoHidingVariant.Repeat or EchoHidingVariant.RepeatSpread ? REPEAT_COUNT : 1;

        public short[] Embed(short[] samples, StenagraphyData data)
        {
            var bytes = data.ToBytes();
            var rawBits = BitUtils.ToBits(bytes);

            var headerRawBitCount = Header.SIZE * 8;
            var dataRawBitCount = rawBits.Length - headerRawBitCount;

            var headerCoded = Encode(rawBits, 0, headerRawBitCount);
            var dataCoded = Encode(rawBits, headerRawBitCount, dataRawBitCount);

            var blockCount = samples.Length / BLOCKSIZE;
            var stride = blockCount / REPEAT_COUNT;

            var totalLogicalBits = headerCoded.Length + dataCoded.Length;

            ValidateCapacity(totalLogicalBits, blockCount, stride);

            var result = new short[samples.Length];
            Array.Copy(samples, result, samples.Length);

            EmbedRegion(samples, result, headerCoded, logicalOffset: 0, stride);
            EmbedRegion(samples, result, dataCoded, logicalOffset: headerCoded.Length, stride);

            return result;
        }

        public StenagraphyData Extract(short[] samples)
        {
            var blockCount = samples.Length / BLOCKSIZE;
            var stride = blockCount / REPEAT_COUNT;

            var headerCodedLength = UseEcc ? HeaderEncodedBitCountHamming : Header.SIZE * 8;

            var headerCoded = ExtractRegion(samples, logicalOffset: 0, headerCodedLength, stride);
            var headerBits = Decode(headerCoded, Header.SIZE * 8);

            var header = Header.FromBytes(BitUtils.ToBytes(headerBits));

            var dataRawBitCount = checked((int)header.DataSize * 8);
            var dataCodedLength = UseEcc ? HammingCode.GetEncodedLength(dataRawBitCount) : dataRawBitCount;

            var dataCoded = ExtractRegion(samples, logicalOffset: headerCodedLength, dataCodedLength, stride);
            var dataBits = Decode(dataCoded, dataRawBitCount);

            var allBits = new bool[headerBits.Length + dataBits.Length];

            Array.Copy(headerBits, allBits, headerBits.Length);
            Array.Copy(dataBits, 0, allBits, headerBits.Length, dataBits.Length);

            return StenagraphyData.FromBytes(BitUtils.ToBytes(allBits));
        }

        public BerResult MeasureBer(short[] samples, StenagraphyData expected)
        {
            var rawBits = BitUtils.ToBits(expected.ToBytes());

            var headerRawBitCount = Header.SIZE * 8;
            var dataRawBitCount = rawBits.Length - headerRawBitCount;

            var headerCoded = Encode(rawBits, 0, headerRawBitCount);
            var dataCoded = Encode(rawBits, headerRawBitCount, dataRawBitCount);

            var expectedCoded = new bool[headerCoded.Length + dataCoded.Length];

            Array.Copy(headerCoded, expectedCoded, headerCoded.Length);
            Array.Copy(dataCoded, 0, expectedCoded, headerCoded.Length, dataCoded.Length);

            var blockCount = samples.Length / BLOCKSIZE;
            var stride = blockCount / REPEAT_COUNT;
            var repeatCount = RepeatCount;

            var lastLogicalIndex = expectedCoded.Length - 1;
            var maxBlockIndex = GetBlockIndex(lastLogicalIndex, repeatCount - 1, stride);

            if (maxBlockIndex >= blockCount || variant == EchoHidingVariant.RepeatSpread && stride <= lastLogicalIndex)
            {
                throw new ArgumentException(Resources.Expection_SamplesSmallerExpectionData);
            }

            var voted = new bool[expectedCoded.Length];
            var rawErrors = 0;
            var rawTotal = 0;

            for (var i = 0; i < expectedCoded.Length; i++)
            {
                var trueCount = 0;

                for (var repeat = 0; repeat < repeatCount; repeat++)
                {
                    var detected = ExtractBitFromBlock(samples, GetBlockIndex(i, repeat, stride));

                    if (detected)
                    {
                        trueCount++;
                    }

                    if (detected != expectedCoded[i])
                    {
                        rawErrors++;
                    }

                    rawTotal++;
                }

                voted[i] = trueCount * 2 > repeatCount;
            }

            var headerBits = Decode(voted[..headerCoded.Length], headerRawBitCount);
            var dataBits = Decode(voted[headerCoded.Length..], dataRawBitCount);

            var infoErrors = 0;

            for (var i = 0; i < headerBits.Length; i++)
            {
                if (headerBits[i] != rawBits[i])
                {
                    infoErrors++;
                }
            }

            for (var i = 0; i < dataBits.Length; i++)
            {
                if (dataBits[i] != rawBits[headerRawBitCount + i])
                {
                    infoErrors++;
                }
            }

            return new BerResult(rawBits.Length, infoErrors, rawTotal, rawErrors);
        }

        private bool[] Encode(bool[] allRawBits, int offset, int count)
        {
            var slice = new bool[count];
            Array.Copy(allRawBits, offset, slice, 0, count);

            return UseEcc ? HammingCode.Encode(slice) : slice;
        }

        private bool[] Decode(bool[] codedBits, int originalBitCount)
        {
            return UseEcc ? HammingCode.Decode(codedBits, originalBitCount) : codedBits;
        }

        private void ValidateCapacity(int totalLogicalBits, int blockCount, int stride)
        {
            if (variant == EchoHidingVariant.RepeatSpread && stride < totalLogicalBits)
            {
                throw new Exception(Resources.Exception_DataBiggerSamples);
            }

            if (RequiredBlocks(totalLogicalBits, stride) > blockCount)
            {
                throw new Exception(Resources.Exception_DataBiggerSamples);
            }
        }

        private int RequiredBlocks(int totalLogicalBits, int stride)
        {
            return variant switch
            {
                EchoHidingVariant.Repeat => totalLogicalBits * REPEAT_COUNT,
                EchoHidingVariant.RepeatSpread => (REPEAT_COUNT - 1) * stride + totalLogicalBits,
                _ => totalLogicalBits,
            };
        }

        private int GetBlockIndex(int logicalIndex, int repeat, int stride)
        {
            return variant switch
            {
                EchoHidingVariant.Repeat => logicalIndex * REPEAT_COUNT + repeat,

                EchoHidingVariant.RepeatSpread => repeat * stride + logicalIndex,

                _ => logicalIndex,
            };
        }

        private void EmbedRegion(short[] samples, short[] result, bool[] codedBits, int logicalOffset, int stride)
        {
            var repeatCount = RepeatCount;

            for (var i = 0; i < codedBits.Length; i++)
            {
                var logicalIndex = logicalOffset + i;
                var delay = codedBits[i] ? DELAY1 : DELAY0;

                for (var repeat = 0; repeat < repeatCount; repeat++)
                {
                    var blockIndex = GetBlockIndex(logicalIndex, repeat, stride);
                    var offset = blockIndex * BLOCKSIZE;

                    EmbedEchoInBlock(samples, result, offset, delay);
                }
            }
        }

        private bool[] ExtractRegion(short[] samples, int logicalOffset, int bitCount, int stride)
        {
            var repeatCount = RepeatCount;
            var totalBlocks = samples.Length / BLOCKSIZE;

            var lastLogicalIndex = logicalOffset + bitCount - 1;
            var maxBlockIndex = GetBlockIndex(lastLogicalIndex, repeatCount - 1, stride);

            if (maxBlockIndex >= totalBlocks || variant == EchoHidingVariant.RepeatSpread && stride <= lastLogicalIndex)
            {
                throw new ArgumentException(Resources.Expection_SamplesSmallerExpectionData);
            }

            var bits = new bool[bitCount];

            for (var i = 0; i < bitCount; i++)
            {
                var logicalIndex = logicalOffset + i;

                bits[i] = repeatCount == 1 ? ExtractBitFromBlock(samples, GetBlockIndex(logicalIndex, 0, stride)) : ExtractBitMajority(samples, logicalIndex, stride, repeatCount);
            }

            return bits;
        }

        private bool ExtractBitMajority(short[] samples, int logicalIndex, int stride, int repeatCount)
        {
            var trueCount = 0;

            for (var repeat = 0; repeat < repeatCount; repeat++)
            {
                var blockIndex = GetBlockIndex(logicalIndex, repeat, stride);

                if (ExtractBitFromBlock(samples, blockIndex))
                {
                    trueCount++;
                }
            }

            return trueCount * 2 > repeatCount;
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

        private bool ExtractBitFromBlock(short[] samples, int blockIndex)
        {
            var offset = blockIndex * BLOCKSIZE;

            var spectrum = new Complex[BLOCKSIZE];

            for (var n = 0; n < BLOCKSIZE; n++)
            {
                var value = UseWindow ? samples[offset + n] * hammingWindow[n] : samples[offset + n];

                spectrum[n] = new Complex(value, 0);
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