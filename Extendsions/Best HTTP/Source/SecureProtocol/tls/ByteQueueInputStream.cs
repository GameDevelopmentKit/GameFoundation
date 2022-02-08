#if !BESTHTTP_DISABLE_ALTERNATE_SSL && (!UNITY_WEBGL || UNITY_EDITOR)
#pragma warning disable
namespace BestHTTP.SecureProtocol.Org.BouncyCastle.Tls
{
    using System;
    using BestHTTP.SecureProtocol.Org.BouncyCastle.Utilities.IO;

    public sealed class ByteQueueInputStream
        : BaseInputStream
    {
        private readonly ByteQueue m_buffer;

        public ByteQueueInputStream() { this.m_buffer = new ByteQueue(); }

        public void AddBytes(byte[] buf) { this.m_buffer.AddData(buf, 0, buf.Length); }

        public void AddBytes(byte[] buf, int bufOff, int bufLen) { this.m_buffer.AddData(buf, bufOff, bufLen); }

        public int Peek(byte[] buf)
        {
            var bytesToRead = Math.Min(this.m_buffer.Available, buf.Length);
            this.m_buffer.Read(buf, 0, bytesToRead, 0);
            return bytesToRead;
        }

        public override int ReadByte()
        {
            if (this.m_buffer.Available == 0)
                return -1;

            return this.m_buffer.RemoveData(1, 0)[0];
        }

        public override int Read(byte[] buf, int off, int len)
        {
            var bytesToRead = Math.Min(this.m_buffer.Available, len);
            this.m_buffer.RemoveData(buf, off, bytesToRead, 0);
            return bytesToRead;
        }

        public long Skip(long n)
        {
            var bytesToRemove = Math.Min((int)n, this.m_buffer.Available);
            this.m_buffer.RemoveData(bytesToRemove);
            return bytesToRemove;
        }

        public int Available => this.m_buffer.Available;

#if PORTABLE || NETFX_CORE
        //protected override void Dispose(bool disposing)
        //{
        //    base.Dispose(disposing);
        //}
#else
        public override void Close() { }
#endif
    }
}
#pragma warning restore
#endif