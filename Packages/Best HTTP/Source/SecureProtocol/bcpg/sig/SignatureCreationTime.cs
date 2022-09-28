#if !BESTHTTP_DISABLE_ALTERNATE_SSL && (!UNITY_WEBGL || UNITY_EDITOR)
#pragma warning disable
namespace BestHTTP.SecureProtocol.Org.BouncyCastle.Bcpg.Sig
{
	using System;
	using BestHTTP.SecureProtocol.Org.BouncyCastle.Utilities.Date;

	/**
    * packet giving signature creation time.
    */
    public class SignatureCreationTime
        : SignatureSubpacket
    {
        protected static byte[] TimeToBytes(
            DateTime time)
        {
            var t    = DateTimeUtilities.DateTimeToUnixMs(time) / 1000L;
            var data = new byte[4];
            data[0] = (byte)(t >> 24);
            data[1] = (byte)(t >> 16);
            data[2] = (byte)(t >> 8);
            data[3] = (byte)t;
            return data;
        }

        public SignatureCreationTime(
            bool critical,
            bool isLongLength,
            byte[] data)
            : base(SignatureSubpacketTag.CreationTime, critical, isLongLength, data)
        {
        }

        public SignatureCreationTime(
            bool critical,
            DateTime date)
            : base(SignatureSubpacketTag.CreationTime, critical, false, TimeToBytes(date))
        {
        }

        public DateTime GetTime()
        {
            long time = ((uint)this.data[0] << 24)
                        | ((uint)this.data[1] << 16)
                        | ((uint)this.data[2] << 8)
                        | this.data[3];
            return DateTimeUtilities.UnixMsToDateTime(time * 1000L);
        }
    }
}
#pragma warning restore
#endif