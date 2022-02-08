#if !BESTHTTP_DISABLE_ALTERNATE_SSL && (!UNITY_WEBGL || UNITY_EDITOR)
#pragma warning disable
namespace BestHTTP.SecureProtocol.Org.BouncyCastle.Tls
{
    using System.Collections;
    using BestHTTP.SecureProtocol.Org.BouncyCastle.Tls.Crypto;

    public sealed class SecurityParameters
    {
        internal int             m_entity                 = -1;
        internal bool            m_secureRenegotiation    = false;
        internal int             m_cipherSuite            = Tls.CipherSuite.TLS_NULL_WITH_NULL_NULL;
        internal short           m_maxFragmentLength      = -1;
        internal int             m_prfAlgorithm           = -1;
        internal int             m_prfCryptoHashAlgorithm = -1;
        internal int             m_prfHashLength          = -1;
        internal int             m_verifyDataLength       = -1;
        internal TlsSecret       m_baseKeyClient;
        internal TlsSecret       m_baseKeyServer;
        internal TlsSecret       m_earlyExporterMasterSecret;
        internal TlsSecret       m_earlySecret;
        internal TlsSecret       m_exporterMasterSecret;
        internal TlsSecret       m_handshakeSecret;
        internal TlsSecret       m_masterSecret;
        internal TlsSecret       m_trafficSecretClient = null;
        internal TlsSecret       m_trafficSecretServer = null;
        internal byte[]          m_clientRandom        = null;
        internal byte[]          m_serverRandom        = null;
        internal byte[]          m_sessionHash;
        internal byte[]          m_sessionID;
        internal byte[]          m_pskIdentity            = null;
        internal byte[]          m_srpIdentity            = null;
        internal byte[]          m_tlsServerEndPoint      = null;
        internal byte[]          m_tlsUnique              = null;
        internal bool            m_encryptThenMac         = false;
        internal bool            m_extendedMasterSecret   = false;
        internal bool            m_extendedPadding        = false;
        internal bool            m_truncatedHmac          = false;
        internal ProtocolName    m_applicationProtocol    = null;
        internal bool            m_applicationProtocolSet = false;
        internal short[]         m_clientCertTypes;
        internal IList           m_clientServerNames;
        internal IList           m_clientSigAlgs;
        internal IList           m_clientSigAlgsCert;
        internal int[]           m_clientSupportedGroups;
        internal IList           m_serverSigAlgs;
        internal IList           m_serverSigAlgsCert;
        internal int[]           m_serverSupportedGroups;
        internal int             m_keyExchangeAlgorithm = -1;
        internal Certificate     m_localCertificate     = null;
        internal Certificate     m_peerCertificate      = null;
        internal ProtocolVersion m_negotiatedVersion    = null;
        internal int             m_statusRequestVersion;

        // TODO[tls-ops] Investigate whether we can handle verify data using TlsSecret
        internal byte[] m_localVerifyData = null;
        internal byte[] m_peerVerifyData  = null;

        internal void Clear()
        {
            this.m_sessionHash           = null;
            this.m_sessionID             = null;
            this.m_clientCertTypes       = null;
            this.m_clientServerNames     = null;
            this.m_clientSigAlgs         = null;
            this.m_clientSigAlgsCert     = null;
            this.m_clientSupportedGroups = null;
            this.m_serverSigAlgs         = null;
            this.m_serverSigAlgsCert     = null;
            this.m_serverSupportedGroups = null;
            this.m_statusRequestVersion  = 0;

            this.m_baseKeyClient             = ClearSecret(this.m_baseKeyClient);
            this.m_baseKeyServer             = ClearSecret(this.m_baseKeyServer);
            this.m_earlyExporterMasterSecret = ClearSecret(this.m_earlyExporterMasterSecret);
            this.m_earlySecret               = ClearSecret(this.m_earlySecret);
            this.m_exporterMasterSecret      = ClearSecret(this.m_exporterMasterSecret);
            this.m_handshakeSecret           = ClearSecret(this.m_handshakeSecret);
            this.m_masterSecret              = ClearSecret(this.m_masterSecret);
        }

        public ProtocolName ApplicationProtocol => this.m_applicationProtocol;

        public TlsSecret BaseKeyClient => this.m_baseKeyClient;

        public TlsSecret BaseKeyServer => this.m_baseKeyServer;

        public int CipherSuite => this.m_cipherSuite;

        public short[] ClientCertTypes => this.m_clientCertTypes;

        public byte[] ClientRandom => this.m_clientRandom;

        public IList ClientServerNames => this.m_clientServerNames;

        public IList ClientSigAlgs => this.m_clientSigAlgs;

        public IList ClientSigAlgsCert => this.m_clientSigAlgsCert;

        public int[] ClientSupportedGroups => this.m_clientSupportedGroups;

        public TlsSecret EarlyExporterMasterSecret => this.m_earlyExporterMasterSecret;

        public TlsSecret EarlySecret => this.m_earlySecret;

        public TlsSecret ExporterMasterSecret => this.m_exporterMasterSecret;

        public int Entity => this.m_entity;

        public TlsSecret HandshakeSecret => this.m_handshakeSecret;

        public bool IsApplicationProtocolSet => this.m_applicationProtocolSet;

        public bool IsEncryptThenMac => this.m_encryptThenMac;

        public bool IsExtendedMasterSecret => this.m_extendedMasterSecret;

        public bool IsExtendedPadding => this.m_extendedPadding;

        public bool IsSecureRenegotiation => this.m_secureRenegotiation;

        public bool IsTruncatedHmac => this.m_truncatedHmac;

        public int KeyExchangeAlgorithm => this.m_keyExchangeAlgorithm;

        public Certificate LocalCertificate => this.m_localCertificate;

        public byte[] LocalVerifyData => this.m_localVerifyData;

        public TlsSecret MasterSecret => this.m_masterSecret;

        public short MaxFragmentLength => this.m_maxFragmentLength;

        public ProtocolVersion NegotiatedVersion => this.m_negotiatedVersion;

        public Certificate PeerCertificate => this.m_peerCertificate;

        public byte[] PeerVerifyData => this.m_peerVerifyData;

        public int PrfAlgorithm => this.m_prfAlgorithm;

        public int PrfCryptoHashAlgorithm => this.m_prfCryptoHashAlgorithm;

        public int PrfHashLength => this.m_prfHashLength;

        public byte[] PskIdentity => this.m_pskIdentity;

        public byte[] ServerRandom => this.m_serverRandom;

        public IList ServerSigAlgs => this.m_serverSigAlgs;

        public IList ServerSigAlgsCert => this.m_serverSigAlgsCert;

        public int[] ServerSupportedGroups => this.m_serverSupportedGroups;

        public byte[] SessionHash => this.m_sessionHash;

        public byte[] SessionID => this.m_sessionID;

        public byte[] SrpIdentity => this.m_srpIdentity;

        public int StatusRequestVersion => this.m_statusRequestVersion;

        public byte[] TlsServerEndPoint => this.m_tlsServerEndPoint;

        public byte[] TlsUnique => this.m_tlsUnique;

        public TlsSecret TrafficSecretClient => this.m_trafficSecretClient;

        public TlsSecret TrafficSecretServer => this.m_trafficSecretServer;

        public int VerifyDataLength => this.m_verifyDataLength;

        private static TlsSecret ClearSecret(TlsSecret secret)
        {
            if (null != secret) secret.Destroy();
            return null;
        }
    }
}
#pragma warning restore
#endif