using System.Globalization;
using System.Numerics;
using WavSteganographyConsole.Base;
using WavSteganographyLib;
using WavSteganographyLib.Base;
using WavSteganographyLib.Utils;

namespace WavSteganographyConsole
{
    public class Program
    {
        static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                return;
            }

            if (!Enum.TryParse<WorkTypes>(args[0], ignoreCase: true, out var workType))
            {
                Console.WriteLine($"Неизвестная операция: {args[0]}");
                return;
            }

            try
            {
                switch (workType)
                {
                    case WorkTypes.Embed:
                        Embed(args);
                        break;

                    case WorkTypes.Extract:
                        Extract(args);
                        break;

                    case WorkTypes.Generate:
                        Generate(args);
                        break;

                    case WorkTypes.Compare:
                        Compare(args); 
                        break;

                    case WorkTypes.Ber:
                        Ber(args);
                        break;

                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Ошибка: {ex.Message}");
            }
        }

        private static void Embed(string[] args)
        {
            if (args.Length != 5)
            {
                Console.WriteLine("Использование: embed <input.wav> <output.wav> <method> <data>");
                return;
            }

            var inputFile = args[1];
            var outputFile = args[2];
            var methodName = args[3];
            var data = args[4];

            Console.WriteLine($"Input:  {inputFile}");
            Console.WriteLine($"Output: {outputFile}");
            Console.WriteLine($"Method: {methodName}");
            Console.WriteLine($"Data:   {data}");

            WavSteganography.Embed(inputFile, outputFile, methodName, data);
        }

        private static void Extract(string[] args)
        {
            if (args.Length != 3)
            {
                Console.WriteLine("Использование: extract <input.wav> method");
                return;
            }

            var inputFile = args[1];
            var methodName = args[2];

            Console.WriteLine($"Input:  {inputFile}");
            Console.WriteLine($"Method: {methodName}");

            var result = WavSteganography.Extract(inputFile, methodName);

            Console.WriteLine($"Result: {result}");
        }

        private static void Generate(string[] args)
        {
            if (args.Length < 2 || args.Length > 3)
            {
                Console.WriteLine("Использование: generate <output.wav> [seconds]");
                return;
            }

            var outputFile = args[1];
            var seconds = args.Length == 3 ? double.Parse(args[2], CultureInfo.InvariantCulture)  : 60.0;

            const int sampleRate = 44100;
            const int bitsPerSample = 16;
            const ushort channels = 1;

            var sampleCount = (int)(seconds * sampleRate);

            var samples = GenerateTestSignal(sampleCount);

            var wav = new WavData
            {
                AudioFormat = 1,
                Channels = channels,
                SampleRate = sampleRate,
                BitsPerSample = bitsPerSample,
                BlockAlign = (ushort)(channels * bitsPerSample / 8),
                ByteRate = (uint)(sampleRate * channels * bitsPerSample / 8),
                Samples = samples,
            };

            WavFile.WriteWavFile(outputFile, wav);

            Console.WriteLine($"Сгенерирован чистый тестовый сигнал: {outputFile}");
            Console.WriteLine($"Длительность: {seconds:F1} с, сэмплов: {sampleCount}, блоков (2048): {sampleCount / 2048}");
        }

        private static short[] GenerateTestSignal(int length, int seed = 42, double amplitude = 3000)
        {
            var rnd = new Random(seed);
            var samples = new short[length];

            for (var n = 0; n < length; n++)
            {
                var value = amplitude * Math.Sin(2 * Math.PI * 440 * n / 44100.0) + amplitude * 0.5 * Math.Sin(2 * Math.PI * 1000 * n / 44100.0) + (rnd.NextDouble() - 0.5) * 200;

                samples[n] = (short)Math.Clamp(value, short.MinValue, short.MaxValue);
            }

            return samples;
        }

        private static void Compare(string[] args)
        {
            if (args.Length != 3)
            {
                Console.WriteLine("Использование: compare <original.wav> <other.wav>");
                return;
            }

            var original = WavFile.ReadWavFile(args[1]);
            var other = WavFile.ReadWavFile(args[2]);

            var n = Math.Min(original.Samples.Length, other.Samples.Length);

            if (n == 0)
            {
                Console.WriteLine("Ошибка: один из файлов пуст.");
                return;
            }

            double signalEnergy = 0;
            double noiseEnergy = 0;

            for (var i = 0; i < n; i++)
            {
                var s = (double)original.Samples[i];
                var d = (double)other.Samples[i] - s;

                signalEnergy += s * s;
                noiseEnergy += d * d;
            }

            var snr = noiseEnergy > 1e-9
                ? 10.0 * Math.Log10(signalEnergy / noiseEnergy)
                : double.PositiveInfinity;

            var lsd = ComputeLogSpectralDistance(original.Samples, other.Samples, n);

            Console.WriteLine($"Compared samples: {n}");
            Console.WriteLine($"SNR: {snr:F2} dB");
            Console.WriteLine($"LSD: {lsd:F3} dB");
        }

        private static void Ber(string[] args)
        {
            if (args.Length != 4)
            {
                Console.WriteLine("Использование: ber <stego.wav> <method> <expected data>");
                Environment.ExitCode = 1;
                return;
            }

            var result = WavSteganography.MeasureBer(args[1], args[2], args[3]);

            Console.WriteLine($"Info bits: {result.InfoBits}, errors: {result.InfoErrors}");
            Console.WriteLine($"Block decisions: {result.RawBits}, errors: {result.RawErrors}");
            Console.WriteLine($"BER: {result.Ber:F6}");
            Console.WriteLine($"BER_RAW: {result.RawBer:F6}");
        }

        private const int CompareBlockSize = 2048;

        private static double ComputeLogSpectralDistance(short[] a, short[] b, int n)
        {
            var blockCount = n / CompareBlockSize;

            if (blockCount == 0)
            {
                return 0.0;
            }

            double totalDistance = 0;

            for (var block = 0; block < blockCount; block++)
            {
                var offset = block * CompareBlockSize;

                var specA = ComputeLogSpectrum(a, offset);
                var specB = ComputeLogSpectrum(b, offset);

                double sumSq = 0;

                for (var k = 0; k < specA.Length; k++)
                {
                    var diff = specA[k] - specB[k];
                    sumSq += diff * diff;
                }

                totalDistance += Math.Sqrt(sumSq / specA.Length);
            }

            return totalDistance / blockCount;
        }

        private static double[] ComputeLogSpectrum(short[] samples, int offset)
        {
            var spectrum = new Complex[CompareBlockSize];

            for (var n = 0; n < CompareBlockSize; n++)
            {
                spectrum[n] = new Complex(samples[offset + n], 0);
            }

            var freq = FFT.Forward(spectrum);

            var half = CompareBlockSize / 2;
            var logMagnitude = new double[half];

            for (var k = 0; k < half; k++)
            {
                logMagnitude[k] = 20.0 * Math.Log10(freq[k].Magnitude + 1e-6);
            }

            return logMagnitude;
        }
    }
}
