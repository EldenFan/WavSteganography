using WavStefanogrpahyLib.Base;

namespace WavSteganographyLib.Form
{
    public class Header(uint size, MethodsType steganographyType)
    {
        #region Fields

        public const int SIZE = 5;

        private readonly uint dataSize = size;

        private readonly MethodsType type = steganographyType;

        #endregion

        #region Public property

        public uint DataSize => dataSize;

        public MethodsType Type => type;

        #endregion

        #region Public methods

        public static Header FromBytes(byte[] bytes)
        {
           var dataSize = BitConverter.ToUInt32(bytes);
           var type = (MethodsType)BitConverter.ToInt16(bytes);

           return new Header(dataSize, type);
        }

        public byte[] ToBytes()
        {
            var bytes = new byte[SIZE];

            Array.Copy(BitConverter.GetBytes(dataSize), bytes, 4);
            Array.Copy(BitConverter.GetBytes((short)type), 0, bytes, 4, 1);
            
            return bytes;
        }

        #endregion
    }
}
