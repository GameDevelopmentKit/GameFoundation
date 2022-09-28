#if !BESTHTTP_DISABLE_ALTERNATE_SSL && (!UNITY_WEBGL || UNITY_EDITOR)
#pragma warning disable
namespace BestHTTP.SecureProtocol.Org.BouncyCastle.Asn1
{
    using System;
    using System.Collections;

    internal class LazyDerSequence
        : DerSequence
    {
        private byte[] encoded;

        internal LazyDerSequence(byte[] encoded)
        {
            if (null == encoded)
                throw new ArgumentNullException("encoded");

            this.encoded = encoded;
        }

        private void Parse()
        {
            lock (this)
            {
                if (null != encoded)
                {
                    Asn1InputStream e = new LazyAsn1InputStream(encoded);
                    var             v = e.ReadVector();

                    this.elements = v.TakeElements();
                    this.encoded = null;
                }
            }
        }

        public override Asn1Encodable this[int index]
        {
            get
            {
                this.Parse();

                return base[index];
            }
        }

        public override IEnumerator GetEnumerator()
        {
            this.Parse();

            return base.GetEnumerator();
        }

        public override int Count
        {
            get
            {
                this.Parse();

                return base.Count;
            }
        }

        internal override int EncodedLength(bool withID)
        {
            lock (this)
            {
                if (this.encoded == null)
                    return base.EncodedLength(withID);
                return Asn1OutputStream.GetLengthOfEncodingDL(withID, this.encoded.Length);
            }
        }

        internal override void Encode(Asn1OutputStream asn1Out, bool withID)
        {
            lock (this)
            {
                if (this.encoded == null)
                    base.Encode(asn1Out, withID);
                else
                    asn1Out.WriteEncodingDL(withID, Asn1Tags.Constructed | Asn1Tags.Sequence, this.encoded);
            }
        }
    }
}
#pragma warning restore
#endif
