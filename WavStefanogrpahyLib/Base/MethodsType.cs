namespace WavStefanogrpahyLib.Base
{
    public enum MethodsType : byte
    {
        /// <summary>Замена младшего бита амплитуды каждого сэмпла (time-domain, baseline).</summary>
        Lsb,

        /// <summary>Кодирование бит через задержку добавляемого эха (time-domain).</summary>
        EchoHiding,

        /// <summary>Кодирование бит в фазовом спектре первого сегмента (frequency-domain).</summary>
        PhaseCoding,

        /// <summary>Расширение спектра псевдослучайной последовательностью (frequency-domain).</summary>
        SpreadSpectrum,

        /// <summary>QIM-встраивание в детализирующие коэффициенты Haar DWT (transform-domain).</summary>
        WaveletCoding,
    }
}
