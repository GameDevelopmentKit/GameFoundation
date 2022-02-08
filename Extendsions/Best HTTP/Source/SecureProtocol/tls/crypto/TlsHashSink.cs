#if !BESTHTTP_DISABLE_ALTERNATE_SSL && (!UNITY_WEBGL || UNITY_EDITOR)
#pragma warning disable
namespace BestHTTP.SecureProtocol.Org.BouncyCastle.Tls.Crypto
{
    using BestHTTP.SecureProtocol.Org.BouncyCastle.Utilities.IO;

    public class TlsHashSink
        : BaseOutputStream
    {
        private readonly TlsHash m_hash;

        public TlsHashSink(TlsHash hash) { this.m_hash = hash; }

        public virtual TlsHash Hash => this.m_hash;

        public override void WriteByte(byte b) { this.m_hash.Update(new[] { b }, 0, 1); }

        public override void Write(byte[] buf, int off, int len)
        {
            if (len > 0) this.m_hash.Update(buf, off, len);
        }
    }
}
#pragma warning restore
#endif