#if !BESTHTTP_DISABLE_ALTERNATE_SSL && (!UNITY_WEBGL || UNITY_EDITOR)
#pragma warning disable
namespace BestHTTP.SecureProtocol.Org.BouncyCastle.Tls.Crypto.Impl.BC
{
    using System;
    using BestHTTP.SecureProtocol.Org.BouncyCastle.Crypto.Engines;
    using BestHTTP.SecureProtocol.Org.BouncyCastle.Crypto.Parameters;
    using BestHTTP.SecureProtocol.Org.BouncyCastle.Crypto.Signers;

    /// <summary>Operator supporting the generation of RSASSA-PSS signatures using the BC light-weight API.</summary>
    public class BcTlsRsaPssSigner
        : BcTlsSigner
    {
        private readonly int m_signatureScheme;

        public BcTlsRsaPssSigner(BcTlsCrypto crypto, RsaKeyParameters privateKey, int signatureScheme)
            : base(crypto, privateKey)
        {
            if (!SignatureScheme.IsRsaPss(signatureScheme))
                throw new ArgumentException("signatureScheme");

            this.m_signatureScheme = signatureScheme;
        }

        public override byte[] GenerateRawSignature(SignatureAndHashAlgorithm algorithm, byte[] hash) { throw new NotSupportedException(); }

        public override TlsStreamSigner GetStreamSigner(SignatureAndHashAlgorithm algorithm)
        {
            if (algorithm == null || SignatureScheme.From(algorithm) != this.m_signatureScheme)
                throw new InvalidOperationException("Invalid algorithm: " + algorithm);

            var cryptoHashAlgorithm = SignatureScheme.GetCryptoHashAlgorithm(this.m_signatureScheme);
            var digest              = this.m_crypto.CreateDigest(cryptoHashAlgorithm);

            var signer = new PssSigner(new RsaBlindedEngine(), digest, digest.GetDigestSize());
            signer.Init(true, new ParametersWithRandom(this.m_privateKey, this.m_crypto.SecureRandom));

            return new BcTlsStreamSigner(signer);
        }
    }
}
#pragma warning restore
#endif