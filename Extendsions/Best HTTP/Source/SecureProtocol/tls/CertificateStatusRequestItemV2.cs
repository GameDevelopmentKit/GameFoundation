#if !BESTHTTP_DISABLE_ALTERNATE_SSL && (!UNITY_WEBGL || UNITY_EDITOR)
#pragma warning disable
namespace BestHTTP.SecureProtocol.Org.BouncyCastle.Tls
{
    using System;
    using System.IO;

    /// <summary>Implementation of the RFC 6961 2.2. CertificateStatusRequestItemV2.</summary>
    public sealed class CertificateStatusRequestItemV2
    {
        public CertificateStatusRequestItemV2(short statusType, object request)
        {
            if (!IsCorrectType(statusType, request))
                throw new ArgumentException("not an instance of the correct type", "request");

            this.StatusType = statusType;
            this.Request    = request;
        }

        public short StatusType { get; }

        public object Request { get; }

        public OcspStatusRequest OcspStatusRequest
        {
            get
            {
                if (!(this.Request is OcspStatusRequest))
                    throw new InvalidOperationException("'request' is not an OcspStatusRequest");

                return (OcspStatusRequest)this.Request;
            }
        }

        /// <summary>Encode this <see cref="CertificateStatusRequestItemV2" /> to a <see cref="Stream" />.</summary>
        /// <param name="output">the <see cref="Stream" /> to encode to.</param>
        /// <exception cref="IOException" />
        public void Encode(Stream output)
        {
            TlsUtilities.WriteUint8(this.StatusType, output);

            var buf = new MemoryStream();
            switch (this.StatusType)
            {
                case CertificateStatusType.ocsp:
                case CertificateStatusType.ocsp_multi:
                    ((OcspStatusRequest)this.Request).Encode(buf);
                    break;
                default:
                    throw new TlsFatalAlert(AlertDescription.internal_error);
            }

            var requestBytes = buf.ToArray();
            TlsUtilities.WriteOpaque16(requestBytes, output);
        }

        /// <summary>Parse a <see cref="CertificateStatusRequestItemV2" /> from a <see cref="Stream" />.</summary>
        /// <param name="input">the <see cref="Stream" /> to parse from.</param>
        /// <returns>a <see cref="CertificateStatusRequestItemV2" /> object.</returns>
        /// <exception cref="IOException" />
        public static CertificateStatusRequestItemV2 Parse(Stream input)
        {
            var status_type = TlsUtilities.ReadUint8(input);

            object request;
            var    requestBytes = TlsUtilities.ReadOpaque16(input);
            var    buf          = new MemoryStream(requestBytes, false);
            switch (status_type)
            {
                case CertificateStatusType.ocsp:
                case CertificateStatusType.ocsp_multi:
                    request = OcspStatusRequest.Parse(buf);
                    break;
                default:
                    throw new TlsFatalAlert(AlertDescription.decode_error);
            }

            TlsProtocol.AssertEmpty(buf);

            return new CertificateStatusRequestItemV2(status_type, request);
        }

        private static bool IsCorrectType(short statusType, object request)
        {
            switch (statusType)
            {
                case CertificateStatusType.ocsp:
                case CertificateStatusType.ocsp_multi:
                    return request is OcspStatusRequest;
                default:
                    throw new ArgumentException("unsupported CertificateStatusType", "statusType");
            }
        }
    }
}
#pragma warning restore
#endif