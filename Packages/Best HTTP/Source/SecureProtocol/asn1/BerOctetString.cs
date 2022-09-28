#if !BESTHTTP_DISABLE_ALTERNATE_SSL && (!UNITY_WEBGL || UNITY_EDITOR)
#pragma warning disable
namespace BestHTTP.SecureProtocol.Org.BouncyCastle.Asn1
{
    using System;
    using System.Collections;
    using System.Diagnostics;
    using BestHTTP.SecureProtocol.Org.BouncyCastle.Utilities;

    public class BerOctetString
        : DerOctetString, IEnumerable
    {
        private const int DefaultSegmentLimit = 1000;

        public static BerOctetString FromSequence(Asn1Sequence seq)
        {
            int count = seq.Count;
            Asn1OctetString[] v = new Asn1OctetString[count];
            for (int i = 0; i < count; ++i)
            {
                v[i] = GetInstance(seq[i]);
            }
            return new BerOctetString(v);
        }

        internal static byte[] FlattenOctetStrings(Asn1OctetString[] octetStrings)
        {
            var count = octetStrings.Length;
            switch (count)
            {
                case 0:
                    return EmptyOctets;
                case 1:
                    return octetStrings[0].str;
                default:
                {
                    var totalOctets                             = 0;
                    for (var i = 0; i < count; ++i) totalOctets += octetStrings[i].str.Length;

                    var str = new byte[totalOctets];
                    var pos = 0;
                    for (var i = 0; i < count; ++i)
                    {
                        var octets = octetStrings[i].str;
                        Array.Copy(octets, 0, str, pos, octets.Length);
                        pos += octets.Length;
                    }

                    Debug.Assert(pos == totalOctets);
                    return str;
                }
            }
        }

        private static Asn1OctetString[] ToOctetStringArray(IEnumerable e)
        {
            IList list = Platform.CreateArrayList(e);

            int count = list.Count;
            Asn1OctetString[] v = new Asn1OctetString[count];
            for (int i = 0; i < count; ++i)
            {
                v[i] = GetInstance(list[i]);
            }
            return v;
        }

        private readonly int               segmentLimit;
        private readonly Asn1OctetString[] elements;


        public BerOctetString(IEnumerable e)
            : this(ToOctetStringArray(e))
        {
        }

        public BerOctetString(byte[] str)
            : this(str, DefaultSegmentLimit)
		{
		}

        public BerOctetString(Asn1OctetString[] elements)
            : this(elements, DefaultSegmentLimit)
        {
        }

        public BerOctetString(byte[] str, int segmentLimit)
            : this(str, null, segmentLimit)
        {
        }

        public BerOctetString(Asn1OctetString[] elements, int segmentLimit)
            : this(FlattenOctetStrings(elements), elements, segmentLimit)
        {
        }

        private BerOctetString(byte[] octets, Asn1OctetString[] elements, int segmentLimit)
            : base(octets)
        {
            this.elements     = elements;
            this.segmentLimit = segmentLimit;
        }

        /**
         * return the DER octets that make up this string.
         */
		public IEnumerator GetEnumerator()
		{
            if (this.elements == null)
                return new ChunkEnumerator(this.str, this.segmentLimit);

            return this.elements.GetEnumerator();
		}


        public IEnumerator GetObjects()
        {
			return GetEnumerator();
		}

        private bool IsConstructed => null != this.elements || this.str.Length > this.segmentLimit;

        internal override int EncodedLength(bool withID)
        {
            throw Platform.CreateNotImplementedException("BerOctetString.EncodedLength");

            // TODO This depends on knowing it's not DER
            //if (!IsConstructed)
            //    return EncodedLength(withID, str.Length);

            //int totalLength = withID ? 4 : 3;

            //if (null != elements)
            //{
            //    for (int i = 0; i < elements.Length; ++i)
            //    {
            //        totalLength += elements[i].EncodedLength(true);
            //    }
            //}
            //else
            //{
            //    int fullSegments = str.Length / segmentLimit;
            //    totalLength += fullSegments * EncodedLength(true, segmentLimit);

            //    int lastSegmentLength = str.Length - (fullSegments * segmentLimit);
            //    if (lastSegmentLength > 0)
            //    {
            //        totalLength += EncodedLength(true, lastSegmentLength);
            //    }
            //}

            //return totalLength;
        }

        internal override void Encode(Asn1OutputStream asn1Out, bool withID)
        {
            if (!asn1Out.IsBer || !this.IsConstructed)
            {
                base.Encode(asn1Out, withID);
                return;
            }

            asn1Out.WriteIdentifier(withID, Asn1Tags.Constructed | Asn1Tags.OctetString);
            asn1Out.WriteByte(0x80);

            if (null != this.elements)
            {
                asn1Out.WritePrimitives(this.elements);
            }
            else
            {
                var pos = 0;
                while (pos < this.str.Length)
                {
                    var segmentLength = Math.Min(this.str.Length - pos, this.segmentLimit);
                    Encode(asn1Out, true, this.str, pos, segmentLength);
                    pos += segmentLength;
                }
            }

            asn1Out.WriteByte(0x00);
            asn1Out.WriteByte(0x00);
        }

        private class ChunkEnumerator
            : IEnumerator
        {
            private readonly byte[] octets;
            private readonly int    segmentLimit;

            private DerOctetString currentSegment;
            private int            nextSegmentPos;

            internal ChunkEnumerator(byte[] octets, int segmentLimit)
            {
                this.octets       = octets;
                this.segmentLimit = segmentLimit;
            }

            public object Current
            {
                get
                {
                    if (null == this.currentSegment)
                        throw new InvalidOperationException();

                    return this.currentSegment;
                }
            }

            public bool MoveNext()
            {
                if (this.nextSegmentPos >= this.octets.Length)
                {
                    this.currentSegment = null;
                    return false;
                }

                var length  = Math.Min(this.octets.Length - this.nextSegmentPos, this.segmentLimit);
                var segment = new byte[length];
                Array.Copy(this.octets, this.nextSegmentPos, segment, 0, length);
                this.currentSegment =  new DerOctetString(segment);
                this.nextSegmentPos += length;
                return true;
            }

            public void Reset()
            {
                this.currentSegment = null;
                this.nextSegmentPos = 0;
            }
        }
    }
}
#pragma warning restore
#endif
