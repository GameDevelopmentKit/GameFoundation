#if !BESTHTTP_DISABLE_ALTERNATE_SSL && (!UNITY_WEBGL || UNITY_EDITOR)
#pragma warning disable
namespace BestHTTP.SecureProtocol.Org.BouncyCastle.Bcpg.Sig
{
    /**
    * packet giving signature expiration time.
    */
    public class SignatureExpirationTime
        : SignatureSubpacket
    {
        protected static byte[] TimeToBytes(
            long t)
        {
            var data = new byte[4];
            data[0] = (byte)(t >> 24);
            data[1] = (byte)(t >> 16);
            data[2] = (byte)(t >> 8);
            data[3] = (byte)t;
            return data;
        }

        public SignatureExpirationTime(
            bool critical,
            bool isLongLength,
            byte[] data)
            : base(SignatureSubpacketTag.ExpireTime, critical, isLongLength, data)
        {
        }

        public SignatureExpirationTime(
            bool critical,
            long seconds)
            : base(SignatureSubpacketTag.ExpireTime, critical, false, TimeToBytes(seconds))
        {
        }

        /**
        * return time in seconds before signature expires after creation time.
        */
        public long Time
        {
            get
            {
                var time = ((long)(this.data[0] & 0xff) << 24) | ((long)(this.data[1] & 0xff) << 16)
                                                               | ((long)(this.data[2] & 0xff) << 8) | ((long)this.data[3] & 0xff);

                return time;
            }
        }
    }
}
#pragma warning restore
#endif