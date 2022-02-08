#if !BESTHTTP_DISABLE_ALTERNATE_SSL && (!UNITY_WEBGL || UNITY_EDITOR)
#pragma warning disable
namespace BestHTTP.SecureProtocol.Org.BouncyCastle.Cmp
{
    using BestHTTP.SecureProtocol.Org.BouncyCastle.Asn1.Cmp;
    using BestHTTP.SecureProtocol.Org.BouncyCastle.Asn1.X509;
    using BestHTTP.SecureProtocol.Org.BouncyCastle.Math;

    public class RevocationDetails
    {
        private readonly RevDetails revDetails;

        public RevocationDetails(RevDetails revDetails) { this.revDetails = revDetails; }

        public X509Name Subject => this.revDetails.CertDetails.Subject;

        public X509Name Issuer => this.revDetails.CertDetails.Issuer;

        public BigInteger SerialNumber => this.revDetails.CertDetails.SerialNumber.Value;

        public RevDetails ToASN1Structure() { return this.revDetails; }
    }
}
#pragma warning restore
#endif