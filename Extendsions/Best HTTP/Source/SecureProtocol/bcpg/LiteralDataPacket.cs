#if !BESTHTTP_DISABLE_ALTERNATE_SSL && (!UNITY_WEBGL || UNITY_EDITOR)
#pragma warning disable
namespace BestHTTP.SecureProtocol.Org.BouncyCastle.Bcpg
{
	using System.IO;
	using BestHTTP.SecureProtocol.Org.BouncyCastle.Utilities;

	/// <remarks>Generic literal data packet.</remarks>
    public class LiteralDataPacket
        : InputStreamPacket
    {
        private readonly byte[] fileName;

        internal LiteralDataPacket(
            BcpgInputStream bcpgIn)
            : base(bcpgIn)
        {
            this.Format = bcpgIn.ReadByte();
            var len = bcpgIn.ReadByte();

            this.fileName = new byte[len];
            for (var i = 0; i != len; ++i)
            {
                var ch = bcpgIn.ReadByte();
                if (ch < 0)
                    throw new IOException("literal data truncated in header");

                this.fileName[i] = (byte)ch;
            }

            this.ModificationTime = (((uint)bcpgIn.ReadByte() << 24)
                                     | ((uint)bcpgIn.ReadByte() << 16)
                                     | ((uint)bcpgIn.ReadByte() << 8)
                                     | (uint)bcpgIn.ReadByte()) * 1000L;
        }

        /// <summary>The format tag value.</summary>
        public int Format { get; }

        /// <summary>The modification time of the file in milli-seconds (since Jan 1, 1970 UTC)</summary>
        public long ModificationTime { get; }

        public string FileName => Strings.FromUtf8ByteArray(this.fileName);

        public byte[] GetRawFileName() { return Arrays.Clone(this.fileName); }
    }
}
#pragma warning restore
#endif