#if !BESTHTTP_DISABLE_ALTERNATE_SSL && (!UNITY_WEBGL || UNITY_EDITOR)
#pragma warning disable
namespace BestHTTP.SecureProtocol.Org.BouncyCastle.Tls
{
    using System;
    using System.Collections;
    using System.IO;
    using BestHTTP.SecureProtocol.Org.BouncyCastle.Asn1;
    using BestHTTP.SecureProtocol.Org.BouncyCastle.Asn1.X509;
    using BestHTTP.SecureProtocol.Org.BouncyCastle.Utilities;

    /// <summary>Parsing and encoding of a <i>CertificateRequest</i> struct from RFC 4346.</summary>
    /// <remarks>
    ///     <pre>
    ///         struct {
    ///         ClientCertificateType certificate_types&lt;1..2^8-1&gt;;
    ///         DistinguishedName certificate_authorities&lt;3..2^16-1&gt;;
    ///         } CertificateRequest;
    ///     </pre>
    ///     Updated for RFC 5246:
    ///     <pre>
    ///         struct {
    ///         ClientCertificateType certificate_types &lt;1..2 ^ 8 - 1&gt;;
    ///         SignatureAndHashAlgorithm supported_signature_algorithms &lt;2 ^ 16 - 1&gt;;
    ///         DistinguishedName certificate_authorities &lt;0..2 ^ 16 - 1&gt;;
    ///         } CertificateRequest;
    ///     </pre>
    ///     Revised for RFC 8446:
    ///     <pre>
    ///         struct {
    ///         opaque certificate_request_context &lt;0..2 ^ 8 - 1&gt;;
    ///         Extension extensions &lt;2..2 ^ 16 - 1&gt;;
    ///         } CertificateRequest;
    ///     </pre>
    /// </remarks>
    /// <seealso cref="ClientCertificateType" />
    /// <seealso cref="X509Name" />
    public sealed class CertificateRequest
    {
        /// <exception cref="IOException" />
        private static IList CheckSupportedSignatureAlgorithms(IList supportedSignatureAlgorithms,
            short alertDescription)
        {
            if (null == supportedSignatureAlgorithms)
                throw new TlsFatalAlert(alertDescription, "'signature_algorithms' is required");

            return supportedSignatureAlgorithms;
        }

        private readonly byte[] m_certificateRequestContext;

        /// <param name="certificateTypes">see <see cref="ClientCertificateType" /> for valid constants.</param>
        /// <param name="supportedSignatureAlgorithms"></param>
        /// <param name="certificateAuthorities">an <see cref="IList" /> of <see cref="X509Name" />.</param>
        public CertificateRequest(short[] certificateTypes, IList supportedSignatureAlgorithms,
            IList certificateAuthorities)
            : this(null, certificateTypes, supportedSignatureAlgorithms, null, certificateAuthorities)
        {
        }

        // TODO[tls13] Prefer to manage the certificateRequestContext internally only? 
        /// <exception cref="IOException" />
        public CertificateRequest(byte[] certificateRequestContext, IList supportedSignatureAlgorithms,
            IList supportedSignatureAlgorithmsCert, IList certificateAuthorities)
            : this(certificateRequestContext, null,
                CheckSupportedSignatureAlgorithms(supportedSignatureAlgorithms, AlertDescription.internal_error),
                supportedSignatureAlgorithmsCert, certificateAuthorities)
        {
            /*
             * TODO[tls13] Removed certificateTypes, added certificate_request_context, added extensions
             * (required: signature_algorithms, optional: status_request, signed_certificate_timestamp,
             * certificate_authorities, oid_filters, signature_algorithms_cert)
             */
        }

        private CertificateRequest(byte[] certificateRequestContext, short[] certificateTypes,
            IList supportedSignatureAlgorithms, IList supportedSignatureAlgorithmsCert, IList certificateAuthorities)
        {
            if (null != certificateRequestContext && !TlsUtilities.IsValidUint8(certificateRequestContext.Length))
                throw new ArgumentException("cannot be longer than 255", "certificateRequestContext");
            if (null != certificateTypes
                && (certificateTypes.Length < 1 || !TlsUtilities.IsValidUint8(certificateTypes.Length)))
                throw new ArgumentException("should have length from 1 to 255", "certificateTypes");

            this.m_certificateRequestContext      = TlsUtilities.Clone(certificateRequestContext);
            this.CertificateTypes                 = certificateTypes;
            this.SupportedSignatureAlgorithms     = supportedSignatureAlgorithms;
            this.SupportedSignatureAlgorithmsCert = supportedSignatureAlgorithmsCert;
            this.CertificateAuthorities           = certificateAuthorities;
        }

        public byte[] GetCertificateRequestContext() { return TlsUtilities.Clone(this.m_certificateRequestContext); }

        /// <returns>an array of certificate types</returns>
        /// <seealso cref="ClientCertificateType" />
        public short[] CertificateTypes { get; }

        /// <returns>
        ///     an <see cref="IList" /> of <see cref="SignatureAndHashAlgorithm" /> (or null before TLS 1.2).
        /// </returns>
        public IList SupportedSignatureAlgorithms { get; }

        /// <returns>
        ///     an optional <see cref="IList" /> of <see cref="SignatureAndHashAlgorithm" />. May be non-null from
        ///     TLS 1.3 onwards.
        /// </returns>
        public IList SupportedSignatureAlgorithmsCert { get; }

        /// <returns>an <see cref="IList" /> of <see cref="X509Name" />.</returns>
        public IList CertificateAuthorities { get; }

        public bool HasCertificateRequestContext(byte[] certificateRequestContext) { return Arrays.AreEqual(this.m_certificateRequestContext, certificateRequestContext); }

