#if !BESTHTTP_DISABLE_ALTERNATE_SSL && (!UNITY_WEBGL || UNITY_EDITOR)
#pragma warning disable
namespace BestHTTP.SecureProtocol.Org.BouncyCastle.Bcpg
{
    /// <remarks>Basic packet for an experimental packet.</remarks>
    public class ExperimentalPacket
        : ContainedPacket //, PublicKeyAlgorithmTag
    {
        private readonly byte[] contents;

        internal ExperimentalPacket(
            PacketTag tag,
            BcpgInputStream bcpgIn)
        {
            this.Tag = tag;

            this.contents = bcpgIn.ReadAll();
        }

        public PacketTag Tag { get; }

        public byte[] GetContents() { return (byte[])this.contents.Clone(); }

        public override void Encode(
            BcpgOutputStream bcpgOut)
        {
            bcpgOut.WritePacket(this.Tag, this.contents, true);
        }
    }
}
#pragma warning restore
#endif