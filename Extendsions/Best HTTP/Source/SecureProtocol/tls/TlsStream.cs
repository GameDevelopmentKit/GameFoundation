#if !BESTHTTP_DISABLE_ALTERNATE_SSL && (!UNITY_WEBGL || UNITY_EDITOR)
#pragma warning disable
namespace BestHTTP.SecureProtocol.Org.BouncyCastle.Tls
{
    using System;
    using System.IO;

    internal class TlsStream
        : Stream
    {
        private readonly TlsProtocol m_handler;

        internal TlsStream(TlsProtocol handler) { this.m_handler = handler; }

        public override bool CanRead => !this.m_handler.IsClosed;

        public override bool CanSeek => false;

        public override bool CanWrite => !this.m_handler.IsClosed;

#if PORTABLE || NETFX_CORE
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                m_handler.Close();
            }
            base.Dispose(disposing);
        }
#else
        public override void Close()
        {
            this.m_handler.Close();
            base.Close();
        }
#endif

        public override void Flush() { this.m_handler.Flush(); }

        public override long Length => throw new NotSupportedException();

        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }

        public override int Read(byte[] buf, int off, int len) { return this.m_handler.ReadApplicationData(buf, off, len); }

        public override int ReadByte()
        {
            var buf = new byte[1];
            var ret = this.Read(buf, 0, 1);
            return ret <= 0 ? -1 : buf[0];
        }

        public override long Seek(long offset, SeekOrigin origin) { throw new NotSupportedException(); }

        public override void SetLength(long value) { throw new NotSupportedException(); }

        public override void Write(byte[] buf, int off, int len) { this.m_handler.WriteApplicationData(buf, off, len); }

        public override void WriteByte(byte b) { this.Write(new[] { b }, 0, 1); }
    }
}
#pragma warning restore
#endif