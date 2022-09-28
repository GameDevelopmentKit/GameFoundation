#if !BESTHTTP_DISABLE_ALTERNATE_SSL && (!UNITY_WEBGL || UNITY_EDITOR)
#pragma warning disable
namespace BestHTTP.SecureProtocol.Org.BouncyCastle.Tls.Crypto.Impl.BC
{
    using BestHTTP.SecureProtocol.Org.BouncyCastle.Crypto;

    internal sealed class BcTlsHash
        : TlsHash
    {
        private readonly BcTlsCrypto m_crypto;
        private readonly int         m_cryptoHashAlgorithm;
        private readonly IDigest     m_digest;

        internal BcTlsHash(BcTlsCrypto crypto, int cryptoHashAlgorithm)
            : this(crypto, cryptoHashAlgorithm, crypto.CreateDigest(cryptoHashAlgorithm))
        {
        }

        private BcTlsHash(BcTlsCrypto crypto, int cryptoHashAlgorithm, IDigest digest)
        {
            this.m_crypto              = crypto;
            this.m_cryptoHashAlgorithm = cryptoHashAlgorithm;
            this.m_digest              = digest;
        }

        public void Update(byte[] data, int offSet, int length) { this.m_digest.BlockUpdate(data, offSet, length); }

        public byte[] CalculateHash()
        {
            var rv = new byte[this.m_digest.GetDigestSize()];
            this.m_digest.DoFinal(rv, 0);
            return rv;
        }

        public TlsHash CloneHash()
        {
            var clone = this.m_crypto.CloneDigest(this.m_cryptoHashAlgorithm, this.m_digest);
            return new BcTlsHash(this.m_crypto, this.m_cryptoHashAlgorithm, clone);
        }

        public void Reset() { this.m_digest.Reset(); }
    }
}
#pragma warning restore
#endif