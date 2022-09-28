#if !BESTHTTP_DISABLE_ALTERNATE_SSL && (!UNITY_WEBGL || UNITY_EDITOR)
#pragma warning disable
namespace BestHTTP.SecureProtocol.Org.BouncyCastle.Tls
{
    using System;
    using System.IO;
    using BestHTTP.SecureProtocol.Org.BouncyCastle.Tls.Crypto;

    /// <summary>(D)TLS DH_anon key exchange.</summary>
    public class TlsDHanonKeyExchange
        : AbstractTlsKeyExchange
    {
        private static int CheckKeyExchange(int keyExchange)
        {
            switch (keyExchange)
            {
                case KeyExchangeAlgorithm.DH_anon:
                    return keyExchange;
                default:
                    throw new ArgumentException("unsupported key exchange algorithm", "keyExchange");
            }
        }

        protected TlsDHGroupVerifier m_dhGroupVerifier;
        protected TlsDHConfig        m_dhConfig;

        protected TlsAgreement m_agreement;

        public TlsDHanonKeyExchange(int keyExchange, TlsDHGroupVerifier dhGroupVerifier)
            : this(keyExchange, dhGroupVerifier, null)
        {
        }

        public TlsDHanonKeyExchange(int keyExchange, TlsDHConfig dhConfig)
            : this(keyExchange, null, dhConfig)
        {
        }

        private TlsDHanonKeyExchange(int keyExchange, TlsDHGroupVerifier dhGroupVerifier, TlsDHConfig dhConfig)
            : base(CheckKeyExchange(keyExchange))
        {
            this.m_dhGroupVerifier = dhGroupVerifier;
            this.m_dhConfig        = dhConfig;
        }

        public override void SkipServerCredentials() { }

        public override void ProcessServerCredentials(TlsCredentials serverCredentials) { throw new TlsFatalAlert(AlertDescription.internal_error); }

        public override void ProcessServerCertificate(Certificate serverCertificate) { throw new TlsFatalAlert(AlertDescription.unexpected_message); }

        public override bool RequiresServerKeyExchange => true;

        public override byte[] GenerateServerKeyExchange()
        {
            var buf = new MemoryStream();

            TlsDHUtilities.WriteDHConfig(this.m_dhConfig, buf);

            this.m_agreement = this.m_context.Crypto.CreateDHDomain(this.m_dhConfig).CreateDH();

            var y = this.m_agreement.GenerateEphemeral();

            TlsUtilities.WriteOpaque16(y, buf);

            return buf.ToArray();
        }

        public override void ProcessServerKeyExchange(Stream input)
        {
            this.m_dhConfig = TlsDHUtilities.ReceiveDHConfig(this.m_context, this.m_dhGroupVerifier, input);

            var y = TlsUtilities.ReadOpaque16(input, 1);

            this.m_agreement = this.m_context.Crypto.CreateDHDomain(this.m_dhConfig).CreateDH();

            this.m_agreement.ReceivePeerValue(y);
        }

        public override short[] GetClientCertificateTypes() { return null; }

        public override void ProcessClientCredentials(TlsCredentials clientCredentials) { throw new TlsFatalAlert(AlertDescription.internal_error); }

        public override void GenerateClientKeyExchange(Stream output)
        {
            var y = this.m_agreement.GenerateEphemeral();

            TlsUtilities.WriteOpaque16(y, output);
        }

        public override void ProcessClientCertificate(Certificate clientCertificate) { throw new TlsFatalAlert(AlertDescription.unexpected_message); }

        public override void ProcessClientKeyExchange(Stream input)
        {
            var y = TlsUtilities.ReadOpaque16(input, 1);

            this.m_agreement.ReceivePeerValue(y);
        }

        public override TlsSecret GeneratePreMasterSecret() { return this.m_agreement.CalculateSecret(); }
    }
}
#pragma warning restore
#endif