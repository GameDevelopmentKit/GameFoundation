#if !BESTHTTP_DISABLE_ALTERNATE_SSL && (!UNITY_WEBGL || UNITY_EDITOR)
#pragma warning disable
namespace BestHTTP.SecureProtocol.Org.BouncyCastle.Tls
{
    using System;
    using System.Collections;
    using BestHTTP.SecureProtocol.Org.BouncyCastle.Tls.Crypto;

    public sealed class CertificateEntry
    {
        public CertificateEntry(TlsCertificate certificate, IDictionary extensions)
        {
            if (null == certificate)
                throw new ArgumentNullException("certificate");

            this.Certificate = certificate;
            this.Extensions  = extensions;
        }

        public TlsCertificate Certificate { get; }

        public IDictionary Extensions { get; }
    }
}
#pragma warning restore
#endif