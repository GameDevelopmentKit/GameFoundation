#if !BESTHTTP_DISABLE_ALTERNATE_SSL && (!UNITY_WEBGL || UNITY_EDITOR)
#pragma warning disable
namespace BestHTTP.SecureProtocol.Org.BouncyCastle.Tls
{
    internal sealed class TlsServerCertificateImpl
        : TlsServerCertificate
    {
        internal TlsServerCertificateImpl(Certificate certificate, CertificateStatus certificateStatus)
        {
            this.Certificate       = certificate;
            this.CertificateStatus = certificateStatus;
        }

        public Certificate Certificate { get; }

        public CertificateStatus CertificateStatus { get; }
    }
}
#pragma warning restore
#endif