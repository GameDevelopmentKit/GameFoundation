#if !BESTHTTP_DISABLE_ALTERNATE_SSL && (!UNITY_WEBGL || UNITY_EDITOR)
#pragma warning disable
namespace BestHTTP.SecureProtocol.Org.BouncyCastle.Tls.Crypto
{
    using BestHTTP.SecureProtocol.Org.BouncyCastle.Utilities.IO;

    public class TlsMacSink
        : BaseOutputStream
    {
        private readonly TlsMac m_mac;

        public TlsMacSink(TlsMac mac) { this.m_mac = mac; }

        public virtual TlsMac Mac => this.m_mac;

        public override void WriteByte(byte b) { this.m_mac.Update(new[] { b }, 0, 1); }

        public override void Write(byte[] buf, int off, int len)
        {
            if (len > 0) this.m_mac.Update(buf, off, len);
        }
    }
}
#pragma warning restore
#endif