#if !BESTHTTP_DISABLE_ALTERNATE_SSL && (!UNITY_WEBGL || UNITY_EDITOR)
#pragma warning disable
namespace BestHTTP.SecureProtocol.Org.BouncyCastle.Tls.Crypto.Impl.BC
{
    using System;
    using BestHTTP.SecureProtocol.Org.BouncyCastle.Crypto.Parameters;
    using BestHTTP.SecureProtocol.Org.BouncyCastle.Crypto.Signers;

    public class BcTlsEd25519Signer
        : BcTlsSigner
    {
        public BcTlsEd25519Signer(BcTlsCrypto crypto, Ed25519PrivateKeyParameters privateKey)
            : base(crypto, privateKey)
        {
        }

        public override byte[] GenerateRawSignature(SignatureAndHashAlgorithm algorithm, byte[] hash) { throw new NotSupportedException(); }

        public override TlsStreamSigner GetStreamSigner(SignatureAndHashAlgorithm algorithm)
        {
            if (algorithm == null || SignatureScheme.From(algorithm) != SignatureScheme.ed25519)
                throw new InvalidOperationException("Invalid algorithm: " + algorithm);

            var signer = new Ed25519Signer();
            signer.Init(true, this.m_privateKey);

            return new BcTlsStreamSigner(signer);
        }
    }
}
#pragma warning restore
#endif