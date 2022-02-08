#if !BESTHTTP_DISABLE_ALTERNATE_SSL && (!UNITY_WEBGL || UNITY_EDITOR)
#pragma warning disable
namespace BestHTTP.SecureProtocol.Org.BouncyCastle.Tls
{
    using System;
    using System.IO;
    using BestHTTP.SecureProtocol.Org.BouncyCastle.Tls.Crypto;
    using BestHTTP.SecureProtocol.Org.BouncyCastle.Utilities.IO;

    /// <summary>(D)TLS ECDHE key exchange (see RFC 4492).</summary>
    public class TlsECDheKeyExchange
        : AbstractTlsKeyExchange
    {
        private static int CheckKeyExchange(int keyExchange)
        {
            switch (keyExchange)
            {
                case KeyExchangeAlgorithm.ECDHE_ECDSA:
                case KeyExchangeAlgorithm.ECDHE_RSA:
                    return keyExchange;
                default:
                    throw new ArgumentException("unsupported key exchange algorithm", "keyExchange");
            }
        }

        protected TlsECConfig m_ecConfig;

        protected TlsCredentialedSigner m_serverCredentials;
        protected TlsCertificate        m_serverCertificate;
        protected TlsAgreement          m_agreement;

        public TlsECDheKeyExchange(int keyExchange)
            : this(keyExchange, null)
        {
        }

        public TlsECDheKeyExchange(int keyExchange, TlsECConfig ecConfig)
            : base(CheckKeyExchange(keyExchange))
        {
            this.m_ecConfig = ecConfig;
        }

        public override void SkipServerCredentials() { throw new TlsFatalAlert(AlertDescription.internal_error); }

        public override void ProcessServerCredentials(TlsCredentials serverCredentials) { this.m_serverCredentials = TlsUtilities.RequireSignerCredentials(serverCredentials); }

        public override void ProcessServerCertificate(Certificate serverCertificate) { this.m_serverCertificate = serverCertificate.GetCertificateAt(0); }

        public override bool RequiresServerKeyExchange => true;

        public override byte[] GenerateServerKeyExchange()
        {
            var digestBuffer = new DigestInputBuffer();

            TlsEccUtilities.WriteECConfig(this.m_ecConfig, digestBuffer);

            this.m_agreement = this.m_context.Crypto.CreateECDomain(this.m_ecConfig).CreateECDH();

            this.GenerateEphemeral(digestBuffer);

            TlsUtilities.GenerateServerKeyExchangeSignature(this.m_context, this.m_serverCredentials, null, digestBuffer);

            return digestBuffer.ToArray();
        }

        public override void ProcessServerKeyExchange(Stream input)
        {
            var    digestBuffer = new DigestInputBuffer();
            Stream teeIn        = new TeeInputStream(input, digestBuffer);

            this.m_ecConfig = TlsEccUtilities.ReceiveECDHConfig(this.m_context, teeIn);

            var point = TlsUtilities.ReadOpaque8(teeIn, 1);

            TlsUtilities.VerifyServerKeyExchangeSignature(this.m_context, input, this.m_serverCertificate, null, digestBuffer);

            this.m_agreement = this.m_context.Crypto.CreateECDomain(this.m_ecConfig).CreateECDH();

            this.ProcessEphemeral(point);
        }

        public override short[] GetClientCertificateTypes()
        {
            /*
             * RFC 4492 3. [...] The ECDSA_fixed_ECDH and RSA_fixed_ECDH mechanisms are usable with
             * ECDH_ECDSA and ECDH_RSA. Their use with ECDHE_ECDSA and ECDHE_RSA is prohibited because
             * the use of a long-term ECDH client key would jeopardize the forward secrecy property of
             * these algorithms.
             */
            return new[]
            {
                ClientCertificateType.dss_sign, ClientCertificateType.ecdsa_sign,
                ClientCertificateType.rsa_sign
            };
        }

        public override void ProcessClientCredentials(TlsCredentials clientCredentials) { TlsUtilities.RequireSignerCredentials(clientCredentials); }

        public override void GenerateClientKeyExchange(Stream output) { this.GenerateEphemeral(output); }

        public override void ProcessClientKeyExchange(Stream input)
        {
            var point = TlsUtilities.ReadOpaque8(input, 1);

            this.ProcessEphemeral(point);
        }

        public override TlsSecret GeneratePreMasterSecret() { return this.m_agreement.CalculateSecret(); }

        protected virtual void GenerateEphemeral(Stream output)
        {
            var point = this.m_agreement.GenerateEphemeral();

            TlsUtilities.WriteOpaque8(point, output);
        }

        protected virtual void ProcessEphemeral(byte[] point)
        {
            TlsEccUtilities.CheckPointEncoding(this.m_ecConfig.NamedGroup, point);

            this.m_agreement.ReceivePeerValue(point);
        }
    }
}
#pragma warning restore
#endif