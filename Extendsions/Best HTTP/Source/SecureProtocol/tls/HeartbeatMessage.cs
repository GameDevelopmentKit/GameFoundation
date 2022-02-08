#if !BESTHTTP_DISABLE_ALTERNATE_SSL && (!UNITY_WEBGL || UNITY_EDITOR)
#pragma warning disable
namespace BestHTTP.SecureProtocol.Org.BouncyCastle.Tls
{
    using System;
    using System.IO;
    using BestHTTP.SecureProtocol.Org.BouncyCastle.Utilities;
    using BestHTTP.SecureProtocol.Org.BouncyCastle.Utilities.IO;

    public sealed class HeartbeatMessage
    {
        public static HeartbeatMessage Create(TlsContext context, short type, byte[] payload) { return Create(context, type, payload, 16); }

        public static HeartbeatMessage Create(TlsContext context, short type, byte[] payload, int paddingLength)
        {
            var padding = context.NonceGenerator.GenerateNonce(paddingLength);

            return new HeartbeatMessage(type, payload, padding);
        }

        private readonly byte[] m_padding;

        public HeartbeatMessage(short type, byte[] payload, byte[] padding)
        {
            if (!HeartbeatMessageType.IsValid(type))
                throw new ArgumentException("not a valid HeartbeatMessageType value", "type");
            if (null == payload || payload.Length >= 1 << 16)
                throw new ArgumentException("must have length < 2^16", "payload");
            if (null == padding || padding.Length < 16)
                throw new ArgumentException("must have length >= 16", "padding");

            this.Type      = type;
            this.Payload   = payload;
            this.m_padding = padding;
        }

        public int PaddingLength =>
            /*
             * RFC 6520 4. The padding of a received HeartbeatMessage message MUST be ignored
             */
            this.m_padding.Length;

        public byte[] Payload { get; }

        public short Type { get; }

        /// <summary>Encode this <see cref="HeartbeatMessage" /> to a <see cref="Stream" />.</summary>
        /// <param name="output">the <see cref="Stream" /> to encode to.</param>
        /// <exception cref="IOException" />
        public void Encode(Stream output)
        {
            TlsUtilities.WriteUint8(this.Type, output);

            TlsUtilities.CheckUint16(this.Payload.Length);
            TlsUtilities.WriteUint16(this.Payload.Length, output);
            output.Write(this.Payload, 0, this.Payload.Length);

            output.Write(this.m_padding, 0, this.m_padding.Length);
        }

        /// <summary>Parse a <see cref="HeartbeatMessage" /> from a <see cref="Stream" />.</summary>
        /// <param name="input">the <see cref="Stream" /> to parse from.</param>
        /// <returns>a <see cref="HeartbeatMessage" /> object.</returns>
        /// <exception cref="IOException" />
        public static HeartbeatMessage Parse(Stream input)
        {
            var type = TlsUtilities.ReadUint8(input);
            if (!HeartbeatMessageType.IsValid(type))
                throw new TlsFatalAlert(AlertDescription.illegal_parameter);

            var payload_length = TlsUtilities.ReadUint16(input);
            var payloadBuffer  = Streams.ReadAll(input);

            var payload = GetPayload(payloadBuffer, payload_length);
            if (null == payload)
                /*
                     * RFC 6520 4. If the payload_length of a received HeartbeatMessage is too large, the received
                     * HeartbeatMessage MUST be discarded silently.
                     */
                return null;

            var padding = GetPadding(payloadBuffer, payload_length);

            return new HeartbeatMessage(type, payload, padding);
        }

        private static byte[] GetPayload(byte[] payloadBuffer, int payloadLength)
        {
            /*
             * RFC 6520 4. The padding_length MUST be at least 16.
             */
            var maxPayloadLength = payloadBuffer.Length - 16;
            if (payloadLength > maxPayloadLength)
                return null;

            return Arrays.CopyOf(payloadBuffer, payloadLength);
        }

        private static byte[] GetPadding(byte[] payloadBuffer, int payloadLength) { return TlsUtilities.CopyOfRangeExact(payloadBuffer, payloadLength, payloadBuffer.Length); }
    }
}
#pragma warning restore
#endif