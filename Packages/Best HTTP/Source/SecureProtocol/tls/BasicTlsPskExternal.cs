#if !BESTHTTP_DISABLE_ALTERNATE_SSL && (!UNITY_WEBGL || UNITY_EDITOR)
#pragma warning disable
namespace BestHTTP.SecureProtocol.Org.BouncyCastle.Tls
{
    using BestHTTP.SecureProtocol.Org.BouncyCastle.Tls.Crypto;
    using BestHTTP.SecureProtocol.Org.BouncyCastle.Utilities;

    public class BasicTlsPskExternal
        : TlsPskExternal
    {
        protected readonly byte[]    m_identity;
        protected readonly TlsSecret m_key;
        protected readonly int       m_prfAlgorithm;

        public BasicTlsPskExternal(byte[] identity, TlsSecret key)
            : this(identity, key, Tls.PrfAlgorithm.tls13_hkdf_sha256)
        {
        }

        public BasicTlsPskExternal(byte[] identity, TlsSecret key, int prfAlgorithm)
        {
            this.m_identity     = Arrays.Clone(identity);
            this.m_key          = key;
            this.m_prfAlgorithm = prfAlgorithm;
        }

        public virtual byte[] Identity => this.m_identity;

        public virtual TlsSecret Key => this.m_key;

        public virtual int PrfAlgorithm => this.m_prfAlgorithm;
    }
}
#pragma warning restore
#endif