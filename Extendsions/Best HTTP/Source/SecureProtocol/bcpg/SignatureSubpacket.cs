#if !BESTHTTP_DISABLE_ALTERNATE_SSL && (!UNITY_WEBGL || UNITY_EDITOR)
#pragma warning disable
namespace BestHTTP.SecureProtocol.Org.BouncyCastle.Bcpg
{
    using System.IO;
    using BestHTTP.SecureProtocol.Org.BouncyCastle.Utilities;

    /// <remarks>Basic type for a PGP Signature sub-packet.</remarks>
    public class SignatureSubpacket
    {
        private readonly bool   critical;
        private readonly bool   isLongLength;
        internal         byte[] data;

        protected internal SignatureSubpacket(
            SignatureSubpacketTag type,
            bool critical,
            bool isLongLength,
            byte[] data)
        {
            this.SubpacketType = type;
            this.critical      = critical;
            this.isLongLength  = isLongLength;
            this.data          = data;
        }

        public SignatureSubpacketTag SubpacketType { get; }

        public bool IsCritical() { return this.critical; }

        public bool IsLongLength() { return this.isLongLength; }

        /// <summary>Return the generic data making up the packet.</summary>
        public byte[] GetData() { return (byte[])this.data.Clone(); }

        public void Encode(
            Stream os)
        {
            var bodyLen = this.data.Length + 1;

            if (this.isLongLength)
            {
                os.WriteByte(0xff);
                os.WriteByte((byte)(bodyLen >> 24));
                os.WriteByte((byte)(bodyLen >> 16));
                os.WriteByte((byte)(bodyLen >> 8));
                os.WriteByte((byte)bodyLen);
            }
            else
            {
                if (bodyLen < 192)
                {
                    os.WriteByte((byte)bodyLen);
                }
                else if (bodyLen <= 8383)
                {
                    bodyLen -= 192;

                    os.WriteByte((byte)(((bodyLen >> 8) & 0xff) + 192));
                    os.WriteByte((byte)bodyLen);
                }
                else
                {
                    os.WriteByte(0xff);
                    os.WriteByte((byte)(bodyLen >> 24));
                    os.WriteByte((byte)(bodyLen >> 16));
                    os.WriteByte((byte)(bodyLen >> 8));
                    os.WriteByte((byte)bodyLen);
                }
            }

            if (this.critical)
                os.WriteByte((byte)(0x80 | (int)this.SubpacketType));
            else
                os.WriteByte((byte)this.SubpacketType);

            os.Write(this.data, 0, this.data.Length);
        }

        public override int GetHashCode() { return (this.critical ? 1 : 0) + 7 * (int)this.SubpacketType + 49 * Arrays.GetHashCode(this.data); }

        public override bool Equals(object obj)
        {
            if (obj == this)
                return true;

            var other = obj as SignatureSubpacket;
            if (null == other)
                return false;

            return this.SubpacketType == other.SubpacketType
                   && this.critical == other.critical
                   && Arrays.AreEqual(this.data, other.data);
        }
    }
}
#pragma warning restore
#endif