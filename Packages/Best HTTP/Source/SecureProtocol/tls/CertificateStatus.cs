#if !BESTHTTP_DISABLE_ALTERNATE_SSL && (!UNITY_WEBGL || UNITY_EDITOR)
#pragma warning disable
namespace BestHTTP.SecureProtocol.Org.BouncyCastle.Tls
{
    using System;
    using System.Collections;
    using System.IO;
    using BestHTTP.SecureProtocol.Org.BouncyCastle.Asn1;
    using BestHTTP.SecureProtocol.Org.BouncyCastle.Asn1.Ocsp;
    using BestHTTP.SecureProtocol.Org.BouncyCastle.Utilities;

    public sealed class CertificateStatus
    {
        public CertificateStatus(short statusType, object response)
        {
            if (!IsCorrectType(statusType, response))
                throw new ArgumentException("not an instance of the correct type", "response");

            this.StatusType = statusType;
            this.Response   = response;
        }

        public short StatusType { get; }

        public object Response { get; }

        public OcspResponse OcspResponse
        {
            get
            {
                if (!IsCorrectType(CertificateStatusType.ocsp, this.Response))
                    throw new InvalidOperationException("'response' is not an OCSPResponse");

                return (OcspResponse)this.Response;
            }
        }

        /// <summary>an <see cref="IList" /> of (possibly null) <see cref="Asn1.Ocsp.OcspResponse" />.</summary>
        public IList OcspResponseList
        {
            get
            {
                if (!IsCorrectType(CertificateStatusType.ocsp_multi, this.Response))
                    throw new InvalidOperationException("'response' is not an OCSPResponseList");

                return (IList)this.Response;
            }
        }

        /// <summary>Encode this <see cref="CertificateStatus" /> to a <see cref="Stream" />.</summary>
        /// <param name="output">the <see cref="Stream" /> to encode to.</param>
        /// <exception cref="IOException" />
        public void Encode(Stream output)
        {
            TlsUtilities.WriteUint8(this.StatusType, output);

            switch (this.StatusType)
            {
                case CertificateStatusType.ocsp:
                {
                    var ocspResponse = (OcspResponse)this.Response;
                    var derEncoding  = ocspResponse.GetEncoded(Asn1Encodable.Der);
                    TlsUtilities.WriteOpaque24(derEncoding, output);
                    break;
                }
                case CertificateStatusType.ocsp_multi:
                {
                    var ocspResponseList = (IList)this.Response;
                    var count            = ocspResponseList.Count;

                    var  derEncodings = Platform.CreateArrayList(count);
                    long totalLength  = 0;
                    foreach (OcspResponse ocspResponse in ocspResponseList)
                    {
                        if (ocspResponse == null)
                        {
                            derEncodings.Add(TlsUtilities.EmptyBytes);
                        }
                        else
                        {
                            var derEncoding = ocspResponse.GetEncoded(Asn1Encodable.Der);
                            derEncodings.Add(derEncoding);
                            totalLength += derEncoding.Length;
                        }

                        totalLength += 3;
                    }

                    TlsUtilities.CheckUint24(totalLength);
                    TlsUtilities.WriteUint24((int)totalLength, output);

                    foreach (byte[] derEncoding in derEncodings) TlsUtilities.WriteOpaque24(derEncoding, output);

                    break;
                }
                default:
                    throw new TlsFatalAlert(AlertDescription.internal_error);
            }
        }

        /// <summary>Parse a <see cref="CertificateStatus" /> from a <see cref="Stream" />.</summary>
        /// <param name="context">the <see cref="TlsContext" /> of the current connection.</param>
        /// <param name="input">the <see cref="Stream" /> to parse from.</param>
        /// <returns>a <see cref="CertificateStatus" /> object.</returns>
        /// <exception cref="IOException" />
        public static CertificateStatus Parse(TlsContext context, Stream input)
        {
            var securityParameters = context.SecurityParameters;

            var peerCertificate = securityParameters.PeerCertificate;
            if (null == peerCertificate || peerCertificate.IsEmpty
                                        || CertificateType.X509 != peerCertificate.CertificateType)
                throw new TlsFatalAlert(AlertDescription.internal_error);

            var certificateCount     = peerCertificate.Length;
            var statusRequestVersion = securityParameters.StatusRequestVersion;

            var    status_type = TlsUtilities.ReadUint8(input);
            object response;

            switch (status_type)
            {
                case CertificateStatusType.ocsp:
                {
                    RequireStatusRequestVersion(1, statusRequestVersion);

                    var derEncoding = TlsUtilities.ReadOpaque24(input, 1);
                    var derObject   = TlsUtilities.ReadDerObject(derEncoding);
                    response = OcspResponse.GetInstance(derObject);
                    break;
                }
                case CertificateStatusType.ocsp_multi:
                {
                    RequireStatusRequestVersion(2, statusRequestVersion);

                    var ocsp_response_list = TlsUtilities.ReadOpaque24(input, 1);
                    var buf                = new MemoryStream(ocsp_response_list, false);

                    var ocspResponseList = Platform.CreateArrayList();
                    while (buf.Position < buf.Length)
                    {
                        if (ocspResponseList.Count >= certificateCount)
                            throw new TlsFatalAlert(AlertDescription.illegal_parameter);

                        var length = TlsUtilities.ReadUint24(buf);
                        if (length < 1)
                        {
                            ocspResponseList.Add(null);
                        }
                        else
                        {
                            var derEncoding  = TlsUtilities.ReadFully(length, buf);
                            var derObject    = TlsUtilities.ReadDerObject(derEncoding);
                            var ocspResponse = OcspResponse.GetInstance(derObject);
                            ocspResponseList.Add(ocspResponse);
                        }
                    }

                    // Match IList capacity to actual size
                    response = Platform.CreateArrayList(ocspResponseList);
                    break;
                }
                default:
                    throw new TlsFatalAlert(AlertDescription.decode_error);
            }

            return new CertificateStatus(status_type, response);
        }

        private static bool IsCorrectType(short statusType, object response)
        {
            switch (statusType)
            {
                case CertificateStatusType.ocsp:
                    return response is OcspResponse;
                case CertificateStatusType.ocsp_multi:
                    return IsOcspResponseList(response);
                default:
                    throw new ArgumentException("unsupported CertificateStatusType", "statusType");
            }
        }

        private static bool IsOcspResponseList(object response)
        {
            if (!(response is IList))
                return false;

            var v     = (IList)response;
            var count = v.Count;
            if (count < 1)
                return false;

            foreach (var e in v)
                if (null != e && !(e is OcspResponse))
                    return false;
            return true;
        }

        /// <exception cref="IOException" />
        private static void RequireStatusRequestVersion(int minVersion, int statusRequestVersion)
        {
            if (statusRequestVersion < minVersion)
                throw new TlsFatalAlert(AlertDescription.decode_error);
        }
    }
}
#pragma warning restore
#endif