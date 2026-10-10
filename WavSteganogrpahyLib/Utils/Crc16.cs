namespace WavSteganographyLib.Utils
{
    /// <summary>
    /// CRC-16/CCITT-FALSE: полином 0x1021, начальное значение 0xFFFF, без отражения и финального XOR.
    /// </summary>
    public static class Crc16
    {
        private static readonly ushort[] Table = BuildTable();

        public static ushort Compute(byte[] data)
        {
            ushort crc = 0xFFFF;

            foreach (var b in data)
            {
                var index = (byte)((crc >> 8) ^ b);
                crc = (ushort)((crc << 8) ^ Table[index]);
            }

            return crc;
        }

        private static ushort[] BuildTable()
        {
            const ushort polynomial = 0x1021;
            var table = new ushort[256];

            for (var i = 0; i < 256; i++)
            {
                var value = (ushort)(i << 8);

                for (var bit = 0; bit < 8; bit++)
                {
                    value = (value & 0x8000) != 0 ? (ushort)((value << 1) ^ polynomial) : (ushort)(value << 1);
                }

                table[i] = value;
            }

            return table;
        }
    }
}
