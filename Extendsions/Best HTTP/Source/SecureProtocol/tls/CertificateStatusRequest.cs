#if !BESTHTTP_DISABLE_ALTERNATE_SSL && (!UNITY_WEBGL || UNITY_EDITOR)
#pragma warning disable
namespace BestHTTP.SecureProtocol.Org.BouncyCastle.Tls
{
    using System;
    using System.IO;

    /// <summary>Implementation of the RFC 3546 3.6. CertificateStatusRequest.</summary>
    public sealed class CertificateStatusRequest
    {
        public CertificateStatusRequest(short statusType, object request)
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
                if (!IsCorrectType(CertificateStatusType.ocsp, this.Request))
                    throw new InvalidOperationException("'request' is not an OCSPStatusRequest");

                return (OcspStatusRequest)this.Request;
            }
        }

        /// <summary>Encode this <see cref="CertificateStatusRequest" /> to a <see cref="Stream" />.</summary>
        /// <param name="output">the <see cref="Stream" /> to encode to.</param>
        /// <exception cref="IOException" />
        public void Encode(Stream output)
        {
            TlsUtilities.WriteUint8(this.StatusType, output);

            switch (this.StatusType)
            {
                case CertificateStatusType.ocsp:
                    ((OcspStatusRequest)this.Request).Encode(output);
                    break;
                default:
                    throw new TlsFatalAlert(AlertDescription.internal_error);
            }
        }

        /// <summary>Parse a <see cref="CertificateStatusRequest" /> from a <see cref="Stream" />.</summary>
        /// <param name="input">the <see cref="Stream" /> to parse from.</param>
        /// <returns>a <see cref="CertificateStatusRequest" /> object.</returns>
        /// <exception cref="IOException" />
        public static CertificateStatusRequest Parse(Stream input)
        {
            var    status_type = TlsUtilities.ReadUint8(input);
            object request;

            switch (status_type)
            {
                case CertificateStatusType.ocsp:
                    request = OcspStatusRequest.Parse(input);
                    break;
                default:
                    throw new TlsFatalAlert(AlertDescription.decode_error);
            }

            return new CertificateStatusRequest(status_type, request);
        }

        private static bool IsCorrectType(short statusType, object request)
        {
            switch (statusType)
            {
                case CertificateStatusType.ocsp:
                    return request is OcspStatusRequest;
                default:
                    throw new ArgumentException("unsupported CertificateStatusType", "statusType");
            }
        }
    }
}
#pragma warning restore
#endif