        /// <summary>Encode this <see cref="CertificateRequest" /> to a <see cref="Stream" />.</summary>
        /// <param name="context">the <see cref="TlsContext" /> of the current connection.</param>
        /// <param name="output">the <see cref="Stream" /> to encode to.</param>
        /// <exception cref="IOException" />
        public void Encode(TlsContext context, Stream output)
        {
            var negotiatedVersion = context.ServerVersion;
            var isTlsV12          = TlsUtilities.IsTlsV12(negotiatedVersion);
            var isTlsV13          = TlsUtilities.IsTlsV13(negotiatedVersion);

            if (isTlsV13 != (null != this.m_certificateRequestContext) ||
                isTlsV13 != (null == this.CertificateTypes) ||
                isTlsV12 != (null != this.SupportedSignatureAlgorithms) ||
                !isTlsV13 && null != this.SupportedSignatureAlgorithmsCert)
                throw new InvalidOperationException();

            if (isTlsV13)
            {
                TlsUtilities.WriteOpaque8(this.m_certificateRequestContext, output);

                var extensions = Platform.CreateHashtable();
                TlsExtensionsUtilities.AddSignatureAlgorithmsExtension(extensions, this.SupportedSignatureAlgorithms);

                if (null != this.SupportedSignatureAlgorithmsCert) TlsExtensionsUtilities.AddSignatureAlgorithmsCertExtension(extensions, this.SupportedSignatureAlgorithmsCert);

                if (null != this.CertificateAuthorities) TlsExtensionsUtilities.AddCertificateAuthoritiesExtension(extensions, this.CertificateAuthorities);

                var extEncoding = TlsProtocol.WriteExtensionsData(extensions);

                TlsUtilities.WriteOpaque16(extEncoding, output);
                return;
            }

            TlsUtilities.WriteUint8ArrayWithUint8Length(this.CertificateTypes, output);

            if (isTlsV12)
                // TODO Check whether SignatureAlgorithm.anonymous is allowed here
                TlsUtilities.EncodeSupportedSignatureAlgorithms(this.SupportedSignatureAlgorithms, output);

            if (this.CertificateAuthorities == null || this.CertificateAuthorities.Count < 1)
            {
                TlsUtilities.WriteUint16(0, output);
            }
            else
            {
                var derEncodings = Platform.CreateArrayList(this.CertificateAuthorities.Count);

                var totalLength = 0;
                foreach (X509Name certificateAuthority in this.CertificateAuthorities)
                {
                    var derEncoding = certificateAuthority.GetEncoded(Asn1Encodable.Der);
                    derEncodings.Add(derEncoding);
                    totalLength += derEncoding.Length + 2;
                }

                TlsUtilities.CheckUint16(totalLength);
                TlsUtilities.WriteUint16(totalLength, output);

                foreach (byte[] derEncoding in derEncodings) TlsUtilities.WriteOpaque16(derEncoding, output);
            }
        }

        /// <summary>Parse a <see cref="CertificateRequest" /> from a <see cref="Stream" /></summary>
        /// <param name="context">the <see cref="TlsContext" /> of the current connection.</param>
        /// <param name="input">the <see cref="Stream" /> to parse from.</param>
        /// <returns>a <see cref="CertificateRequest" /> object.</returns>
        /// <exception cref="IOException" />
        public static CertificateRequest Parse(TlsContext context, Stream input)
        {
            var negotiatedVersion = context.ServerVersion;
            var isTlsV13          = TlsUtilities.IsTlsV13(negotiatedVersion);

            if (isTlsV13)
            {
                var certificateRequestContext = TlsUtilities.ReadOpaque8(input);

                /*
                 * TODO[tls13] required: signature_algorithms; optional: status_request,
                 * signed_certificate_timestamp, certificate_authorities, oid_filters,
                 * signature_algorithms_cert
                 */

                var extEncoding = TlsUtilities.ReadOpaque16(input);

                var extensions = TlsProtocol.ReadExtensionsData13(HandshakeType.certificate_request,
                    extEncoding);

                var supportedSignatureAlgorithms13 = CheckSupportedSignatureAlgorithms(
                    TlsExtensionsUtilities.GetSignatureAlgorithmsExtension(extensions),
                    AlertDescription.missing_extension);
                var supportedSignatureAlgorithmsCert13 = TlsExtensionsUtilities
                    .GetSignatureAlgorithmsCertExtension(extensions);
                var certificateAuthorities13 = TlsExtensionsUtilities.GetCertificateAuthoritiesExtension(extensions);

                return new CertificateRequest(certificateRequestContext, supportedSignatureAlgorithms13,
                    supportedSignatureAlgorithmsCert13, certificateAuthorities13);
            }

            var isTLSv12 = TlsUtilities.IsTlsV12(negotiatedVersion);

            var certificateTypes = TlsUtilities.ReadUint8ArrayWithUint8Length(input, 1);

            IList supportedSignatureAlgorithms         = null;
            if (isTLSv12) supportedSignatureAlgorithms = TlsUtilities.ParseSupportedSignatureAlgorithms(input);

            IList certificateAuthorities = null;
            {
                var certAuthData = TlsUtilities.ReadOpaque16(input);
                if (certAuthData.Length > 0)
                {
                    certificateAuthorities = Platform.CreateArrayList();
                    var bis = new MemoryStream(certAuthData, false);
                    do
                    {
                        var derEncoding = TlsUtilities.ReadOpaque16(bis, 1);
                        var asn1        = TlsUtilities.ReadDerObject(derEncoding);
                        certificateAuthorities.Add(X509Name.GetInstance(asn1));
                    } while (bis.Position < bis.Length);
                }
            }

            return new CertificateRequest(certificateTypes, supportedSignatureAlgorithms, certificateAuthorities);
        }
    }
}
#pragma warning restore
#endif