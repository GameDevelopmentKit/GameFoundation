#if !BESTHTTP_DISABLE_ALTERNATE_SSL && (!UNITY_WEBGL || UNITY_EDITOR)
#pragma warning disable
namespace BestHTTP.SecureProtocol.Org.BouncyCastle.Asn1.Nist
{
    using System;
    using BestHTTP.SecureProtocol.Org.BouncyCastle.Utilities;

    /// <summary>
    ///     KMACwithSHAKE128-params ::= SEQUENCE {
    ///     kMACOutputLength     INTEGER DEFAULT 256, -- Output length in bits
    ///     customizationString  OCTET STRING DEFAULT ''H
    ///     }
    /// </summary>
    public class KMacWithShake128Params : Asn1Encodable
    {
        private static readonly byte[] EMPTY_STRING = new byte[0];
        private static readonly int    DEF_LENGTH   = 256;

        private readonly byte[] customizationString;

        public KMacWithShake128Params(int outputLength)
        {
            this.OutputLength        = outputLength;
            this.customizationString = EMPTY_STRING;
        }

        public KMacWithShake128Params(int outputLength, byte[] customizationString)
        {
            this.OutputLength        = outputLength;
            this.customizationString = Arrays.Clone(customizationString);
        }

        public static KMacWithShake128Params GetInstance(object o)
        {
            if (o is KMacWithShake128Params)
                return (KMacWithShake128Params)o;
            if (o != null) return new KMacWithShake128Params(Asn1Sequence.GetInstance(o));

            return null;
        }

        private KMacWithShake128Params(Asn1Sequence seq)
        {
            if (seq.Count > 2)
                throw new InvalidOperationException("sequence size greater than 2");

            if (seq.Count == 2)
            {
                this.OutputLength        = DerInteger.GetInstance(seq[0]).IntValueExact;
                this.customizationString = Arrays.Clone(Asn1OctetString.GetInstance(seq[1]).GetOctets());
            }
            else if (seq.Count == 1)
            {
                if (seq[0] is DerInteger)
                {
                    this.OutputLength        = DerInteger.GetInstance(seq[0]).IntValueExact;
                    this.customizationString = EMPTY_STRING;
                }
                else
                {
                    this.OutputLength        = DEF_LENGTH;
                    this.customizationString = Arrays.Clone(Asn1OctetString.GetInstance(seq[0]).GetOctets());
                }
            }
            else
            {
                this.OutputLength        = DEF_LENGTH;
                this.customizationString = EMPTY_STRING;
            }
        }

        public int OutputLength { get; }

        public byte[] CustomizationString => Arrays.Clone(this.customizationString);

        public override Asn1Object ToAsn1Object()
        {
            var v = new Asn1EncodableVector();
            if (this.OutputLength != DEF_LENGTH) v.Add(new DerInteger(this.OutputLength));

            if (this.customizationString.Length != 0) v.Add(new DerOctetString(this.CustomizationString));

            return new DerSequence(v);
        }
    }
}
#pragma warning restore
#endif