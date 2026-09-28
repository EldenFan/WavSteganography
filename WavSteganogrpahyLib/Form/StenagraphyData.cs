using System.Text;
using WavSteganographyLib.Utils;

namespace WavSteganographyLib.Form
{
    public class StenagraphyData
    {
        #region Fields

        private readonly Header header;

        private readonly byte[] data;

        #endregion

        #region Constructor

        public StenagraphyData(string dataToHide)
        {
            data = Encoding.UTF8.GetBytes(dataToHide);

            var crc = Crc32.Compute(data);

            header = new Header((uint)data.Length, crc);
        }

        private StenagraphyData(Header header, byte[] data)
        {
            this.header = header;
            this.data = data;
        }

        #endregion

        #region Public Property

        public int Size => data.Length + Header.SIZE;

        public byte[] Data => data;

        #endregion

        #region Public methods

        public static StenagraphyData FromBytes(byte[] bytes)
        {
            var header = Header.FromBytes(bytes);
            var data = new byte[header.DataSize];

            Array.Copy(bytes, Header.SIZE, data, 0, header.DataSize);

            var actualCrc = Crc32.Compute(data);
            if (actualCrc != header.Crc)
            {
                throw new InvalidDataException("Контрольная сумма CRC не совпадает: извлечённые данные повреждены");
            }

            return new StenagraphyData(header, data);
        }

        public byte[] ToBytes()
        {
            var bytes = new byte[Size];

            Array.Copy(header.ToBytes(), 0, bytes, 0, Header.SIZE);
            Array.Copy(data, 0, bytes, Header.SIZE, data.Length);

            return bytes;
        }

        #endregion
    }
}
