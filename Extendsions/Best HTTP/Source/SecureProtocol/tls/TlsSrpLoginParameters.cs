#if !BESTHTTP_DISABLE_ALTERNATE_SSL && (!UNITY_WEBGL || UNITY_EDITOR)
#pragma warning disable
namespace BestHTTP.SecureProtocol.Org.BouncyCastle.Tls
{
    using BestHTTP.SecureProtocol.Org.BouncyCastle.Math;
    using BestHTTP.SecureProtocol.Org.BouncyCastle.Tls.Crypto;
    using BestHTTP.SecureProtocol.Org.BouncyCastle.Utilities;

    public class TlsSrpLoginParameters
    {
        protected byte[]       m_identity;
        protected TlsSrpConfig m_srpConfig;
        protected BigInteger   m_verifier;
        protected byte[]       m_salt;

        public TlsSrpLoginParameters(byte[] identity, TlsSrpConfig srpConfig, BigInteger verifier, byte[] salt)
        {
            this.m_identity  = Arrays.Clone(identity);
            this.m_srpConfig = srpConfig;
            this.m_verifier  = verifier;
            this.m_salt      = Arrays.Clone(salt);
        }

        public virtual TlsSrpConfig Config => this.m_srpConfig;

        public virtual byte[] Identity => this.m_identity;

        public virtual byte[] Salt => this.m_salt;

        public virtual BigInteger Verifier => this.m_verifier;
    }
}
#pragma warning restore
#endif