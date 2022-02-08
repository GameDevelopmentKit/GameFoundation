#if !BESTHTTP_DISABLE_ALTERNATE_SSL && (!UNITY_WEBGL || UNITY_EDITOR)
#pragma warning disable
namespace BestHTTP.SecureProtocol.Org.BouncyCastle.Tls
{
    using System.Collections;
    using System.IO;
    using BestHTTP.SecureProtocol.Org.BouncyCastle.Tls.Crypto;

    public class SrpTlsServer
        : AbstractTlsServer
    {
        private static readonly int[] DefaultCipherSuites =
        {
            CipherSuite.TLS_SRP_SHA_DSS_WITH_AES_256_CBC_SHA,
            CipherSuite.TLS_SRP_SHA_DSS_WITH_AES_128_CBC_SHA,
            CipherSuite.TLS_SRP_SHA_RSA_WITH_AES_256_CBC_SHA,
            CipherSuite.TLS_SRP_SHA_RSA_WITH_AES_128_CBC_SHA,
            CipherSuite.TLS_SRP_SHA_WITH_AES_256_CBC_SHA,
            CipherSuite.TLS_SRP_SHA_WITH_AES_128_CBC_SHA
        };

        protected readonly TlsSrpIdentityManager m_srpIdentityManager;

        protected byte[]                m_srpIdentity;
        protected TlsSrpLoginParameters m_srpLoginParameters;

        public SrpTlsServer(TlsCrypto crypto, TlsSrpIdentityManager srpIdentityManager)
            : base(crypto)
        {
            this.m_srpIdentityManager = srpIdentityManager;
        }

        /// <exception cref="IOException" />
        protected virtual TlsCredentialedSigner GetDsaSignerCredentials() { throw new TlsFatalAlert(AlertDescription.internal_error); }

        /// <exception cref="IOException" />
        protected virtual TlsCredentialedSigner GetRsaSignerCredentials() { throw new TlsFatalAlert(AlertDescription.internal_error); }

        protected override ProtocolVersion[] GetSupportedVersions() { return ProtocolVersion.TLSv12.DownTo(ProtocolVersion.TLSv10); }

        protected override int[] GetSupportedCipherSuites() { return TlsUtilities.GetSupportedCipherSuites(this.Crypto, DefaultCipherSuites); }

        public override void ProcessClientExtensions(IDictionary clientExtensions)
        {
            base.ProcessClientExtensions(clientExtensions);

            this.m_srpIdentity = TlsSrpUtilities.GetSrpExtension(clientExtensions);
        }

        public override int GetSelectedCipherSuite()
        {
            var cipherSuite = base.GetSelectedCipherSuite();

            if (TlsSrpUtilities.IsSrpCipherSuite(cipherSuite))
            {
                if (this.m_srpIdentity != null) this.m_srpLoginParameters = this.m_srpIdentityManager.GetLoginParameters(this.m_srpIdentity);

                if (this.m_srpLoginParameters == null)
                    throw new TlsFatalAlert(AlertDescription.unknown_psk_identity);
            }

            return cipherSuite;
        }

        public override TlsCredentials GetCredentials()
        {
            var keyExchangeAlgorithm = this.m_context.SecurityParameters.KeyExchangeAlgorithm;

            switch (keyExchangeAlgorithm)
            {
                case KeyExchangeAlgorithm.SRP:
                    return null;

                case KeyExchangeAlgorithm.SRP_DSS:
                    return this.GetDsaSignerCredentials();

                case KeyExchangeAlgorithm.SRP_RSA:
                    return this.GetRsaSignerCredentials();

                default:
                    // Note: internal error here; selected a key exchange we don't implement!
                    throw new TlsFatalAlert(AlertDescription.internal_error);
            }
        }

        public override TlsSrpLoginParameters GetSrpLoginParameters() { return this.m_srpLoginParameters; }
    }
}
#pragma warning restore
#endif