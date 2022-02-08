#if !BESTHTTP_DISABLE_ALTERNATE_SSL && (!UNITY_WEBGL || UNITY_EDITOR)
#pragma warning disable
namespace BestHTTP.SecureProtocol.Org.BouncyCastle.Crypto.Operators
{
    using System.IO;
    using BestHTTP.SecureProtocol.Org.BouncyCastle.Asn1;
    using BestHTTP.SecureProtocol.Org.BouncyCastle.Asn1.X509;
    using BestHTTP.SecureProtocol.Org.BouncyCastle.Crypto.IO;
    using BestHTTP.SecureProtocol.Org.BouncyCastle.Security;

    public class Asn1DigestFactory : IDigestFactory
    {
        public static Asn1DigestFactory Get(DerObjectIdentifier oid) { return new Asn1DigestFactory(DigestUtilities.GetDigest(oid), oid); }

        public static Asn1DigestFactory Get(string mechanism)
        {
            var oid = DigestUtilities.GetObjectIdentifier(mechanism);
            return new Asn1DigestFactory(DigestUtilities.GetDigest(oid), oid);
        }

        private readonly IDigest             mDigest;
        private readonly DerObjectIdentifier mOid;

        public Asn1DigestFactory(IDigest digest, DerObjectIdentifier oid)
        {
            this.mDigest = digest;
            this.mOid    = oid;
        }

        public virtual object AlgorithmDetails => new AlgorithmIdentifier(this.mOid);

        public virtual int DigestLength => this.mDigest.GetDigestSize();

        public virtual IStreamCalculator CreateCalculator() { return new DfDigestStream(this.mDigest); }
    }

    internal class DfDigestStream : IStreamCalculator
    {
        private readonly DigestSink mStream;

        public DfDigestStream(IDigest digest) { this.mStream = new DigestSink(digest); }

        public Stream Stream => this.mStream;

        public object GetResult()
        {
            var result = new byte[this.mStream.Digest.GetDigestSize()];
            this.mStream.Digest.DoFinal(result, 0);
            return new SimpleBlockResult(result);
        }
    }
}
#pragma warning restore
#endif