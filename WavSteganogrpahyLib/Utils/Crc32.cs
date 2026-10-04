namespace WavSteganographyLib.Utils
{
    public static class Crc32
    {
        private static readonly uint[] Table = BuildTable();

        public static uint Compute(byte[] data)
        {
            var crc = 0xFFFFFFFFu;

            foreach (var b in data)
            {
                var index = (byte)(crc ^ b);
                crc = (crc >> 8) ^ Table[index];
            }

            return crc ^ 0xFFFFFFFFu;
        }

        private static uint[] BuildTable()
        {
            const uint polynomial = 0xEDB88320u;
            var table = new uint[256];

            for (uint i = 0; i < 256; i++)
            {
                var value = i;

                for (var bit = 0; bit < 8; bit++)
                {
                    value = (value & 1) != 0 ? (value >> 1) ^ polynomial : value >> 1;
                }

                table[i] = value;
            }

            return table;
        }
    }
}
