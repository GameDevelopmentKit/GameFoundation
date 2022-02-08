#if !BESTHTTP_DISABLE_ALTERNATE_SSL && (!UNITY_WEBGL || UNITY_EDITOR)
#pragma warning disable
namespace BestHTTP.SecureProtocol.Org.BouncyCastle.Tls.Crypto.Impl.BC
{
    using System;
    using BestHTTP.SecureProtocol.Org.BouncyCastle.Crypto.Engines;
    using BestHTTP.SecureProtocol.Org.BouncyCastle.Crypto.Parameters;
    using BestHTTP.SecureProtocol.Org.BouncyCastle.Crypto.Signers;

    /// <summary>Operator supporting the verification of RSASSA-PSS signatures using the BC light-weight API.</summary>
    public class BcTlsRsaPssVerifier
        : BcTlsVerifier
    {
        private readonly int m_signatureScheme;

        public BcTlsRsaPssVerifier(BcTlsCrypto crypto, RsaKeyParameters publicKey, int signatureScheme)
            : base(crypto, publicKey)
        {
            if (!SignatureScheme.IsRsaPss(signatureScheme))
                throw new ArgumentException("signatureScheme");

            this.m_signatureScheme = signatureScheme;
        }

        public override bool VerifyRawSignature(DigitallySigned signature, byte[] hash) { throw new NotSupportedException(); }

        public override TlsStreamVerifier GetStreamVerifier(DigitallySigned signature)
        {
            var algorithm = signature.Algorithm;
            if (algorithm == null || SignatureScheme.From(algorithm) != this.m_signatureScheme)
                throw new InvalidOperationException("Invalid algorithm: " + algorithm);

            var cryptoHashAlgorithm = SignatureScheme.GetCryptoHashAlgorithm(this.m_signatureScheme);
            var digest              = this.m_crypto.CreateDigest(cryptoHashAlgorithm);

            var verifier = new PssSigner(new RsaEngine(), digest, digest.GetDigestSize());
            verifier.Init(false, this.m_publicKey);

            return new BcTlsStreamVerifier(verifier, signature.Signature);
        }
    }
}
#pragma warning restore
#endif