using System.Text;
using WavSteganographyLib.Factory;
using WavSteganographyLib.Form;
using WavSteganographyLib.Utils;

namespace WavSteganographyLib
{
    public static class WavSteganography
    {
        public static void Embed(string inputFile, string outputFile, string method, string data)
        {
            var wav = WavFile.ReadWavFile(inputFile);

            var steganographyMethod = MethodFactory.Create(method);

            var steganographyData = new StenagraphyData(data, steganographyMethod.Type);

            var samples = steganographyMethod.Embed(wav.Samples, steganographyData);

            wav.Samples = samples;

            WavFile.WriteWavFile(outputFile, wav);
        }

        public static string Extract(string inputFile, string method)
        {
            var wav = WavFile.ReadWavFile(inputFile);

            var steganographyMethod = MethodFactory.Create(method);

            var steganographyData = steganographyMethod.Extract(wav.Samples);

            return Encoding.UTF8.GetString(steganographyData.Data);
        }
    }
}
