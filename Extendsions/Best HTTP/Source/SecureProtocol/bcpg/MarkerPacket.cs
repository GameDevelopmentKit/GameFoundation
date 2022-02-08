#if !BESTHTTP_DISABLE_ALTERNATE_SSL && (!UNITY_WEBGL || UNITY_EDITOR)
#pragma warning disable
namespace BestHTTP.SecureProtocol.Org.BouncyCastle.Bcpg
{
    /// <remarks>Basic type for a marker packet.</remarks>
    public class MarkerPacket
        : ContainedPacket
    {
        // "PGP"
        private readonly byte[] marker = { 0x50, 0x47, 0x50 };

        public MarkerPacket(
            BcpgInputStream bcpgIn)
        {
            bcpgIn.ReadFully(this.marker);
        }

        public override void Encode(
            BcpgOutputStream bcpgOut)
        {
            bcpgOut.WritePacket(PacketTag.Marker, this.marker, true);
        }
    }
}
#pragma warning restore
#endif