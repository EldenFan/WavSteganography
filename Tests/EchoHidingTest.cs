using System.Text;
using WavSteganographyLib.Form;
using WavSteganographyLib.Methods;

namespace Tests
{
    [TestFixture]
    public class EchoHidingTest
    {  
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

        [Test]
        public void EmbedExtract_RoundTrip()
        {
            var method = new EchoHidingMethod();

            var original = "Hello Echo Hiding!";
            var data = new StenagraphyData(original);

            var samples = GenerateTestSignal(10000 * 2048);

            var embedded = method.Embed(samples, data);
            var extracted = method.Extract(embedded);

            Assert.That(Encoding.UTF8.GetString(extracted.Data), Is.EqualTo(original));
        }
    }
}
