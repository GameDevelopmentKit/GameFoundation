#if !BESTHTTP_DISABLE_ALTERNATE_SSL && (!UNITY_WEBGL || UNITY_EDITOR)
#pragma warning disable
namespace BestHTTP.SecureProtocol.Org.BouncyCastle.Bcpg
{
    using System;
    using BestHTTP.SecureProtocol.Org.BouncyCastle.Asn1;
    using BestHTTP.SecureProtocol.Org.BouncyCastle.Math.EC;

    /// <remarks>Base class for an ECDH Public Key.</remarks>
    public class ECDHPublicBcpgKey
        : ECPublicBcpgKey
    {
        private readonly byte                     reserved;
        private readonly HashAlgorithmTag         hashFunctionId;
        private readonly SymmetricKeyAlgorithmTag symAlgorithmId;

        /// <param name="bcpgIn">The stream to read the packet from.</param>
        public ECDHPublicBcpgKey(
            BcpgInputStream bcpgIn)
            : base(bcpgIn)
        {
            var length        = bcpgIn.ReadByte();
            var kdfParameters = new byte[length];
            if (kdfParameters.Length != 3)
                throw new InvalidOperationException("kdf parameters size of 3 expected.");

            bcpgIn.ReadFully(kdfParameters);

            this.reserved       = kdfParameters[0];
            this.hashFunctionId = (HashAlgorithmTag)kdfParameters[1];
            this.symAlgorithmId = (SymmetricKeyAlgorithmTag)kdfParameters[2];

            this.VerifyHashAlgorithm();
            this.VerifySymmetricKeyAlgorithm();
        }

        public ECDHPublicBcpgKey(
            DerObjectIdentifier oid,
            ECPoint point,
            HashAlgorithmTag hashAlgorithm,
            SymmetricKeyAlgorithmTag symmetricKeyAlgorithm)
            : base(oid, point)
        {
            this.reserved       = 1;
            this.hashFunctionId = hashAlgorithm;
            this.symAlgorithmId = symmetricKeyAlgorithm;

            this.VerifyHashAlgorithm();
            this.VerifySymmetricKeyAlgorithm();
        }

        public virtual byte Reserved => this.reserved;

        public virtual HashAlgorithmTag HashAlgorithm => this.hashFunctionId;

        public virtual SymmetricKeyAlgorithmTag SymmetricKeyAlgorithm => this.symAlgorithmId;

        public override void Encode(
            BcpgOutputStream bcpgOut)
        {
            base.Encode(bcpgOut);
            bcpgOut.WriteByte(0x3);
            bcpgOut.WriteByte(this.reserved);
            bcpgOut.WriteByte((byte)this.hashFunctionId);
            bcpgOut.WriteByte((byte)this.symAlgorithmId);
        }

        private void VerifyHashAlgorithm()
        {
            switch (this.hashFunctionId)
            {
                case HashAlgorithmTag.Sha256:
                case HashAlgorithmTag.Sha384:
                case HashAlgorithmTag.Sha512:
                    break;
                default:
                    throw new InvalidOperationException("Hash algorithm must be SHA-256 or stronger.");
            }
        }

        private void VerifySymmetricKeyAlgorithm()
        {
            switch (this.symAlgorithmId)
            {
                case SymmetricKeyAlgorithmTag.Aes128:
                case SymmetricKeyAlgorithmTag.Aes192:
                case SymmetricKeyAlgorithmTag.Aes256:
                    break;
                default:
                    throw new InvalidOperationException("Symmetric key algorithm must be AES-128 or stronger.");
            }
        }
    }
}
#pragma warning restore
#endif