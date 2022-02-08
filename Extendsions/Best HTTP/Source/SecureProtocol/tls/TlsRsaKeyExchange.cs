#if !BESTHTTP_DISABLE_ALTERNATE_SSL && (!UNITY_WEBGL || UNITY_EDITOR)
#pragma warning disable
namespace BestHTTP.SecureProtocol.Org.BouncyCastle.Tls
{
    using System;
    using System.IO;
    using BestHTTP.SecureProtocol.Org.BouncyCastle.Tls.Crypto;

    /// <summary>(D)TLS RSA key exchange.</summary>
    public class TlsRsaKeyExchange
        : AbstractTlsKeyExchange
    {
        private static int CheckKeyExchange(int keyExchange)
        {
            switch (keyExchange)
            {
                case KeyExchangeAlgorithm.RSA:
                    return keyExchange;
                default:
                    throw new ArgumentException("unsupported key exchange algorithm", "keyExchange");
            }
        }

        protected TlsCredentialedDecryptor m_serverCredentials;
        protected TlsEncryptor             m_serverEncryptor;
        protected TlsSecret                m_preMasterSecret;

        public TlsRsaKeyExchange(int keyExchange)
            : base(CheckKeyExchange(keyExchange))
        {
        }

        public override void SkipServerCredentials() { throw new TlsFatalAlert(AlertDescription.internal_error); }

        public override void ProcessServerCredentials(TlsCredentials serverCredentials) { this.m_serverCredentials = TlsUtilities.RequireDecryptorCredentials(serverCredentials); }

        public override void ProcessServerCertificate(Certificate serverCertificate)
        {
            this.m_serverEncryptor = serverCertificate.GetCertificateAt(0).CreateEncryptor(
                TlsCertificateRole.RsaEncryption);
        }

        public override short[] GetClientCertificateTypes()
        {
            return new[]
            {
                ClientCertificateType.rsa_sign, ClientCertificateType.dss_sign,
                ClientCertificateType.ecdsa_sign
            };
        }

        public override void ProcessClientCredentials(TlsCredentials clientCredentials) { TlsUtilities.RequireSignerCredentials(clientCredentials); }

        public override void GenerateClientKeyExchange(Stream output)
        {
            this.m_preMasterSecret = TlsUtilities.GenerateEncryptedPreMasterSecret(this.m_context, this.m_serverEncryptor,
                output);
        }

        public override void ProcessClientKeyExchange(Stream input)
        {
            var encryptedPreMasterSecret = TlsUtilities.ReadEncryptedPms(this.m_context, input);

            this.m_preMasterSecret = this.m_serverCredentials.Decrypt(new TlsCryptoParameters(this.m_context),
                encryptedPreMasterSecret);
        }

        public override TlsSecret GeneratePreMasterSecret()
        {
            var tmp = this.m_preMasterSecret;
            this.m_preMasterSecret = null;
            return tmp;
        }
    }
}
#pragma warning restore
#endif