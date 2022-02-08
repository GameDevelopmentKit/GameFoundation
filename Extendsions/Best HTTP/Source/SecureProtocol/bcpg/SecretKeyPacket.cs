#if !BESTHTTP_DISABLE_ALTERNATE_SSL && (!UNITY_WEBGL || UNITY_EDITOR)
#pragma warning disable
namespace BestHTTP.SecureProtocol.Org.BouncyCastle.Bcpg
{
	using System.IO;
	using BestHTTP.SecureProtocol.Org.BouncyCastle.Utilities;

	/// <remarks>Basic packet for a PGP secret key.</remarks>
    public class SecretKeyPacket
        : ContainedPacket //, PublicKeyAlgorithmTag
    {
        public const int UsageNone     = 0x00;
        public const int UsageChecksum = 0xff;
        public const int UsageSha1     = 0xfe;

        private readonly byte[] secKeyData;
        private readonly byte[] iv;

        internal SecretKeyPacket(
            BcpgInputStream bcpgIn)
        {
            if (this is SecretSubkeyPacket)
                this.PublicKeyPacket = new PublicSubkeyPacket(bcpgIn);
            else
                this.PublicKeyPacket = new PublicKeyPacket(bcpgIn);

            this.S2kUsage = bcpgIn.ReadByte();

            if (this.S2kUsage == UsageChecksum || this.S2kUsage == UsageSha1)
            {
                this.EncAlgorithm = (SymmetricKeyAlgorithmTag)bcpgIn.ReadByte();
                this.S2k          = new S2k(bcpgIn);
            }
            else
            {
                this.EncAlgorithm = (SymmetricKeyAlgorithmTag)this.S2kUsage;
            }

            if (!(this.S2k != null && this.S2k.Type == S2k.GnuDummyS2K && this.S2k.ProtectionMode == 0x01))
                if (this.S2kUsage != 0)
                {
                    if ((int)this.EncAlgorithm < 7)
                        this.iv = new byte[8];
                    else
                        this.iv = new byte[16];
                    bcpgIn.ReadFully(this.iv);
                }

            this.secKeyData = bcpgIn.ReadAll();
        }

        public SecretKeyPacket(
            PublicKeyPacket pubKeyPacket,
            SymmetricKeyAlgorithmTag encAlgorithm,
            S2k s2k,
            byte[] iv,
            byte[] secKeyData)
        {
            this.PublicKeyPacket = pubKeyPacket;
            this.EncAlgorithm    = encAlgorithm;

            if (encAlgorithm != SymmetricKeyAlgorithmTag.Null)
                this.S2kUsage = UsageChecksum;
            else
                this.S2kUsage = UsageNone;

            this.S2k        = s2k;
            this.iv         = Arrays.Clone(iv);
            this.secKeyData = secKeyData;
        }

        public SecretKeyPacket(
            PublicKeyPacket pubKeyPacket,
            SymmetricKeyAlgorithmTag encAlgorithm,
            int s2kUsage,
            S2k s2k,
            byte[] iv,
            byte[] secKeyData)
        {
            this.PublicKeyPacket = pubKeyPacket;
            this.EncAlgorithm    = encAlgorithm;
            this.S2kUsage        = s2kUsage;
            this.S2k             = s2k;
            this.iv              = Arrays.Clone(iv);
            this.secKeyData      = secKeyData;
        }

        public SymmetricKeyAlgorithmTag EncAlgorithm { get; }

        public int S2kUsage { get; }

        public byte[] GetIV() { return Arrays.Clone(this.iv); }

        public S2k S2k { get; }

        public PublicKeyPacket PublicKeyPacket { get; }

        public byte[] GetSecretKeyData() { return this.secKeyData; }

        public byte[] GetEncodedContents()
        {
            var bOut = new MemoryStream();
            var pOut = new BcpgOutputStream(bOut);

            pOut.Write(this.PublicKeyPacket.GetEncodedContents());

            pOut.WriteByte((byte)this.S2kUsage);

            if (this.S2kUsage == UsageChecksum || this.S2kUsage == UsageSha1)
            {
                pOut.WriteByte((byte)this.EncAlgorithm);
                pOut.WriteObject(this.S2k);
            }

            if (this.iv != null) pOut.Write(this.iv);

            if (this.secKeyData != null && this.secKeyData.Length > 0) pOut.Write(this.secKeyData);

            return bOut.ToArray();
        }

        public override void Encode(
            BcpgOutputStream bcpgOut)
        {
            bcpgOut.WritePacket(PacketTag.SecretKey, this.GetEncodedContents(), true);
        }
    }
}
#pragma warning restore
#endif