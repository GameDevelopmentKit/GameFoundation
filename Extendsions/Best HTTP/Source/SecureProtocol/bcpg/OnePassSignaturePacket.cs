#if !BESTHTTP_DISABLE_ALTERNATE_SSL && (!UNITY_WEBGL || UNITY_EDITOR)
#pragma warning disable
namespace BestHTTP.SecureProtocol.Org.BouncyCastle.Bcpg
{
	using System.IO;

	/// <remarks>Generic signature object</remarks>
    public class OnePassSignaturePacket
        : ContainedPacket
    {
        private readonly int version;
        private readonly int nested;

        internal OnePassSignaturePacket(
            BcpgInputStream bcpgIn)
        {
            this.version       = bcpgIn.ReadByte();
            this.SignatureType = bcpgIn.ReadByte();
            this.HashAlgorithm = (HashAlgorithmTag)bcpgIn.ReadByte();
            this.KeyAlgorithm  = (PublicKeyAlgorithmTag)bcpgIn.ReadByte();

            this.KeyId |= (long)bcpgIn.ReadByte() << 56;
            this.KeyId |= (long)bcpgIn.ReadByte() << 48;
            this.KeyId |= (long)bcpgIn.ReadByte() << 40;
            this.KeyId |= (long)bcpgIn.ReadByte() << 32;
            this.KeyId |= (long)bcpgIn.ReadByte() << 24;
            this.KeyId |= (long)bcpgIn.ReadByte() << 16;
            this.KeyId |= (long)bcpgIn.ReadByte() << 8;
            this.KeyId |= (uint)bcpgIn.ReadByte();

            this.nested = bcpgIn.ReadByte();
        }

        public OnePassSignaturePacket(
            int sigType,
            HashAlgorithmTag hashAlgorithm,
            PublicKeyAlgorithmTag keyAlgorithm,
            long keyId,
            bool isNested)
        {
            this.version       = 3;
            this.SignatureType = sigType;
            this.HashAlgorithm = hashAlgorithm;
            this.KeyAlgorithm  = keyAlgorithm;
            this.KeyId         = keyId;
            this.nested        = isNested ? 0 : 1;
        }

        public int SignatureType { get; }

        /// <summary>The encryption algorithm tag.</summary>
        public PublicKeyAlgorithmTag KeyAlgorithm { get; }

        /// <summary>The hash algorithm tag.</summary>
        public HashAlgorithmTag HashAlgorithm { get; }

        public long KeyId { get; }

        public override void Encode(
            BcpgOutputStream bcpgOut)
        {
            var bOut = new MemoryStream();
            var pOut = new BcpgOutputStream(bOut);

            pOut.Write(
                (byte)this.version,
                (byte)this.SignatureType,
                (byte)this.HashAlgorithm,
                (byte)this.KeyAlgorithm);

            pOut.WriteLong(this.KeyId);

            pOut.WriteByte((byte)this.nested);

            bcpgOut.WritePacket(PacketTag.OnePassSignature, bOut.ToArray(), true);
        }
    }
}
#pragma warning restore
#endif