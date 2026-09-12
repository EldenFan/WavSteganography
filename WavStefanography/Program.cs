using WavSteganographyConsole.Base;
using WavSteganographyLib;

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
    }
}
