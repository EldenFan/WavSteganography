using System.Numerics;

namespace WavStefanogrpahyLib.Utils
{
    public static class FFT
    {
        public static Complex[] Forward(Complex[] input) => Transform(input, false);

        public static Complex[] Inverse(Complex[] input)
        {
            var result = Transform(input, true);
            for (int i = 0; i < result.Length; i++)
            {
                result[i] /= input.Length;
            }
            return result;
        }

        private static Complex[] Transform(Complex[] input, bool inverse)
        {
            var n = input.Length;

            if (n == 1)
            {
                return [input[0]];
            }
            if ((n & (n - 1)) != 0)
            {
                throw new ArgumentException("Длина входа БПФ должна быть степенью двойки");
            }

            var even = new Complex[n / 2];
            var odd = new Complex[n / 2];
            for (int i = 0; i < n / 2; i++)
            {
                even[i] = input[2 * i];
                odd[i] = input[2 * i + 1];
            }

            var evenT = Transform(even, inverse);
            var oddT = Transform(odd, inverse);

            var result = new Complex[n];
            double sign = inverse ? 1 : -1;
            for (int k = 0; k < n / 2; k++)
            {
                var twiddle = Complex.FromPolarCoordinates(1.0, sign * 2 * Math.PI * k / n) * oddT[k];
                result[k] = evenT[k] + twiddle;
                result[k + n / 2] = evenT[k] - twiddle;
            }
            return result;
        }
    }
}
