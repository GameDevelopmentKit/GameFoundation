#if !BESTHTTP_DISABLE_ALTERNATE_SSL && (!UNITY_WEBGL || UNITY_EDITOR)
#pragma warning disable
namespace BestHTTP.SecureProtocol.Org.BouncyCastle.Tls.Crypto
{
    // TODO[tls-port] Would rather this be sealed
    /// <summary>Carrier class for context-related parameters needed for creating secrets and ciphers.</summary>
    public class TlsCryptoParameters
    {
        private readonly TlsContext m_context;

        /// <summary>Base constructor.</summary>
        /// <param name="context">the context for this parameters object.</param>
        public TlsCryptoParameters(TlsContext context) { this.m_context = context; }

        public SecurityParameters SecurityParameters => this.m_context.SecurityParameters;

        public ProtocolVersion ClientVersion => this.m_context.ClientVersion;

        public ProtocolVersion RsaPreMasterSecretVersion => this.m_context.RsaPreMasterSecretVersion;

        // TODO[tls-port] Would rather this be non-virtual
        public virtual ProtocolVersion ServerVersion => this.m_context.ServerVersion;

        public bool IsServer => this.m_context.IsServer;

        public TlsNonceGenerator NonceGenerator => this.m_context.NonceGenerator;
    }
}
#pragma warning restore
#endif