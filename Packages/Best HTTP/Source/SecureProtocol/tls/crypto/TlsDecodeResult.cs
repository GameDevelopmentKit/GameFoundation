#if !BESTHTTP_DISABLE_ALTERNATE_SSL && (!UNITY_WEBGL || UNITY_EDITOR)
#pragma warning disable
namespace BestHTTP.SecureProtocol.Org.BouncyCastle.Tls.Crypto
{
    public sealed class TlsDecodeResult
    {
        public readonly byte[] buf;
        public readonly int    off, len;
        public readonly short  contentType;

        public TlsDecodeResult(byte[] buf, int off, int len, short contentType)
        {
            this.buf         = buf;
            this.off         = off;
            this.len         = len;
            this.contentType = contentType;
        }
    }
}
#pragma warning restore
#endif