#if !BESTHTTP_DISABLE_ALTERNATE_SSL && (!UNITY_WEBGL || UNITY_EDITOR)
#pragma warning disable
namespace BestHTTP.SecureProtocol.Org.BouncyCastle.Tls
{
    using System;
    using System.IO;
    using BestHTTP.SecureProtocol.Org.BouncyCastle.Tls.Crypto;
    using BestHTTP.SecureProtocol.Org.BouncyCastle.Utilities.IO;

    public class TlsDheKeyExchange
        : AbstractTlsKeyExchange
    {
        private static int CheckKeyExchange(int keyExchange)
        {
            switch (keyExchange)
            {
                case KeyExchangeAlgorithm.DHE_DSS:
                case KeyExchangeAlgorithm.DHE_RSA:
                    return keyExchange;
                default:
                    throw new ArgumentException("unsupported key exchange algorithm", "keyExchange");
            }
        }

        protected TlsDHGroupVerifier m_dhGroupVerifier;
        protected TlsDHConfig        m_dhConfig;

        protected TlsCredentialedSigner m_serverCredentials;
        protected TlsCertificate        m_serverCertificate;
        protected TlsAgreement          m_agreement;

        public TlsDheKeyExchange(int keyExchange, TlsDHGroupVerifier dhGroupVerifier)
            : this(keyExchange, dhGroupVerifier, null)
        {
        }

        public TlsDheKeyExchange(int keyExchange, TlsDHConfig dhConfig)
            : this(keyExchange, null, dhConfig)
        {
        }

        private TlsDheKeyExchange(int keyExchange, TlsDHGroupVerifier dhGroupVerifier, TlsDHConfig dhConfig)
            : base(CheckKeyExchange(keyExchange))
        {
            this.m_dhGroupVerifier = dhGroupVerifier;
            this.m_dhConfig        = dhConfig;
        }

        public override void SkipServerCredentials() { throw new TlsFatalAlert(AlertDescription.internal_error); }

        public override void ProcessServerCredentials(TlsCredentials serverCredentials) { this.m_serverCredentials = TlsUtilities.RequireSignerCredentials(serverCredentials); }

        public override void ProcessServerCertificate(Certificate serverCertificate) { this.m_serverCertificate = serverCertificate.GetCertificateAt(0); }

        public override bool RequiresServerKeyExchange => true;

        public override byte[] GenerateServerKeyExchange()
        {
            var digestBuffer = new DigestInputBuffer();

            TlsDHUtilities.WriteDHConfig(this.m_dhConfig, digestBuffer);

            this.m_agreement = this.m_context.Crypto.CreateDHDomain(this.m_dhConfig).CreateDH();

            var y = this.m_agreement.GenerateEphemeral();

            TlsUtilities.WriteOpaque16(y, digestBuffer);

            TlsUtilities.GenerateServerKeyExchangeSignature(this.m_context, this.m_serverCredentials, null, digestBuffer);

            return digestBuffer.ToArray();
        }

        public override void ProcessServerKeyExchange(Stream input)
        {
            var    digestBuffer = new DigestInputBuffer();
            Stream teeIn        = new TeeInputStream(input, digestBuffer);

            this.m_dhConfig = TlsDHUtilities.ReceiveDHConfig(this.m_context, this.m_dhGroupVerifier, teeIn);

            var y = TlsUtilities.ReadOpaque16(teeIn, 1);

            TlsUtilities.VerifyServerKeyExchangeSignature(this.m_context, input, this.m_serverCertificate, null, digestBuffer);

            this.m_agreement = this.m_context.Crypto.CreateDHDomain(this.m_dhConfig).CreateDH();

            this.m_agreement.ReceivePeerValue(y);
        }

        public override short[] GetClientCertificateTypes()
        {
            return new[]
            {
                ClientCertificateType.dss_sign, ClientCertificateType.ecdsa_sign,
                ClientCertificateType.rsa_sign
            };
        }

        public override void ProcessClientCredentials(TlsCredentials clientCredentials) { TlsUtilities.RequireSignerCredentials(clientCredentials); }

        public override void GenerateClientKeyExchange(Stream output)
        {
            var y = this.m_agreement.GenerateEphemeral();

            TlsUtilities.WriteOpaque16(y, output);
        }

        public override void ProcessClientKeyExchange(Stream input) { this.m_agreement.ReceivePeerValue(TlsUtilities.ReadOpaque16(input, 1)); }

        public override TlsSecret GeneratePreMasterSecret() { return this.m_agreement.CalculateSecret(); }
    }
}
#pragma warning restore
#endif