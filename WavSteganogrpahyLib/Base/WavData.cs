namespace WavSteganographyLib.Base
{
    public class WavData
    {
        public ushort AudioFormat { get; set; }
        public ushort Channels { get; set; }
        public uint SampleRate { get; set; }
        public uint ByteRate { get; set; }
        public ushort BlockAlign { get; set; }
        public ushort BitsPerSample { get; set; }
        public short[] Samples { get; set; } = [];
    }
}
