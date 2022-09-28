#if !BESTHTTP_DISABLE_ALTERNATE_SSL && (!UNITY_WEBGL || UNITY_EDITOR)
#pragma warning disable
namespace BestHTTP.SecureProtocol.Org.BouncyCastle.Tls.Crypto.Impl.BC
{
    using BestHTTP.SecureProtocol.Org.BouncyCastle.Crypto;
    using BestHTTP.SecureProtocol.Org.BouncyCastle.Crypto.Parameters;
    using BestHTTP.SecureProtocol.Org.BouncyCastle.Crypto.Signers;

    /// <summary>
    ///     Implementation class for generation of the raw DSA signature type using the BC light-weight API.
    /// </summary>
    public class BcTlsDsaSigner
        : BcTlsDssSigner
    {
        public BcTlsDsaSigner(BcTlsCrypto crypto, DsaPrivateKeyParameters privateKey)
            : base(crypto, privateKey)
        {
        }

        protected override IDsa CreateDsaImpl(int cryptoHashAlgorithm) { return new DsaSigner(new HMacDsaKCalculator(this.m_crypto.CreateDigest(cryptoHashAlgorithm))); }

        protected override short SignatureAlgorithm => Tls.SignatureAlgorithm.dsa;
    }
}
#pragma warning restore
#endif