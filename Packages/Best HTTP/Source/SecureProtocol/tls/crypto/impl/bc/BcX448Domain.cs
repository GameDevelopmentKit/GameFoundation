#if !BESTHTTP_DISABLE_ALTERNATE_SSL && (!UNITY_WEBGL || UNITY_EDITOR)
#pragma warning disable
namespace BestHTTP.SecureProtocol.Org.BouncyCastle.Tls.Crypto.Impl.BC
{
    public class BcX448Domain
        : TlsECDomain
    {
        protected readonly BcTlsCrypto m_crypto;

        public BcX448Domain(BcTlsCrypto crypto) { this.m_crypto = crypto; }

        public virtual TlsAgreement CreateECDH() { return new BcX448(this.m_crypto); }
    }
}
#pragma warning restore
#endif