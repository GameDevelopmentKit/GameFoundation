#if !BESTHTTP_DISABLE_ALTERNATE_SSL && (!UNITY_WEBGL || UNITY_EDITOR)
#pragma warning disable
namespace BestHTTP.SecureProtocol.Org.BouncyCastle.Tls
{
    using BestHTTP.SecureProtocol.Org.BouncyCastle.Utilities.IO;

    /// <summary>OutputStream based on a ByteQueue implementation.</summary>
    public sealed class ByteQueueOutputStream
        : BaseOutputStream
    {
        public ByteQueueOutputStream() { this.Buffer = new ByteQueue(); }

        public ByteQueue Buffer { get; }

        public override void WriteByte(byte b) { this.Buffer.AddData(new[] { b }, 0, 1); }

        public override void Write(byte[] buf, int off, int len) { this.Buffer.AddData(buf, off, len); }
    }
}
#pragma warning restore
#endif