#if !BESTHTTP_DISABLE_ALTERNATE_SSL && (!UNITY_WEBGL || UNITY_EDITOR)
#pragma warning disable
namespace BestHTTP.SecureProtocol.Org.BouncyCastle.Tls
{
    using System;
    using System.Collections;
    using System.IO;
    using BestHTTP.SecureProtocol.Org.BouncyCastle.Tls.Crypto;
    using BestHTTP.SecureProtocol.Org.BouncyCastle.Utilities;

    /// <summary>Parsing and encoding of a <i>Certificate</i> struct from RFC 4346.</summary>
    /// <remarks>
    ///     <pre>
    ///         opaque ASN.1Cert&lt;2^24-1&gt;;
    ///         struct {
    ///         ASN.1Cert certificate_list&lt;0..2^24-1&gt;;
    ///         } Certificate;
    ///     </pre>
    /// </remarks>
    public sealed class Certificate
    {
        private static readonly TlsCertificate[]   EmptyCerts       = new TlsCertificate[0];
        private static readonly CertificateEntry[] EmptyCertEntries = new CertificateEntry[0];

        public static readonly Certificate EmptyChain      = new(EmptyCerts);
        public static readonly Certificate EmptyChainTls13 = new(TlsUtilities.EmptyBytes, EmptyCertEntries);

        public sealed class ParseOptions
        {
            public int MaxChainLength { get; private set; } = int.MaxValue;

            public ParseOptions SetMaxChainLength(int maxChainLength)
            {
                this.MaxChainLength = maxChainLength;
                return this;
            }
        }

        private static CertificateEntry[] Convert(TlsCertificate[] certificateList)
        {
            if (TlsUtilities.IsNullOrContainsNull(certificateList))
                throw new ArgumentException("cannot be null or contain any nulls", "certificateList");

            var count                                 = certificateList.Length;
            var result                                = new CertificateEntry[count];
            for (var i = 0; i < count; ++i) result[i] = new CertificateEntry(certificateList[i], null);
            return result;
        }

        private readonly byte[]             m_certificateRequestContext;
        private readonly CertificateEntry[] m_certificateEntryList;

        public Certificate(TlsCertificate[] certificateList)
            : this(null, Convert(certificateList))
        {
        }

        // TODO[tls13] Prefer to manage the certificateRequestContext internally only? 
        public Certificate(byte[] certificateRequestContext, CertificateEntry[] certificateEntryList)
        {
            if (null != certificateRequestContext && !TlsUtilities.IsValidUint8(certificateRequestContext.Length))
                throw new ArgumentException("cannot be longer than 255", "certificateRequestContext");
            if (TlsUtilities.IsNullOrContainsNull(certificateEntryList))
                throw new ArgumentException("cannot be null or contain any nulls", "certificateEntryList");

            this.m_certificateRequestContext = TlsUtilities.Clone(certificateRequestContext);
            this.m_certificateEntryList      = certificateEntryList;
        }

        public byte[] GetCertificateRequestContext() { return TlsUtilities.Clone(this.m_certificateRequestContext); }

        /// <returns>an array of <see cref="TlsCertificate" /> representing a certificate chain.</returns>
        public TlsCertificate[] GetCertificateList() { return this.CloneCertificateList(); }

        public TlsCertificate GetCertificateAt(int index) { return this.m_certificateEntryList[index].Certificate; }

        public CertificateEntry GetCertificateEntryAt(int index) { return this.m_certificateEntryList[index]; }

        public CertificateEntry[] GetCertificateEntryList() { return this.CloneCertificateEntryList(); }

        public short CertificateType => Tls.CertificateType.X509;

        public int Length => this.m_certificateEntryList.Length;

        /// <returns>
        ///     <c>true</c> if this certificate chain contains no certificates, or <c>false</c> otherwise.
        /// </returns>
        public bool IsEmpty => this.m_certificateEntryList.Length == 0;

