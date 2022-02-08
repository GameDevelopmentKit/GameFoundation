#if !BESTHTTP_DISABLE_ALTERNATE_SSL && (!UNITY_WEBGL || UNITY_EDITOR)
#pragma warning disable
namespace BestHTTP.SecureProtocol.Org.BouncyCastle.Tls
{
    using System.Collections;
    using System.IO;
    using BestHTTP.SecureProtocol.Org.BouncyCastle.Utilities;

    public sealed class ServerHello
    {
        private static readonly byte[] HelloRetryRequestMagic =
        {
            0xCF, 0x21, 0xAD, 0x74, 0xE5, 0x9A, 0x61, 0x11, 0xBE,
            0x1D, 0x8C, 0x02, 0x1E, 0x65, 0xB8, 0x91, 0xC2, 0xA2, 0x11, 0x16, 0x7A, 0xBB, 0x8C, 0x5E, 0x07, 0x9E, 0x09,
            0xE2, 0xC8, 0xA8, 0x33, 0x9C
        };

        public ServerHello(byte[] sessionID, int cipherSuite, IDictionary extensions)
            : this(ProtocolVersion.TLSv12, Arrays.Clone(HelloRetryRequestMagic), sessionID, cipherSuite, extensions)
        {
        }

        public ServerHello(ProtocolVersion version, byte[] random, byte[] sessionID, int cipherSuite,
            IDictionary extensions)
        {
            this.Version     = version;
            this.Random      = random;
            this.SessionID   = sessionID;
            this.CipherSuite = cipherSuite;
            this.Extensions  = extensions;
        }

        public int CipherSuite { get; }

        public IDictionary Extensions { get; }

        public byte[] Random { get; }

        public byte[] SessionID { get; }

        public ProtocolVersion Version { get; }

        public bool IsHelloRetryRequest() { return Arrays.AreEqual(HelloRetryRequestMagic, this.Random); }

        /// <summary>Encode this <see cref="ServerHello" /> to a <see cref="Stream" />.</summary>
        /// <param name="context">the <see cref="TlsContext" /> of the current connection.</param>
        /// <param name="output">the <see cref="Stream" /> to encode to.</param>
        /// <exception cref="IOException" />
        public void Encode(TlsContext context, Stream output)
        {
            TlsUtilities.WriteVersion(this.Version, output);

            output.Write(this.Random, 0, this.Random.Length);

            TlsUtilities.WriteOpaque8(this.SessionID, output);

            TlsUtilities.WriteUint16(this.CipherSuite, output);

            TlsUtilities.WriteUint8(CompressionMethod.cls_null, output);

            TlsProtocol.WriteExtensions(output, this.Extensions);
        }

        /// <summary>Parse a <see cref="ServerHello" /> from a <see cref="MemoryStream" />.</summary>
        /// <param name="input">the <see cref="MemoryStream" /> to parse from.</param>
        /// <returns>a <see cref="ServerHello" /> object.</returns>
        /// <exception cref="IOException" />
        public static ServerHello Parse(MemoryStream input)
        {
            var version = TlsUtilities.ReadVersion(input);

            var random = TlsUtilities.ReadFully(32, input);

            var sessionID = TlsUtilities.ReadOpaque8(input, 0, 32);

            var cipherSuite = TlsUtilities.ReadUint16(input);

            var compressionMethod = TlsUtilities.ReadUint8(input);
            if (CompressionMethod.cls_null != compressionMethod)
                throw new TlsFatalAlert(AlertDescription.illegal_parameter);

            var extensions = TlsProtocol.ReadExtensions(input);

            return new ServerHello(version, random, sessionID, cipherSuite, extensions);
        }
    }
}
#pragma warning restore
#endif