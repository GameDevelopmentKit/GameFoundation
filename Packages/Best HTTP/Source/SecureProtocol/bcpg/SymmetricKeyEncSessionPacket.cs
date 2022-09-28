#if !BESTHTTP_DISABLE_ALTERNATE_SSL && (!UNITY_WEBGL || UNITY_EDITOR)
#pragma warning disable
namespace BestHTTP.SecureProtocol.Org.BouncyCastle.Bcpg
{
    using System.IO;

    /**
    * Basic type for a symmetric encrypted session key packet
    */
    public class SymmetricKeyEncSessionPacket
        : ContainedPacket
    {
        private readonly byte[] secKeyData;

        public SymmetricKeyEncSessionPacket(
            BcpgInputStream bcpgIn)
        {
            this.Version      = bcpgIn.ReadByte();
            this.EncAlgorithm = (SymmetricKeyAlgorithmTag)bcpgIn.ReadByte();

            this.S2k = new S2k(bcpgIn);

            this.secKeyData = bcpgIn.ReadAll();
        }

        public SymmetricKeyEncSessionPacket(
            SymmetricKeyAlgorithmTag encAlgorithm,
            S2k s2k,
            byte[] secKeyData)
        {
            this.Version      = 4;
            this.EncAlgorithm = encAlgorithm;
            this.S2k          = s2k;
            this.secKeyData   = secKeyData;
        }

        /**
        * @return int
        */
        public SymmetricKeyAlgorithmTag EncAlgorithm { get; }

        /**
        * @return S2k
        */
        public S2k S2k { get; }

        /**
        * @return byte[]
        */
        public byte[] GetSecKeyData() { return this.secKeyData; }

        /**
        * @return int
        */
        public int Version { get; }

        public override void Encode(
            BcpgOutputStream bcpgOut)
        {
            var bOut = new MemoryStream();
            var pOut = new BcpgOutputStream(bOut);

            pOut.Write(
                (byte)this.Version,
                (byte)this.EncAlgorithm);

            pOut.WriteObject(this.S2k);

            if (this.secKeyData != null && this.secKeyData.Length > 0) pOut.Write(this.secKeyData);

            bcpgOut.WritePacket(PacketTag.SymmetricKeyEncryptedSessionKey, bOut.ToArray(), true);
        }
    }
}
#pragma warning restore
#endif