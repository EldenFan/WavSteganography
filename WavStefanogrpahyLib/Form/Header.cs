using WavStefanogrpahyLib.Base;

namespace WavSteganographyLib.Form
{
    public class Header(uint size, uint crc)
    {
        #region Fields

        public const int SIZE = 8;

        private readonly uint dataSize = size;

        private readonly uint crc = crc;

        #endregion

        #region Public property

        public uint DataSize => dataSize;

        public uint Crc => crc;

        #endregion

        #region Public methods

        public static Header FromBytes(byte[] bytes)
        {
           var dataSize = BitConverter.ToUInt32(bytes);
           var crc = BitConverter.ToUInt32(bytes, 4);

           return new Header(dataSize, crc);
        }

        public byte[] ToBytes()
        {
            var bytes = new byte[SIZE];

            Array.Copy(BitConverter.GetBytes(dataSize), bytes, 4);
            Array.Copy(BitConverter.GetBytes(crc), 0, bytes, 4, 4);
            
            return bytes;
        }

        #endregion
    }
}
