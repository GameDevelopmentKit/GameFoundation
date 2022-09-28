#if !BESTHTTP_DISABLE_ALTERNATE_SSL && (!UNITY_WEBGL || UNITY_EDITOR)
#pragma warning disable
namespace BestHTTP.SecureProtocol.Org.BouncyCastle.Tls
{
    using System.IO;
    using BestHTTP.SecureProtocol.Org.BouncyCastle.Tls.Crypto;

    /// <summary>Base class for supporting a TLS key exchange implementation.</summary>
    public abstract class AbstractTlsKeyExchange
        : TlsKeyExchange
    {
        protected readonly int m_keyExchange;

        protected TlsContext m_context;

        protected AbstractTlsKeyExchange(int keyExchange) { this.m_keyExchange = keyExchange; }

        public virtual void Init(TlsContext context) { this.m_context = context; }

        public abstract void SkipServerCredentials();

        public abstract void ProcessServerCredentials(TlsCredentials serverCredentials);

        public virtual void ProcessServerCertificate(Certificate serverCertificate) { throw new TlsFatalAlert(AlertDescription.internal_error); }

        public virtual bool RequiresServerKeyExchange => false;

        public virtual byte[] GenerateServerKeyExchange()
        {
            if (this.RequiresServerKeyExchange)
                throw new TlsFatalAlert(AlertDescription.internal_error);

            return null;
        }

        public virtual void SkipServerKeyExchange()
        {
            if (this.RequiresServerKeyExchange)
                throw new TlsFatalAlert(AlertDescription.unexpected_message);
        }

        public virtual void ProcessServerKeyExchange(Stream input)
        {
            if (!this.RequiresServerKeyExchange)
                throw new TlsFatalAlert(AlertDescription.unexpected_message);
        }

        public virtual short[] GetClientCertificateTypes() { return null; }

        public virtual void SkipClientCredentials() { }

        public abstract void ProcessClientCredentials(TlsCredentials clientCredentials);

        public virtual void ProcessClientCertificate(Certificate clientCertificate) { }

        public abstract void GenerateClientKeyExchange(Stream output);

        public virtual void ProcessClientKeyExchange(Stream input)
        {
            // Key exchange implementation MUST support client key exchange
            throw new TlsFatalAlert(AlertDescription.internal_error);
        }

        public virtual bool RequiresCertificateVerify => true;

        public abstract TlsSecret GeneratePreMasterSecret();
    }
}
#pragma warning restore
#endif