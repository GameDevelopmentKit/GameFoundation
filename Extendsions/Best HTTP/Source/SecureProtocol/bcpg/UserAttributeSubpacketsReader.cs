#if !BESTHTTP_DISABLE_ALTERNATE_SSL && (!UNITY_WEBGL || UNITY_EDITOR)
#pragma warning disable
namespace BestHTTP.SecureProtocol.Org.BouncyCastle.Bcpg
{
    using System.IO;
    using BestHTTP.SecureProtocol.Org.BouncyCastle.Bcpg.Attr;
    using BestHTTP.SecureProtocol.Org.BouncyCastle.Utilities.IO;

    /**
    * reader for user attribute sub-packets
    */
    public class UserAttributeSubpacketsParser
    {
        private readonly Stream input;

        public UserAttributeSubpacketsParser(
            Stream input)
        {
            this.input = input;
        }

        public virtual UserAttributeSubpacket ReadPacket()
        {
            var l = this.input.ReadByte();
            if (l < 0)
                return null;

            var bodyLen    = 0;
            var longLength = false;
            if (l < 192)
            {
                bodyLen = l;
            }
            else if (l <= 223)
            {
                bodyLen = ((l - 192) << 8) + this.input.ReadByte() + 192;
            }
            else if (l == 255)
            {
                bodyLen = (this.input.ReadByte() << 24) | (this.input.ReadByte() << 16)
                                                        | (this.input.ReadByte() << 8) | this.input.ReadByte();
                longLength = true;
            }
            else
            {
                throw new IOException("unrecognised length reading user attribute sub packet");
            }

            var tag = this.input.ReadByte();
            if (tag < 0)
                throw new EndOfStreamException("unexpected EOF reading user attribute sub packet");

            var data = new byte[bodyLen - 1];
            if (Streams.ReadFully(this.input, data) < data.Length)
                throw new EndOfStreamException();

            var type = (UserAttributeSubpacketTag)tag;
            switch (type)
            {
                case UserAttributeSubpacketTag.ImageAttribute:
                    return new ImageAttrib(longLength, data);
            }

            return new UserAttributeSubpacket(type, longLength, data);
        }
    }
}
#pragma warning restore
#endif