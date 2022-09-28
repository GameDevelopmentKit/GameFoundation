#if !BESTHTTP_DISABLE_ALTERNATE_SSL && (!UNITY_WEBGL || UNITY_EDITOR)
#pragma warning disable
namespace BestHTTP.SecureProtocol.Org.BouncyCastle.Bcpg
{
    using System.IO;

    /// <summary>Basic type for a trust packet.</summary>
    public class TrustPacket
        : ContainedPacket
    {
        private readonly byte[] levelAndTrustAmount;

        public TrustPacket(
            BcpgInputStream bcpgIn)
        {
            var bOut = new MemoryStream();

            int ch;
            while ((ch = bcpgIn.ReadByte()) >= 0) bOut.WriteByte((byte)ch);

            this.levelAndTrustAmount = bOut.ToArray();
        }

        public TrustPacket(
            int trustCode)
        {
            this.levelAndTrustAmount = new[] { (byte)trustCode };
        }

        public byte[] GetLevelAndTrustAmount() { return (byte[])this.levelAndTrustAmount.Clone(); }

        public override void Encode(
            BcpgOutputStream bcpgOut)
        {
            bcpgOut.WritePacket(PacketTag.Trust, this.levelAndTrustAmount, true);
        }
    }
}
#pragma warning restore
#endif