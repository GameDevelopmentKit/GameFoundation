#if !BESTHTTP_DISABLE_ALTERNATE_SSL && (!UNITY_WEBGL || UNITY_EDITOR)
#pragma warning disable
namespace BestHTTP.SecureProtocol.Org.BouncyCastle.Bcpg
{
	using System.IO;
	using BestHTTP.SecureProtocol.Org.BouncyCastle.Utilities;
	using BestHTTP.SecureProtocol.Org.BouncyCastle.Utilities.IO;

	/// <remarks>Basic packet for a PGP public key.</remarks>
    public class PublicKeyEncSessionPacket
        : ContainedPacket //, PublicKeyAlgorithmTag
    {
        private readonly byte[][] data;

        internal PublicKeyEncSessionPacket(
            BcpgInputStream bcpgIn)
        {
            this.Version = bcpgIn.ReadByte();

            this.KeyId |= (long)bcpgIn.ReadByte() << 56;
            this.KeyId |= (long)bcpgIn.ReadByte() << 48;
            this.KeyId |= (long)bcpgIn.ReadByte() << 40;
            this.KeyId |= (long)bcpgIn.ReadByte() << 32;
            this.KeyId |= (long)bcpgIn.ReadByte() << 24;
            this.KeyId |= (long)bcpgIn.ReadByte() << 16;
            this.KeyId |= (long)bcpgIn.ReadByte() << 8;
            this.KeyId |= (uint)bcpgIn.ReadByte();

            this.Algorithm = (PublicKeyAlgorithmTag)bcpgIn.ReadByte();

            switch (this.Algorithm)
            {
                case PublicKeyAlgorithmTag.RsaEncrypt:
                case PublicKeyAlgorithmTag.RsaGeneral:
                    this.data = new[] { new MPInteger(bcpgIn).GetEncoded() };
                    break;
                case PublicKeyAlgorithmTag.ElGamalEncrypt:
                case PublicKeyAlgorithmTag.ElGamalGeneral:
                    var p = new MPInteger(bcpgIn);
                    var g = new MPInteger(bcpgIn);
                    this.data = new[]
                    {
                        p.GetEncoded(),
                        g.GetEncoded()
                    };
                    break;
                case PublicKeyAlgorithmTag.ECDH:
                    this.data = new[] { Streams.ReadAll(bcpgIn) };
                    break;
                default:
                    throw new IOException("unknown PGP public key algorithm encountered");
            }
        }

        public PublicKeyEncSessionPacket(
            long keyId,
            PublicKeyAlgorithmTag algorithm,
            byte[][] data)
        {
            this.Version   = 3;
            this.KeyId     = keyId;
            this.Algorithm = algorithm;
            this.data      = new byte[data.Length][];
            for (var i = 0; i < data.Length; ++i) this.data[i] = Arrays.Clone(data[i]);
        }

        public int Version { get; }

        public long KeyId { get; }

        public PublicKeyAlgorithmTag Algorithm { get; }

        public byte[][] GetEncSessionKey() { return this.data; }

        public override void Encode(
            BcpgOutputStream bcpgOut)
        {
            var bOut = new MemoryStream();
            var pOut = new BcpgOutputStream(bOut);

            pOut.WriteByte((byte)this.Version);

            pOut.WriteLong(this.KeyId);

            pOut.WriteByte((byte)this.Algorithm);

            for (var i = 0; i < this.data.Length; ++i) pOut.Write(this.data[i]);

            Platform.Dispose(pOut);

            bcpgOut.WritePacket(PacketTag.PublicKeyEncryptedSession, bOut.ToArray(), true);
        }
    }
}
#pragma warning restore
#endif