#if !BESTHTTP_DISABLE_ALTERNATE_SSL && (!UNITY_WEBGL || UNITY_EDITOR)
#pragma warning disable
namespace BestHTTP.SecureProtocol.Org.BouncyCastle.Bcpg
{
    using System.IO;
    using BestHTTP.SecureProtocol.Org.BouncyCastle.Utilities;

    /**
    * Basic type for a user attribute sub-packet.
    */
    public class UserAttributeSubpacket
    {
        internal readonly  UserAttributeSubpacketTag type;
        private readonly   bool                      longLength; // we preserve this as not everyone encodes length properly.
        protected readonly byte[]                    data;

        protected internal UserAttributeSubpacket(UserAttributeSubpacketTag type, byte[] data)
            : this(type, false, data)
        {
        }

        protected internal UserAttributeSubpacket(UserAttributeSubpacketTag type, bool forceLongLength, byte[] data)
        {
            this.type       = type;
            this.longLength = forceLongLength;
            this.data       = data;
        }

        public virtual UserAttributeSubpacketTag SubpacketType => this.type;

        /**
        * return the generic data making up the packet.
        */
        public virtual byte[] GetData() { return this.data; }

        public virtual void Encode(Stream os)
        {
            var bodyLen = this.data.Length + 1;

            if (bodyLen < 192 && !this.longLength)
            {
                os.WriteByte((byte)bodyLen);
            }
            else if (bodyLen <= 8383 && !this.longLength)
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

            os.WriteByte((byte)this.type);
            os.Write(this.data, 0, this.data.Length);
        }

        public override bool Equals(
            object obj)
        {
            if (obj == this)
                return true;

            var other = obj as UserAttributeSubpacket;

            if (other == null)
                return false;

            return this.type == other.type
                   && Arrays.AreEqual(this.data, other.data);
        }

        public override int GetHashCode() { return this.type.GetHashCode() ^ Arrays.GetHashCode(this.data); }
    }
}
#pragma warning restore
#endif