#if !BESTHTTP_DISABLE_ALTERNATE_SSL && (!UNITY_WEBGL || UNITY_EDITOR)
#pragma warning disable
namespace BestHTTP.SecureProtocol.Org.BouncyCastle.Tls
{
    using System.Collections;
    using System.IO;
    using BestHTTP.SecureProtocol.Org.BouncyCastle.Utilities;
    using BestHTTP.SecureProtocol.Org.BouncyCastle.Utilities.IO;

    public sealed class ClientHello
    {
        public ClientHello(ProtocolVersion version, byte[] random, byte[] sessionID, byte[] cookie,
            int[] cipherSuites, IDictionary extensions, int bindersSize)
        {
            this.Version      = version;
            this.Random       = random;
            this.SessionID    = sessionID;
            this.Cookie       = cookie;
            this.CipherSuites = cipherSuites;
            this.Extensions   = extensions;
            this.BindersSize  = bindersSize;
        }

        public int BindersSize { get; }

        public int[] CipherSuites { get; }

        public byte[] Cookie { get; }

        public IDictionary Extensions { get; }

        public byte[] Random { get; }

        public byte[] SessionID { get; }

        public ProtocolVersion Version { get; }

        /// <summary>Encode this <see cref="ClientHello" /> to a <see cref="Stream" />.</summary>
        /// <param name="context">the <see cref="TlsContext" /> of the current connection.</param>
        /// <param name="output">the <see cref="Stream" /> to encode to.</param>
        /// <exception cref="IOException" />
        public void Encode(TlsContext context, Stream output)
        {
            if (this.BindersSize < 0)
                throw new TlsFatalAlert(AlertDescription.internal_error);

            TlsUtilities.WriteVersion(this.Version, output);

            output.Write(this.Random, 0, this.Random.Length);

            TlsUtilities.WriteOpaque8(this.SessionID, output);

            if (null != this.Cookie) TlsUtilities.WriteOpaque8(this.Cookie, output);

            TlsUtilities.WriteUint16ArrayWithUint16Length(this.CipherSuites, output);

            TlsUtilities.WriteUint8ArrayWithUint8Length(new[] { CompressionMethod.cls_null }, output);

            TlsProtocol.WriteExtensions(output, this.Extensions, this.BindersSize);
        }

        /// <summary>Parse a <see cref="ClientHello" /> from a <see cref="MemoryStream" />.</summary>
        /// <param name="messageInput">the <see cref="MemoryStream" /> to parse from.</param>
        /// <param name="dtlsOutput">
        ///     for DTLS this should be non-null; the input is copied to this
        ///     <see cref="Stream" />, minus the cookie field.
        /// </param>
        /// <returns>a <see cref="ClientHello" /> object.</returns>
        /// <exception cref="TlsFatalAlert" />
        public static ClientHello Parse(MemoryStream messageInput, Stream dtlsOutput)
        {
            try
            {
                return ImplParse(messageInput, dtlsOutput);
            }
            catch (TlsFatalAlert e)
            {
                throw e;
            }
            catch (IOException e)
            {
                throw new TlsFatalAlert(AlertDescription.decode_error, e);
            }
        }

        /// <exception cref="IOException" />
        private static ClientHello ImplParse(MemoryStream messageInput, Stream dtlsOutput)
        {
            Stream input                  = messageInput;
            if (null != dtlsOutput) input = new TeeInputStream(input, dtlsOutput);

            var clientVersion = TlsUtilities.ReadVersion(input);

            var random = TlsUtilities.ReadFully(32, input);

            var sessionID = TlsUtilities.ReadOpaque8(input, 0, 32);

            byte[] cookie = null;
            if (null != dtlsOutput)
            {
                /*
                 * RFC 6347 This specification increases the cookie size limit to 255 bytes for greater
                 * future flexibility. The limit remains 32 for previous versions of DTLS.
                 */
                var maxCookieLength = ProtocolVersion.DTLSv12.IsEqualOrEarlierVersionOf(clientVersion) ? 255 : 32;

                cookie = TlsUtilities.ReadOpaque8(messageInput, 0, maxCookieLength);
            }

            var cipher_suites_length = TlsUtilities.ReadUint16(input);
            if (cipher_suites_length < 2 || (cipher_suites_length & 1) != 0
                                         || (int)(messageInput.Length - messageInput.Position) < cipher_suites_length)
                throw new TlsFatalAlert(AlertDescription.decode_error);

            /*
             * NOTE: "If the session_id field is not empty (implying a session resumption request) this
             * vector must include at least the cipher_suite from that session."
             */
            var cipherSuites = TlsUtilities.ReadUint16Array(cipher_suites_length / 2, input);

            var compressionMethods = TlsUtilities.ReadUint8ArrayWithUint8Length(input, 1);
            if (!Arrays.Contains(compressionMethods, CompressionMethod.cls_null))
                throw new TlsFatalAlert(AlertDescription.handshake_failure);

            /*
             * NOTE: Can't use TlsProtocol.ReadExtensions directly because TeeInputStream a) won't have
             * 'Length' or 'Position' properties in the FIPS provider, b) isn't a MemoryStream.
             */
            IDictionary extensions = null;
            if (messageInput.Position < messageInput.Length)
            {
                var extBytes = TlsUtilities.ReadOpaque16(input);

                TlsProtocol.AssertEmpty(messageInput);

                extensions = TlsProtocol.ReadExtensionsDataClientHello(extBytes);
            }

            return new ClientHello(clientVersion, random, sessionID, cookie, cipherSuites, extensions, -1);
        }
    }
}
#pragma warning restore
#endif