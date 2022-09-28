#if !BESTHTTP_DISABLE_ALTERNATE_SSL && (!UNITY_WEBGL || UNITY_EDITOR)
#pragma warning disable
namespace BestHTTP.SecureProtocol.Org.BouncyCastle.Tls.Crypto.Impl.BC
{
    using BestHTTP.SecureProtocol.Org.BouncyCastle.Crypto.Macs;
    using BestHTTP.SecureProtocol.Org.BouncyCastle.Crypto.Parameters;

    internal sealed class BcTlsHmac
        : TlsHmac
    {
        private readonly HMac m_hmac;

        internal BcTlsHmac(HMac hmac) { this.m_hmac = hmac; }

        public void SetKey(byte[] key, int keyOff, int keyLen) { this.m_hmac.Init(new KeyParameter(key, keyOff, keyLen)); }

        public void Update(byte[] input, int inOff, int length) { this.m_hmac.BlockUpdate(input, inOff, length); }

        public byte[] CalculateMac()
        {
            var rv = new byte[this.m_hmac.GetMacSize()];
            this.m_hmac.DoFinal(rv, 0);
            return rv;
        }

        public void CalculateMac(byte[] output, int outOff) { this.m_hmac.DoFinal(output, outOff); }

        public int InternalBlockSize => this.m_hmac.GetUnderlyingDigest().GetByteLength();

        public int MacLength => this.m_hmac.GetMacSize();

        public void Reset() { this.m_hmac.Reset(); }
    }
}
#pragma warning restore
#endif