#if !BESTHTTP_DISABLE_ALTERNATE_SSL && (!UNITY_WEBGL || UNITY_EDITOR)
#pragma warning disable
namespace BestHTTP.SecureProtocol.Org.BouncyCastle.Tls
{
    using System;
    using System.IO;
    using BestHTTP.SecureProtocol.Org.BouncyCastle.Asn1;
    using BestHTTP.SecureProtocol.Org.BouncyCastle.Asn1.X509;
    using BestHTTP.SecureProtocol.Org.BouncyCastle.Utilities;

    public sealed class TrustedAuthority
    {
        public TrustedAuthority(short identifierType, object identifier)
        {
            if (!IsCorrectType(identifierType, identifier))
                throw new ArgumentException("not an instance of the correct type", "identifier");

            this.IdentifierType = identifierType;
            this.Identifier     = identifier;
        }

        public short IdentifierType { get; }

        public object Identifier { get; }

        public byte[] GetCertSha1Hash() { return Arrays.Clone((byte[])this.Identifier); }

        public byte[] GetKeySha1Hash() { return Arrays.Clone((byte[])this.Identifier); }

        public X509Name X509Name
        {
            get
            {
                this.CheckCorrectType(Tls.IdentifierType.x509_name);
                return (X509Name)this.Identifier;
            }
        }

        /// <summary>Encode this <see cref="TrustedAuthority" /> to a <see cref="Stream" />.</summary>
        /// <param name="output">the <see cref="Stream" /> to encode to.</param>
        /// <exception cref="IOException" />
        public void Encode(Stream output)
        {
            TlsUtilities.WriteUint8(this.IdentifierType, output);

            switch (this.IdentifierType)
            {
                case Tls.IdentifierType.cert_sha1_hash:
                case Tls.IdentifierType.key_sha1_hash:
                {
                    var sha1Hash = (byte[])this.Identifier;
                    output.Write(sha1Hash, 0, sha1Hash.Length);
                    break;
                }
                case Tls.IdentifierType.pre_agreed:
                {
                    break;
                }
                case Tls.IdentifierType.x509_name:
                {
                    var dn          = (X509Name)this.Identifier;
                    var derEncoding = dn.GetEncoded(Asn1Encodable.Der);
                    TlsUtilities.WriteOpaque16(derEncoding, output);
                    break;
                }
                default:
                    throw new TlsFatalAlert(AlertDescription.internal_error);
            }
        }

        /// <summary>Parse a <see cref="TrustedAuthority" /> from a <see cref="Stream" />.</summary>
        /// <param name="input">the <see cref="Stream" /> to parse from.</param>
        /// <returns>a <see cref="TrustedAuthority" /> object.</returns>
        /// <exception cref="IOException" />
        public static TrustedAuthority Parse(Stream input)
        {
            var    identifier_type = TlsUtilities.ReadUint8(input);
            object identifier;

            switch (identifier_type)
            {
                case Tls.IdentifierType.cert_sha1_hash:
                case Tls.IdentifierType.key_sha1_hash:
                {
                    identifier = TlsUtilities.ReadFully(20, input);
                    break;
                }
                case Tls.IdentifierType.pre_agreed:
                {
                    identifier = null;
                    break;
                }
                case Tls.IdentifierType.x509_name:
                {
                    var derEncoding = TlsUtilities.ReadOpaque16(input, 1);
                    var asn1        = TlsUtilities.ReadDerObject(derEncoding);
                    identifier = X509Name.GetInstance(asn1);
                    break;
                }
                default:
                    throw new TlsFatalAlert(AlertDescription.decode_error);
            }

            return new TrustedAuthority(identifier_type, identifier);
        }

        private void CheckCorrectType(short expectedIdentifierType)
        {
            if (this.IdentifierType != expectedIdentifierType || !IsCorrectType(expectedIdentifierType, this.Identifier))
                throw new InvalidOperationException("TrustedAuthority is not of type "
                                                    + Tls.IdentifierType.GetName(expectedIdentifierType));
        }

        private static bool IsCorrectType(short identifierType, object identifier)
        {
            switch (identifierType)
            {
                case Tls.IdentifierType.cert_sha1_hash:
                case Tls.IdentifierType.key_sha1_hash:
                    return IsSha1Hash(identifier);
                case Tls.IdentifierType.pre_agreed:
                    return identifier == null;
                case Tls.IdentifierType.x509_name:
                    return identifier is X509Name;
                default:
                    throw new ArgumentException("unsupported IdentifierType", "identifierType");
            }
        }

        private static bool IsSha1Hash(object identifier) { return identifier is byte[] && ((byte[])identifier).Length == 20; }
    }
}
#pragma warning restore
#endif