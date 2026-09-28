using System.Globalization;
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
    }
}
