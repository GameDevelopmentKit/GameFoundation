#if !BESTHTTP_DISABLE_ALTERNATE_SSL && (!UNITY_WEBGL || UNITY_EDITOR)
#pragma warning disable
namespace BestHTTP.SecureProtocol.Org.BouncyCastle.Tls
{
    using System;
    using System.IO;

    /// <summary>RFC 6066 3. Server Name Indication</summary>
    /// <remarks>
    ///     Current implementation uses this guidance: "For backward compatibility, all future data structures associated
    ///     with new NameTypes MUST begin with a 16-bit length field. TLS MAY treat provided server names as opaque data
    ///     and pass the names and types to the application.". RFC 6066 specifies ASCII encoding for host_name (possibly
    ///     using A-labels for IDNs), but note that the previous version (RFC 4366) specified UTF-8 encoding (see RFC 6066
    ///     Appendix A). For maximum compatibility, it is recommended that client code tolerate receiving UTF-8 from the
    ///     peer, but only generate ASCII itself.
    /// </remarks>
    public sealed class ServerName
    {
        public ServerName(short nameType, byte[] nameData)
        {
            if (!TlsUtilities.IsValidUint8(nameType))
                throw new ArgumentException("must be from 0 to 255", "nameType");
            if (null == nameData)
                throw new ArgumentNullException("nameData");
            if (nameData.Length < 1 || !TlsUtilities.IsValidUint16(nameData.Length))
                throw new ArgumentException("must have length from 1 to 65535", "nameData");

            this.NameType = nameType;
            this.NameData = nameData;
        }

        public byte[] NameData { get; }

        public short NameType { get; }

        /// <summary>Encode this <see cref="ServerName" /> to a <see cref="Stream" />.</summary>
        /// <param name="output">the <see cref="Stream" /> to encode to.</param>
        /// <exception cref="IOException" />
        public void Encode(Stream output)
        {
            TlsUtilities.WriteUint8(this.NameType, output);
            TlsUtilities.WriteOpaque16(this.NameData, output);
        }

        /// <summary>Parse a <see cref="ServerName" /> from a <see cref="Stream" />.</summary>
        /// <param name="input">the <see cref="Stream" /> to parse from.</param>
        /// <returns>a <see cref="ServerName" /> object.</returns>
        /// <exception cref="IOException" />
        public static ServerName Parse(Stream input)
        {
            var name_type = TlsUtilities.ReadUint8(input);
            var nameData  = TlsUtilities.ReadOpaque16(input, 1);
            return new ServerName(name_type, nameData);
        }
    }
}
#pragma warning restore
#endif