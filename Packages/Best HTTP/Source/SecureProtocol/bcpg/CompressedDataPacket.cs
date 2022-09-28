#if !BESTHTTP_DISABLE_ALTERNATE_SSL && (!UNITY_WEBGL || UNITY_EDITOR)
#pragma warning disable
namespace BestHTTP.SecureProtocol.Org.BouncyCastle.Bcpg
{
    /// <remarks>Generic compressed data object.</remarks>
    public class CompressedDataPacket
        : InputStreamPacket
    {
        internal CompressedDataPacket(
            BcpgInputStream bcpgIn)
            : base(bcpgIn)
        {
            this.Algorithm = (CompressionAlgorithmTag)bcpgIn.ReadByte();
        }

        /// <summary>The algorithm tag value.</summary>
        public CompressionAlgorithmTag Algorithm { get; }
    }
}
#pragma warning restore
#endif