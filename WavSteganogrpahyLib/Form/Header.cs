namespace WavSteganographyLib.Form
{
    public class Header(ushort size, ushort crc)
    {
        #region Fields

        public const int SIZE = 4;

        private readonly ushort dataSize = size;

        private readonly ushort crc = crc;

        #endregion

        #region Public property

        public ushort DataSize => dataSize;

        public ushort Crc => crc;

        #endregion

        #region Public methods

        public static Header FromBytes(byte[] bytes)
        {
           var dataSize = BitConverter.ToUInt16(bytes);
           var crc = BitConverter.ToUInt16(bytes, 2);

           return new Header(dataSize, crc);
        }

        public byte[] ToBytes()
        {
            var bytes = new byte[SIZE];

            Array.Copy(BitConverter.GetBytes(dataSize), bytes, 2);
            Array.Copy(BitConverter.GetBytes(crc), 0, bytes, 2, 2);
            
            return bytes;
        }

        #endregion
    }
}
