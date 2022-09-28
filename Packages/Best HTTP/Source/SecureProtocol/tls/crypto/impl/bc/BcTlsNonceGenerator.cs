#if !BESTHTTP_DISABLE_ALTERNATE_SSL && (!UNITY_WEBGL || UNITY_EDITOR)
#pragma warning disable
namespace BestHTTP.SecureProtocol.Org.BouncyCastle.Tls.Crypto.Impl.BC
{
    using BestHTTP.SecureProtocol.Org.BouncyCastle.Crypto.Prng;

    internal sealed class BcTlsNonceGenerator
        : TlsNonceGenerator
    {
        private readonly IRandomGenerator m_randomGenerator;

        internal BcTlsNonceGenerator(IRandomGenerator randomGenerator) { this.m_randomGenerator = randomGenerator; }

        public byte[] GenerateNonce(int size)
        {
            var nonce = new byte[size];
            this.m_randomGenerator.NextBytes(nonce);
            return nonce;
        }
    }
}
#pragma warning restore
#endif