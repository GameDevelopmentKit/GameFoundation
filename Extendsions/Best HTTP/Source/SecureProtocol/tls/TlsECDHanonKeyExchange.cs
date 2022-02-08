#if !BESTHTTP_DISABLE_ALTERNATE_SSL && (!UNITY_WEBGL || UNITY_EDITOR)
#pragma warning disable
namespace BestHTTP.SecureProtocol.Org.BouncyCastle.Tls
{
    using System;
    using System.IO;
    using BestHTTP.SecureProtocol.Org.BouncyCastle.Tls.Crypto;

    /// <summary>(D)TLS ECDH_anon key exchange (see RFC 4492).</summary>
    public class TlsECDHanonKeyExchange
        : AbstractTlsKeyExchange
    {
        private static int CheckKeyExchange(int keyExchange)
        {
            switch (keyExchange)
            {
                case KeyExchangeAlgorithm.ECDH_anon:
                    return keyExchange;
                default:
                    throw new ArgumentException("unsupported key exchange algorithm", "keyExchange");
            }
        }

        protected TlsECConfig m_ecConfig;

        protected TlsAgreement m_agreement;

        public TlsECDHanonKeyExchange(int keyExchange)
            : this(keyExchange, null)
        {
        }

        public TlsECDHanonKeyExchange(int keyExchange, TlsECConfig ecConfig)
            : base(CheckKeyExchange(keyExchange))
        {
            this.m_ecConfig = ecConfig;
        }

        public override void SkipServerCredentials() { }

        public override void ProcessServerCredentials(TlsCredentials serverCredentials) { throw new TlsFatalAlert(AlertDescription.internal_error); }

        public override void ProcessServerCertificate(Certificate serverCertificate) { throw new TlsFatalAlert(AlertDescription.unexpected_message); }

        public override bool RequiresServerKeyExchange => true;

        public override byte[] GenerateServerKeyExchange()
        {
            var buf = new MemoryStream();

            TlsEccUtilities.WriteECConfig(this.m_ecConfig, buf);

            this.m_agreement = this.m_context.Crypto.CreateECDomain(this.m_ecConfig).CreateECDH();

            this.GenerateEphemeral(buf);

            return buf.ToArray();
        }

        public override void ProcessServerKeyExchange(Stream input)
        {
            this.m_ecConfig = TlsEccUtilities.ReceiveECDHConfig(this.m_context, input);

            var point = TlsUtilities.ReadOpaque8(input, 1);

            this.m_agreement = this.m_context.Crypto.CreateECDomain(this.m_ecConfig).CreateECDH();

            this.ProcessEphemeral(point);
        }

        public override short[] GetClientCertificateTypes() { return null; }

        public override void ProcessClientCredentials(TlsCredentials clientCredentials) { throw new TlsFatalAlert(AlertDescription.internal_error); }

        public override void GenerateClientKeyExchange(Stream output) { this.GenerateEphemeral(output); }

        public override void ProcessClientCertificate(Certificate clientCertificate) { throw new TlsFatalAlert(AlertDescription.unexpected_message); }

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