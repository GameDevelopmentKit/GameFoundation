#if !BESTHTTP_DISABLE_ALTERNATE_SSL && (!UNITY_WEBGL || UNITY_EDITOR)
#pragma warning disable
namespace BestHTTP.SecureProtocol.Org.BouncyCastle.Tls.Crypto.Impl.BC
{
    using System.IO;
    using BestHTTP.SecureProtocol.Org.BouncyCastle.Crypto;
    using BestHTTP.SecureProtocol.Org.BouncyCastle.Crypto.Parameters;

    /// <summary>Support class for ephemeral Diffie-Hellman using the BC light-weight library.</summary>
    public class BcTlsDH
        : TlsAgreement
    {
        protected readonly BcTlsDHDomain m_domain;

        protected AsymmetricCipherKeyPair m_localKeyPair;
        protected DHPublicKeyParameters   m_peerPublicKey;

        public BcTlsDH(BcTlsDHDomain domain) { this.m_domain = domain; }

        /// <exception cref="IOException" />
        public virtual byte[] GenerateEphemeral()
        {
            this.m_localKeyPair = this.m_domain.GenerateKeyPair();

            return this.m_domain.EncodePublicKey((DHPublicKeyParameters)this.m_localKeyPair.Public);
        }

        /// <exception cref="IOException" />
        public virtual void ReceivePeerValue(byte[] peerValue) { this.m_peerPublicKey = this.m_domain.DecodePublicKey(peerValue); }

        /// <exception cref="IOException" />
        public virtual TlsSecret CalculateSecret() { return this.m_domain.CalculateDHAgreement((DHPrivateKeyParameters)this.m_localKeyPair.Private, this.m_peerPublicKey); }
    }
}
#pragma warning restore
#endif