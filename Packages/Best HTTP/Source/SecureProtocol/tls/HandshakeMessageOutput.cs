#if !BESTHTTP_DISABLE_ALTERNATE_SSL && (!UNITY_WEBGL || UNITY_EDITOR)
#pragma warning disable
namespace BestHTTP.SecureProtocol.Org.BouncyCastle.Tls
{
    using System.IO;
    using BestHTTP.SecureProtocol.Org.BouncyCastle.Utilities;

    internal sealed class HandshakeMessageOutput
        : MemoryStream
    {
        internal static int GetLength(int bodyLength) { return 4 + bodyLength; }

        /// <exception cref="IOException" />
        internal static void Send(TlsProtocol protocol, short handshakeType, byte[] body)
        {
            var message = new HandshakeMessageOutput(handshakeType, body.Length);
            message.Write(body, 0, body.Length);
            message.Send(protocol);
        }

        /// <exception cref="IOException" />
        internal HandshakeMessageOutput(short handshakeType)
            : this(handshakeType, 60)
        {
        }

        /// <exception cref="IOException" />
        internal HandshakeMessageOutput(short handshakeType, int bodyLength)
            : base(GetLength(bodyLength))
        {
            TlsUtilities.CheckUint8(handshakeType);
            TlsUtilities.WriteUint8(handshakeType, this);
            // Reserve space for length
            this.Seek(3L, SeekOrigin.Current);
        }

        /// <exception cref="IOException" />
        internal void Send(TlsProtocol protocol)
        {
            // Patch actual length back in
            var bodyLength = (int)this.Length - 4;
            TlsUtilities.CheckUint24(bodyLength);

            this.Seek(1L, SeekOrigin.Begin);
            TlsUtilities.WriteUint24(bodyLength, this);

#if PORTABLE || NETFX_CORE
            byte[] buf = ToArray();
            int count = buf.Length;
#else
            var buf   = this.GetBuffer();
            var count = (int)this.Length;
#endif
            protocol.WriteHandshakeMessage(buf, 0, count);

            Platform.Dispose(this);
        }

        internal void PrepareClientHello(TlsHandshakeHash handshakeHash, int bindersSize)
        {
            // Patch actual length back in
            var bodyLength = (int)this.Length - 4 + bindersSize;
            TlsUtilities.CheckUint24(bodyLength);

            this.Seek(1L, SeekOrigin.Begin);
            TlsUtilities.WriteUint24(bodyLength, this);

#if PORTABLE || NETFX_CORE
            byte[] buf = ToArray();
            int count = buf.Length;
#else
            var buf   = this.GetBuffer();
            var count = (int)this.Length;
#endif

            handshakeHash.Update(buf, 0, count);

            this.Seek(0L, SeekOrigin.End);
        }

        internal void SendClientHello(TlsClientProtocol clientProtocol, TlsHandshakeHash handshakeHash, int bindersSize)
        {
#if PORTABLE || NETFX_CORE
            byte[] buf = ToArray();
            int count = buf.Length;
#else
            var buf   = this.GetBuffer();
            var count = (int)this.Length;
#endif

            if (bindersSize > 0) handshakeHash.Update(buf, count - bindersSize, bindersSize);

            clientProtocol.WriteHandshakeMessage(buf, 0, count);

            Platform.Dispose(this);
        }
    }
}
#pragma warning restore
#endif