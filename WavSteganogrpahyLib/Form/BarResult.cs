namespace WavSteganographyLib.Form
{
    public readonly record struct BerResult(int InfoBits, int InfoErrors, int RawBits, int RawErrors)
    {
        public double Ber => InfoBits == 0 ? 0.0 : (double)InfoErrors / InfoBits;

        public double RawBer => RawBits == 0 ? 0.0 : (double)RawErrors / RawBits;
    }
}
