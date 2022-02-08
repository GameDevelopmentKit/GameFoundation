#if !BESTHTTP_DISABLE_ALTERNATE_SSL && (!UNITY_WEBGL || UNITY_EDITOR)
#pragma warning disable
namespace BestHTTP.SecureProtocol.Org.BouncyCastle.Cmp
{
    using BestHTTP.SecureProtocol.Org.BouncyCastle.Asn1;
    using BestHTTP.SecureProtocol.Org.BouncyCastle.Asn1.Cmp;
    using BestHTTP.SecureProtocol.Org.BouncyCastle.Asn1.Crmf;
    using BestHTTP.SecureProtocol.Org.BouncyCastle.Asn1.X509;
    using BestHTTP.SecureProtocol.Org.BouncyCastle.Math;

    public class RevocationDetailsBuilder
    {
        private readonly CertTemplateBuilder _templateBuilder = new();

        public RevocationDetailsBuilder SetPublicKey(SubjectPublicKeyInfo publicKey)
        {
            if (publicKey != null) this._templateBuilder.SetPublicKey(publicKey);

            return this;
        }

        public RevocationDetailsBuilder SetIssuer(X509Name issuer)
        {
            if (issuer != null) this._templateBuilder.SetIssuer(issuer);

            return this;
        }

        public RevocationDetailsBuilder SetSerialNumber(BigInteger serialNumber)
        {
            if (serialNumber != null) this._templateBuilder.SetSerialNumber(new DerInteger(serialNumber));

            return this;
        }

        public RevocationDetailsBuilder SetSubject(X509Name subject)
        {
            if (subject != null) this._templateBuilder.SetSubject(subject);

            return this;
        }

        public RevocationDetails Build() { return new RevocationDetails(new RevDetails(this._templateBuilder.Build())); }
    }
}
#pragma warning restore
#endif