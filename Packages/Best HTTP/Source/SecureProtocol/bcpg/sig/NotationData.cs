#if !BESTHTTP_DISABLE_ALTERNATE_SSL && (!UNITY_WEBGL || UNITY_EDITOR)
#pragma warning disable
namespace BestHTTP.SecureProtocol.Org.BouncyCastle.Bcpg.Sig
{
	using System;
	using System.IO;
	using System.Text;

	/**
     * Class provided a NotationData object according to
     * RFC2440, Chapter 5.2.3.15. Notation Data
     */
    public class NotationData
        : SignatureSubpacket
    {
        public const int HeaderFlagLength  = 4;
        public const int HeaderNameLength  = 2;
        public const int HeaderValueLength = 2;

        public NotationData(
            bool critical,
            bool isLongLength,
            byte[] data)
            : base(SignatureSubpacketTag.NotationData, critical, isLongLength, data)
        {
        }

        public NotationData(
            bool critical,
            bool humanReadable,
            string notationName,
            string notationValue)
            : base(SignatureSubpacketTag.NotationData, critical, false,
                CreateData(humanReadable, notationName, notationValue))
        {
        }

        private static byte[] CreateData(
            bool humanReadable,
            string notationName,
            string notationValue)
        {
            var os = new MemoryStream();

            // (4 octets of flags, 2 octets of name length (M),
            // 2 octets of value length (N),
            // M octets of name data,
            // N octets of value data)

            // flags
            os.WriteByte(humanReadable ? (byte)0x80 : (byte)0x00);
            os.WriteByte(0x0);
            os.WriteByte(0x0);
            os.WriteByte(0x0);

            byte[] nameData,   valueData = null;
            int    nameLength, valueLength;

            nameData   = Encoding.UTF8.GetBytes(notationName);
            nameLength = Math.Min(nameData.Length, 0xFF);

            valueData   = Encoding.UTF8.GetBytes(notationValue);
            valueLength = Math.Min(valueData.Length, 0xFF);

            // name length
            os.WriteByte((byte)(nameLength >> 8));
            os.WriteByte((byte)(nameLength >> 0));

            // value length
            os.WriteByte((byte)(valueLength >> 8));
            os.WriteByte((byte)(valueLength >> 0));

            // name
            os.Write(nameData, 0, nameLength);

            // value
            os.Write(valueData, 0, valueLength);

            return os.ToArray();
        }

        public bool IsHumanReadable => this.data[0] == 0x80;

        public string GetNotationName()
        {
            var nameLength = (this.data[HeaderFlagLength] << 8) + (this.data[HeaderFlagLength + 1] << 0);
            var namePos    = HeaderFlagLength + HeaderNameLength + HeaderValueLength;

            return Encoding.UTF8.GetString(this.data, namePos, nameLength);
        }

        public string GetNotationValue()
        {
            var nameLength  = (this.data[HeaderFlagLength] << 8) + (this.data[HeaderFlagLength + 1] << 0);
            var valueLength = (this.data[HeaderFlagLength + HeaderNameLength] << 8) + (this.data[HeaderFlagLength + HeaderNameLength + 1] << 0);
            var valuePos    = HeaderFlagLength + HeaderNameLength + HeaderValueLength + nameLength;

            return Encoding.UTF8.GetString(this.data, valuePos, valueLength);
        }

        public byte[] GetNotationValueBytes()
        {
            var nameLength  = (this.data[HeaderFlagLength] << 8) + (this.data[HeaderFlagLength + 1] << 0);
            var valueLength = (this.data[HeaderFlagLength + HeaderNameLength] << 8) + (this.data[HeaderFlagLength + HeaderNameLength + 1] << 0);
            var valuePos    = HeaderFlagLength + HeaderNameLength + HeaderValueLength + nameLength;

            var bytes = new byte[valueLength];
            Array.Copy(this.data, valuePos, bytes, 0, valueLength);
            return bytes;
        }
    }
}
#pragma warning restore
#endif