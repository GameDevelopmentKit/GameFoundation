#if !BESTHTTP_DISABLE_ALTERNATE_SSL && (!UNITY_WEBGL || UNITY_EDITOR)
#pragma warning disable
namespace BestHTTP.SecureProtocol.Org.BouncyCastle.Asn1
{
    using System.IO;

    public class BerOutputStream
        : DerOutputStream
    {
        public BerOutputStream(Stream os)
            : base(os)
        {
        }

        public override void WriteObject(Asn1Encodable encodable)
        {
            Asn1OutputStream.Create(this.s).WriteObject(encodable);
        }

        public override void WriteObject(Asn1Object primitive)
        {
            Asn1OutputStream.Create(this.s).WriteObject(primitive);
        }
    }
}
#pragma warning restore
#endif
