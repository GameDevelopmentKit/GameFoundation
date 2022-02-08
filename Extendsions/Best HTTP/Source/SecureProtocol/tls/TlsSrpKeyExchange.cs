#if !BESTHTTP_DISABLE_ALTERNATE_SSL && (!UNITY_WEBGL || UNITY_EDITOR)
#pragma warning disable
namespace BestHTTP.SecureProtocol.Org.BouncyCastle.Tls
{
    using System;
    using System.IO;
    using BestHTTP.SecureProtocol.Org.BouncyCastle.Math;
    using BestHTTP.SecureProtocol.Org.BouncyCastle.Tls.Crypto;
    using BestHTTP.SecureProtocol.Org.BouncyCastle.Utilities;
    using BestHTTP.SecureProtocol.Org.BouncyCastle.Utilities.IO;

    /// <summary>(D)TLS SRP key exchange (RFC 5054).</summary>
    public class TlsSrpKeyExchange
        : AbstractTlsKeyExchange
    {
        private static int CheckKeyExchange(int keyExchange)
        {
            switch (keyExchange)
            {
                case KeyExchangeAlgorithm.SRP:
                case KeyExchangeAlgorithm.SRP_DSS:
                case KeyExchangeAlgorithm.SRP_RSA:
                    return keyExchange;
                default:
                    throw new ArgumentException("unsupported key exchange algorithm", "keyExchange");
            }
        }

        protected TlsSrpIdentity       m_srpIdentity;
        protected TlsSrpConfigVerifier m_srpConfigVerifier;
        protected TlsCertificate       m_serverCertificate;
        protected byte[]               m_srpSalt;
        protected TlsSrp6Client        m_srpClient;

        protected TlsSrpLoginParameters m_srpLoginParameters;
        protected TlsCredentialedSigner m_serverCredentials;
        protected TlsSrp6Server         m_srpServer;

        protected BigInteger m_srpPeerCredentials;

        public TlsSrpKeyExchange(int keyExchange, TlsSrpIdentity srpIdentity, TlsSrpConfigVerifier srpConfigVerifier)
            : base(CheckKeyExchange(keyExchange))
        {
            this.m_srpIdentity       = srpIdentity;
            this.m_srpConfigVerifier = srpConfigVerifier;
        }

        public TlsSrpKeyExchange(int keyExchange, TlsSrpLoginParameters srpLoginParameters)
            : base(CheckKeyExchange(keyExchange))
        {
            this.m_srpLoginParameters = srpLoginParameters;
        }

        public override void SkipServerCredentials()
        {
            if (this.m_keyExchange != KeyExchangeAlgorithm.SRP)
                throw new TlsFatalAlert(AlertDescription.internal_error);
        }

        public override void ProcessServerCredentials(TlsCredentials serverCredentials)
        {
            if (this.m_keyExchange == KeyExchangeAlgorithm.SRP)
                throw new TlsFatalAlert(AlertDescription.internal_error);

            this.m_serverCredentials = TlsUtilities.RequireSignerCredentials(serverCredentials);
        }

        public override void ProcessServerCertificate(Certificate serverCertificate)
        {
            if (this.m_keyExchange == KeyExchangeAlgorithm.SRP)
                throw new TlsFatalAlert(AlertDescription.internal_error);

            this.m_serverCertificate = serverCertificate.GetCertificateAt(0);
        }

        public override bool RequiresServerKeyExchange => true;

        public override byte[] GenerateServerKeyExchange()
        {
            var config = this.m_srpLoginParameters.Config;

            this.m_srpServer = this.m_context.Crypto.CreateSrp6Server(config, this.m_srpLoginParameters.Verifier);

            var B = this.m_srpServer.GenerateServerCredentials();

            var ng        = config.GetExplicitNG();
            var srpParams = new ServerSrpParams(ng[0], ng[1], this.m_srpLoginParameters.Salt, B);

            var digestBuffer = new DigestInputBuffer();

            srpParams.Encode(digestBuffer);

            if (this.m_serverCredentials != null) TlsUtilities.GenerateServerKeyExchangeSignature(this.m_context, this.m_serverCredentials, null, digestBuffer);

            return digestBuffer.ToArray();
        }

        public override void ProcessServerKeyExchange(Stream input)
        {
            DigestInputBuffer digestBuffer = null;
            var               teeIn        = input;

            if (this.m_keyExchange != KeyExchangeAlgorithm.SRP)
            {
                digestBuffer = new DigestInputBuffer();
                teeIn        = new TeeInputStream(input, digestBuffer);
            }

            var srpParams = ServerSrpParams.Parse(teeIn);

            if (digestBuffer != null)
                TlsUtilities.VerifyServerKeyExchangeSignature(this.m_context, input, this.m_serverCertificate, null,
                    digestBuffer);

            var config = new TlsSrpConfig();
            config.SetExplicitNG(new[] { srpParams.N, srpParams.G });

            if (!this.m_srpConfigVerifier.Accept(config))
                throw new TlsFatalAlert(AlertDescription.insufficient_security);

            this.m_srpSalt = srpParams.S;

            /*
             * RFC 5054 2.5.3: The client MUST abort the handshake with an "illegal_parameter" alert if
             * B % N = 0.
             */
            this.m_srpPeerCredentials = ValidatePublicValue(srpParams.N, srpParams.B);
            this.m_srpClient          = this.m_context.Crypto.CreateSrp6Client(config);
        }

        public override void ProcessClientCredentials(TlsCredentials clientCredentials) { throw new TlsFatalAlert(AlertDescription.internal_error); }

        public override void GenerateClientKeyExchange(Stream output)
        {
            var identity = this.m_srpIdentity.GetSrpIdentity();
            var password = this.m_srpIdentity.GetSrpPassword();

            var A = this.m_srpClient.GenerateClientCredentials(this.m_srpSalt, identity, password);
            TlsSrpUtilities.WriteSrpParameter(A, output);

            this.m_context.SecurityParameters.m_srpIdentity = Arrays.Clone(identity);
        }

        public override void ProcessClientKeyExchange(Stream input)
        {
            /*
             * RFC 5054 2.5.4: The server MUST abort the handshake with an "illegal_parameter" alert if
             * A % N = 0.
             */
            this.m_srpPeerCredentials = ValidatePublicValue(this.m_srpLoginParameters.Config.GetExplicitNG()[0],
                TlsSrpUtilities.ReadSrpParameter(input));

            this.m_context.SecurityParameters.m_srpIdentity = Arrays.Clone(this.m_srpLoginParameters.Identity);
        }

        public override TlsSecret GeneratePreMasterSecret()
        {
            var S = this.m_srpServer != null
                ? this.m_srpServer.CalculateSecret(this.m_srpPeerCredentials)
                : this.m_srpClient.CalculateSecret(this.m_srpPeerCredentials);

            // TODO Check if this needs to be a fixed size
            return this.m_context.Crypto.CreateSecret(BigIntegers.AsUnsignedByteArray(S));
        }

        protected static BigInteger ValidatePublicValue(BigInteger N, BigInteger val)
        {
            val = val.Mod(N);

            // Check that val % N != 0
            if (val.Equals(BigInteger.Zero))
                throw new TlsFatalAlert(AlertDescription.illegal_parameter);

            return val;
        }
    }
}
#pragma warning restore
#endif