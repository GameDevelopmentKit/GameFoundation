#if !BESTHTTP_DISABLE_ALTERNATE_SSL && (!UNITY_WEBGL || UNITY_EDITOR)
#pragma warning disable
namespace BestHTTP.SecureProtocol.Org.BouncyCastle.Cmp
{
    using System.Collections;
    using BestHTTP.SecureProtocol.Org.BouncyCastle.Asn1;
    using BestHTTP.SecureProtocol.Org.BouncyCastle.Asn1.Cmp;
    using BestHTTP.SecureProtocol.Org.BouncyCastle.Cms;
    using BestHTTP.SecureProtocol.Org.BouncyCastle.Math;
    using BestHTTP.SecureProtocol.Org.BouncyCastle.Security;
    using BestHTTP.SecureProtocol.Org.BouncyCastle.Utilities;
    using BestHTTP.SecureProtocol.Org.BouncyCastle.X509;

    public class CertificateConfirmationContentBuilder
    {
        private static readonly DefaultSignatureAlgorithmIdentifierFinder sigAlgFinder = new();

        private readonly DefaultDigestAlgorithmIdentifierFinder digestAlgFinder;
        private readonly IList                                  acceptedCerts  = Platform.CreateArrayList();
        private readonly IList                                  acceptedReqIds = Platform.CreateArrayList();

        public CertificateConfirmationContentBuilder()
            : this(new DefaultDigestAlgorithmIdentifierFinder())
        {
        }

        public CertificateConfirmationContentBuilder(DefaultDigestAlgorithmIdentifierFinder digestAlgFinder) { this.digestAlgFinder = digestAlgFinder; }

        public CertificateConfirmationContentBuilder AddAcceptedCertificate(X509Certificate certHolder,
            BigInteger certReqId)
        {
            this.acceptedCerts.Add(certHolder);
            this.acceptedReqIds.Add(certReqId);
            return this;
        }

        public CertificateConfirmationContent Build()
        {
            var v = new Asn1EncodableVector();
            for (var i = 0; i != this.acceptedCerts.Count; i++)
            {
                var cert  = (X509Certificate)this.acceptedCerts[i];
                var reqId = (BigInteger)this.acceptedReqIds[i];


                var algorithmIdentifier = sigAlgFinder.Find(cert.SigAlgName);

                var digAlg = this.digestAlgFinder.find(algorithmIdentifier);
                if (null == digAlg)
                    throw new CmpException("cannot find algorithm for digest from signature");

                var digest = DigestUtilities.CalculateDigest(digAlg.Algorithm, cert.GetEncoded());

                v.Add(new CertStatus(digest, reqId));
            }

            return new CertificateConfirmationContent(CertConfirmContent.GetInstance(new DerSequence(v)), this.digestAlgFinder);
        }
    }
}
#pragma warning restore
#endif