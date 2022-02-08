#if !BESTHTTP_DISABLE_ALTERNATE_SSL && (!UNITY_WEBGL || UNITY_EDITOR)
#pragma warning disable
namespace BestHTTP.SecureProtocol.Org.BouncyCastle.Tls.Crypto.Impl.BC
{
    using BestHTTP.SecureProtocol.Org.BouncyCastle.Crypto;
    using BestHTTP.SecureProtocol.Org.BouncyCastle.Crypto.Agreement.Srp;
    using BestHTTP.SecureProtocol.Org.BouncyCastle.Math;

    internal sealed class BcTlsSrp6Server
        : TlsSrp6Server
    {
        private readonly Srp6Server m_srp6Server;

        internal BcTlsSrp6Server(Srp6Server srp6Server) { this.m_srp6Server = srp6Server; }

        public BigInteger GenerateServerCredentials() { return this.m_srp6Server.GenerateServerCredentials(); }

        public BigInteger CalculateSecret(BigInteger clientA)
        {
            try
            {
                return this.m_srp6Server.CalculateSecret(clientA);
            }
            catch (CryptoException e)
            {
                throw new TlsFatalAlert(AlertDescription.illegal_parameter, e);
            }
        }
    }
}
#pragma warning restore
#endif