        /// <summary>
        ///     Encode this <see cref="Certificate" /> to a <see cref="Stream" />, and optionally calculate the
        ///     "end point hash" (per RFC 5929's tls-server-end-point binding).
        /// </summary>
        /// <param name="context">the <see cref="TlsContext" /> of the current connection.</param>
        /// <param name="messageOutput">the <see cref="Stream" /> to encode to.</param>
        /// <param name="endPointHashOutput">
        ///     the <see cref="Stream" /> to write the "end point hash" to (or null).
        /// </param>
        /// <exception cref="IOException" />
        public void Encode(TlsContext context, Stream messageOutput, Stream endPointHashOutput)
        {
            var isTlsV13 = TlsUtilities.IsTlsV13(context);

            if (null != this.m_certificateRequestContext != isTlsV13)
                throw new InvalidOperationException();

            if (isTlsV13) TlsUtilities.WriteOpaque8(this.m_certificateRequestContext, messageOutput);

            var count         = this.m_certificateEntryList.Length;
            var certEncodings = Platform.CreateArrayList(count);
            var extEncodings  = isTlsV13 ? Platform.CreateArrayList(count) : null;

            long totalLength = 0;
            for (var i = 0; i < count; ++i)
            {
                var entry       = this.m_certificateEntryList[i];
                var cert        = entry.Certificate;
                var derEncoding = cert.GetEncoded();

                if (i == 0 && endPointHashOutput != null) CalculateEndPointHash(context, cert, derEncoding, endPointHashOutput);

                certEncodings.Add(derEncoding);
                totalLength += derEncoding.Length;
                totalLength += 3;

                if (isTlsV13)
                {
                    var extensions = entry.Extensions;
                    var extEncoding = null == extensions
                        ? TlsUtilities.EmptyBytes
                        : TlsProtocol.WriteExtensionsData(extensions);

                    extEncodings.Add(extEncoding);
                    totalLength += extEncoding.Length;
                    totalLength += 2;
                }
            }

            TlsUtilities.CheckUint24(totalLength);
            TlsUtilities.WriteUint24((int)totalLength, messageOutput);

            for (var i = 0; i < count; ++i)
            {
                var certEncoding = (byte[])certEncodings[i];
                TlsUtilities.WriteOpaque24(certEncoding, messageOutput);

                if (isTlsV13)
                {
                    var extEncoding = (byte[])extEncodings[i];
                    TlsUtilities.WriteOpaque16(extEncoding, messageOutput);
                }
            }
        }

        /// <summary>Parse a <see cref="Certificate" /> from a <see cref="Stream" />.</summary>
        /// <param name="options">the <see cref="ParseOptions" /> to apply during parsing.</param>
        /// <param name="context">the <see cref="TlsContext" /> of the current connection.</param>
        /// <param name="messageInput">the <see cref="Stream" /> to parse from.</param>
        /// <param name="endPointHashOutput">
        ///     the <see cref="Stream" /> to write the "end point hash" to (or null).
        /// </param>
        /// <returns>a <see cref="Certificate" /> object.</returns>
        /// <exception cref="IOException" />
        public static Certificate Parse(ParseOptions options, TlsContext context, Stream messageInput,
            Stream endPointHashOutput)
        {
            var securityParameters = context.SecurityParameters;
            var isTlsV13           = TlsUtilities.IsTlsV13(securityParameters.NegotiatedVersion);

            byte[] certificateRequestContext        = null;
            if (isTlsV13) certificateRequestContext = TlsUtilities.ReadOpaque8(messageInput);

            var totalLength = TlsUtilities.ReadUint24(messageInput);
            if (totalLength == 0)
                return !isTlsV13                           ? EmptyChain
                    : certificateRequestContext.Length < 1 ? EmptyChainTls13
                                                             : new Certificate(certificateRequestContext, EmptyCertEntries);

            var certListData = TlsUtilities.ReadFully(totalLength, messageInput);
            var buf          = new MemoryStream(certListData, false);

            var crypto         = context.Crypto;
            var maxChainLength = Math.Max(1, options.MaxChainLength);

            var certificate_list = Platform.CreateArrayList();
            while (buf.Position < buf.Length)
            {
                if (certificate_list.Count >= maxChainLength)
                    throw new TlsFatalAlert(AlertDescription.internal_error,
                        "Certificate chain longer than maximum (" + maxChainLength + ")");

                var derEncoding = TlsUtilities.ReadOpaque24(buf, 1);
                var cert        = crypto.CreateCertificate(derEncoding);

                if (certificate_list.Count < 1 && endPointHashOutput != null) CalculateEndPointHash(context, cert, derEncoding, endPointHashOutput);

                IDictionary extensions = null;
                if (isTlsV13)
                {
                    var extEncoding = TlsUtilities.ReadOpaque16(buf);

                    extensions = TlsProtocol.ReadExtensionsData13(HandshakeType.certificate, extEncoding);
                }

                certificate_list.Add(new CertificateEntry(cert, extensions));
            }

            var certificateList                                                 = new CertificateEntry[certificate_list.Count];
            for (var i = 0; i < certificate_list.Count; i++) certificateList[i] = (CertificateEntry)certificate_list[i];

            return new Certificate(certificateRequestContext, certificateList);
        }

        private static void CalculateEndPointHash(TlsContext context, TlsCertificate cert, byte[] encoding,
            Stream output)
        {
            var endPointHash = TlsUtilities.CalculateEndPointHash(context, cert, encoding);
            if (endPointHash != null && endPointHash.Length > 0) output.Write(endPointHash, 0, endPointHash.Length);
        }

        private TlsCertificate[] CloneCertificateList()
        {
            var count = this.m_certificateEntryList.Length;
            if (0 == count)
                return EmptyCerts;

            var result                                = new TlsCertificate[count];
            for (var i = 0; i < count; ++i) result[i] = this.m_certificateEntryList[i].Certificate;
            return result;
        }

        private CertificateEntry[] CloneCertificateEntryList()
        {
            var count = this.m_certificateEntryList.Length;
            if (0 == count)
                return EmptyCertEntries;

            var result = new CertificateEntry[count];
            Array.Copy(this.m_certificateEntryList, 0, result, 0, count);
            return result;
        }
    }
}
#pragma warning restore
#endif