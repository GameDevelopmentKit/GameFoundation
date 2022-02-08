#if !BESTHTTP_DISABLE_ALTERNATE_SSL && (!UNITY_WEBGL || UNITY_EDITOR)
#pragma warning disable
namespace BestHTTP.SecureProtocol.Org.BouncyCastle.Tls.Crypto.Impl.BC
{
    using System;
    using BestHTTP.SecureProtocol.Org.BouncyCastle.Crypto;
    using BestHTTP.SecureProtocol.Org.BouncyCastle.Crypto.Digests;
    using BestHTTP.SecureProtocol.Org.BouncyCastle.Crypto.Signers;

    /// <summary>
    ///     BC light-weight base class for the verifiers supporting the two DSA style algorithms from FIPS PUB
    ///     186-4: DSA and ECDSA.
    /// </summary>
    public abstract class BcTlsDssVerifier
        : BcTlsVerifier
    {
        protected BcTlsDssVerifier(BcTlsCrypto crypto, AsymmetricKeyParameter publicKey)
            : base(crypto, publicKey)
        {
        }

        protected abstract IDsa CreateDsaImpl(int cryptoHashAlgorithm);

        protected abstract short SignatureAlgorithm { get; }

        public override bool VerifyRawSignature(DigitallySigned signedParams, byte[] hash)
        {
            var algorithm = signedParams.Algorithm;
            if (algorithm != null && algorithm.Signature != this.SignatureAlgorithm)
                throw new InvalidOperationException("Invalid algorithm: " + algorithm);

            var cryptoHashAlgorithm = null == algorithm
                ? CryptoHashAlgorithm.sha1
                : TlsCryptoUtilities.GetHash(algorithm.Hash);

            ISigner signer = new DsaDigestSigner(this.CreateDsaImpl(cryptoHashAlgorithm), new NullDigest());
            signer.Init(false, this.m_publicKey);
            if (algorithm == null)
                // Note: Only use the SHA1 part of the (MD5/SHA1) hash
                signer.BlockUpdate(hash, 16, 20);
            else
                signer.BlockUpdate(hash, 0, hash.Length);
            return signer.VerifySignature(signedParams.Signature);
        }
    }
}
#pragma warning restore
#endif