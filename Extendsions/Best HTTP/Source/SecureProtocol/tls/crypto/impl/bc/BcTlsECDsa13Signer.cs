#if !BESTHTTP_DISABLE_ALTERNATE_SSL && (!UNITY_WEBGL || UNITY_EDITOR)
#pragma warning disable
namespace BestHTTP.SecureProtocol.Org.BouncyCastle.Tls.Crypto.Impl.BC
{
    using System;
    using BestHTTP.SecureProtocol.Org.BouncyCastle.Crypto;
    using BestHTTP.SecureProtocol.Org.BouncyCastle.Crypto.Digests;
    using BestHTTP.SecureProtocol.Org.BouncyCastle.Crypto.Parameters;
    using BestHTTP.SecureProtocol.Org.BouncyCastle.Crypto.Signers;

    /// <summary>
    ///     Implementation class for generation of ECDSA signatures in TLS 1.3+ using the BC light-weight API.
    /// </summary>
    public class BcTlsECDsa13Signer
        : BcTlsSigner
    {
        private readonly int m_signatureScheme;

        public BcTlsECDsa13Signer(BcTlsCrypto crypto, ECPrivateKeyParameters privateKey, int signatureScheme)
            : base(crypto, privateKey)
        {
            if (!SignatureScheme.IsECDsa(signatureScheme))
                throw new ArgumentException("signatureScheme");

            this.m_signatureScheme = signatureScheme;
        }

        public override byte[] GenerateRawSignature(SignatureAndHashAlgorithm algorithm, byte[] hash)
        {
            if (algorithm == null || SignatureScheme.From(algorithm) != this.m_signatureScheme)
                throw new InvalidOperationException("Invalid algorithm: " + algorithm);

            var  cryptoHashAlgorithm = SignatureScheme.GetCryptoHashAlgorithm(this.m_signatureScheme);
            IDsa dsa                 = new ECDsaSigner(new HMacDsaKCalculator(this.m_crypto.CreateDigest(cryptoHashAlgorithm)));

            ISigner signer = new DsaDigestSigner(dsa, new NullDigest());
            signer.Init(true, new ParametersWithRandom(this.m_privateKey, this.m_crypto.SecureRandom));
            signer.BlockUpdate(hash, 0, hash.Length);
            try
            {
                return signer.GenerateSignature();
            }
            catch (CryptoException e)
            {
                throw new TlsFatalAlert(AlertDescription.internal_error, e);
            }
        }
    }
}
#pragma warning restore
#endif