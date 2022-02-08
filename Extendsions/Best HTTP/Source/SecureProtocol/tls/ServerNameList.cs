#if !BESTHTTP_DISABLE_ALTERNATE_SSL && (!UNITY_WEBGL || UNITY_EDITOR)
#pragma warning disable
namespace BestHTTP.SecureProtocol.Org.BouncyCastle.Tls
{
    using System;
    using System.Collections;
    using System.IO;
    using BestHTTP.SecureProtocol.Org.BouncyCastle.Utilities;
    using BestHTTP.SecureProtocol.Org.BouncyCastle.Utilities.IO;

    public sealed class ServerNameList
    {
        /// <param name="serverNameList">an <see cref="IList" /> of <see cref="ServerName" />.</param>
        public ServerNameList(IList serverNameList)
        {
            if (null == serverNameList)
                throw new ArgumentNullException("serverNameList");

            this.ServerNames = serverNameList;
        }

        /// <returns>an <see cref="IList" /> of <see cref="ServerName" />.</returns>
        public IList ServerNames { get; }

        /// <summary>Encode this <see cref="ServerNameList" /> to a <see cref="Stream" />.</summary>
        /// <param name="output">the <see cref="Stream" /> to encode to .</param>
        /// <exception cref="IOException" />
        public void Encode(Stream output)
        {
            var buf = new MemoryStream();

            var nameTypesSeen = TlsUtilities.EmptyShorts;
            foreach (ServerName entry in this.ServerNames)
            {
                nameTypesSeen = CheckNameType(nameTypesSeen, entry.NameType);
                if (null == nameTypesSeen)
                    throw new TlsFatalAlert(AlertDescription.internal_error);

                entry.Encode(buf);
            }

            var length = (int)buf.Length;
            TlsUtilities.CheckUint16(length);
            TlsUtilities.WriteUint16(length, output);
            Streams.WriteBufTo(buf, output);
        }

        /// <summary>Parse a <see cref="ServerNameList" /> from a <see cref="Stream" />.</summary>
        /// <param name="input">the <see cref="Stream" /> to parse from.</param>
        /// <returns>a <see cref="ServerNameList" /> object.</returns>
        /// <exception cref="IOException" />
        public static ServerNameList Parse(Stream input)
        {
            var data = TlsUtilities.ReadOpaque16(input, 1);

            var buf = new MemoryStream(data, false);

            var nameTypesSeen    = TlsUtilities.EmptyShorts;
            var server_name_list = Platform.CreateArrayList();
            while (buf.Position < buf.Length)
            {
                var entry = ServerName.Parse(buf);

                nameTypesSeen = CheckNameType(nameTypesSeen, entry.NameType);
                if (null == nameTypesSeen)
                    throw new TlsFatalAlert(AlertDescription.illegal_parameter);

                server_name_list.Add(entry);
            }

            return new ServerNameList(server_name_list);
        }

        private static short[] CheckNameType(short[] nameTypesSeen, short nameType)
        {
            // RFC 6066 3. The ServerNameList MUST NOT contain more than one name of the same NameType.
            if (Arrays.Contains(nameTypesSeen, nameType))
                return null;

            return Arrays.Append(nameTypesSeen, nameType);
        }
    }
}
#pragma warning restore
#endif