#if !BESTHTTP_DISABLE_ALTERNATE_SSL && (!UNITY_WEBGL || UNITY_EDITOR)
#pragma warning disable
namespace BestHTTP.SecureProtocol.Org.BouncyCastle.Tls.Crypto.Impl.BC
{
    using BestHTTP.SecureProtocol.Org.BouncyCastle.Crypto;
    using BestHTTP.SecureProtocol.Org.BouncyCastle.Crypto.Parameters;

    /// <summary>Support class for ephemeral Elliptic Curve Diffie-Hellman using the BC light-weight library.</summary>
    public class BcTlsECDH
        : TlsAgreement
    {
        protected readonly BcTlsECDomain m_domain;

        protected AsymmetricCipherKeyPair m_localKeyPair;
        protected ECPublicKeyParameters   m_peerPublicKey;

        public BcTlsECDH(BcTlsECDomain domain) { this.m_domain = domain; }

        public virtual byte[] GenerateEphemeral()
        {
            this.m_localKeyPair = this.m_domain.GenerateKeyPair();

            return this.m_domain.EncodePublicKey((ECPublicKeyParameters)this.m_localKeyPair.Public);
        }

        public virtual void ReceivePeerValue(byte[] peerValue) { this.m_peerPublicKey = this.m_domain.DecodePublicKey(peerValue); }

        public virtual TlsSecret CalculateSecret() { return this.m_domain.CalculateECDHAgreement((ECPrivateKeyParameters)this.m_localKeyPair.Private, this.m_peerPublicKey); }
    }
}
#pragma warning restore
#endif