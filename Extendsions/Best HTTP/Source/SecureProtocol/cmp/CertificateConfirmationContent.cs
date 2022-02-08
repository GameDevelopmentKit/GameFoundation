#if !BESTHTTP_DISABLE_ALTERNATE_SSL && (!UNITY_WEBGL || UNITY_EDITOR)
#pragma warning disable
namespace BestHTTP.SecureProtocol.Org.BouncyCastle.Cmp
{
    using BestHTTP.SecureProtocol.Org.BouncyCastle.Asn1.Cmp;
    using BestHTTP.SecureProtocol.Org.BouncyCastle.Cms;

    public class CertificateConfirmationContent
    {
        private readonly DefaultDigestAlgorithmIdentifierFinder digestAlgFinder;
        private readonly CertConfirmContent                     content;

        public CertificateConfirmationContent(CertConfirmContent content) { this.content = content; }

        public CertificateConfirmationContent(CertConfirmContent content,
            DefaultDigestAlgorithmIdentifierFinder digestAlgFinder)
        {
            this.content         = content;
            this.digestAlgFinder = digestAlgFinder;
        }

        public CertConfirmContent ToAsn1Structure() { return this.content; }

        public CertificateStatus[] GetStatusMessages()
        {
            var statusArray                              = this.content.ToCertStatusArray();
            var ret                                      = new CertificateStatus[statusArray.Length];
            for (var i = 0; i != ret.Length; i++) ret[i] = new CertificateStatus(this.digestAlgFinder, statusArray[i]);

            return ret;
        }
    }
}
#pragma warning restore
#endif