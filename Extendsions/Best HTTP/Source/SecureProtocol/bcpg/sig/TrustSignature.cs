#if !BESTHTTP_DISABLE_ALTERNATE_SSL && (!UNITY_WEBGL || UNITY_EDITOR)
#pragma warning disable
namespace BestHTTP.SecureProtocol.Org.BouncyCastle.Bcpg.Sig
{
    /**
    * packet giving trust.
    */
    public class TrustSignature
        : SignatureSubpacket
    {
        private static byte[] IntToByteArray(
            int v1,
            int v2)
        {
            return new[] { (byte)v1, (byte)v2 };
        }

        public TrustSignature(
            bool critical,
            bool isLongLength,
            byte[] data)
            : base(SignatureSubpacketTag.TrustSig, critical, isLongLength, data)
        {
        }

        public TrustSignature(
            bool critical,
            int depth,
            int trustAmount)
            : base(SignatureSubpacketTag.TrustSig, critical, false, IntToByteArray(depth, trustAmount))
        {
        }

        public int Depth => this.data[0] & 0xff;

        public int TrustAmount => this.data[1] & 0xff;
    }
}
#pragma warning restore
#endif