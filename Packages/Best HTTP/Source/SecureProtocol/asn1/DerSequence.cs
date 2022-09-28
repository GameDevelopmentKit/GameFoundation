#if !BESTHTTP_DISABLE_ALTERNATE_SSL && (!UNITY_WEBGL || UNITY_EDITOR)
#pragma warning disable
namespace BestHTTP.SecureProtocol.Org.BouncyCastle.Asn1
{
	using System.IO;
	using BestHTTP.SecureProtocol.Org.BouncyCastle.Utilities;

	public class DerSequence
		: Asn1Sequence
	{
		public static readonly DerSequence Empty = new DerSequence();

		public static DerSequence FromVector(Asn1EncodableVector elementVector)
		{
            return elementVector.Count < 1 ? Empty : new DerSequence(elementVector);
		}

		/**
		 * create an empty sequence
		 */
		public DerSequence()
			: base()
		{
		}

		/**
		 * create a sequence containing one object
		 */
		public DerSequence(Asn1Encodable element)
			: base(element)
		{
		}

		public DerSequence(params Asn1Encodable[] elements)
            : base(elements)
		{
		}

		/**
		 * create a sequence containing a vector of objects.
		 */
		public DerSequence(Asn1EncodableVector elementVector)
            : base(elementVector)
		{
		}

		internal override int EncodedLength(bool withID) { throw Platform.CreateNotImplementedException("DerSequence.EncodedLength"); }

		/*
		 * A note on the implementation:
		 * <p>
		 * As Der requires the constructed, definite-length model to
		 * be used for structured types, this varies slightly from the
		 * ASN.1 descriptions given. Rather than just outputing Sequence,
		 * we also have to specify Constructed, and the objects length.
		 */
		internal override void Encode(Asn1OutputStream asn1Out, bool withID)
		{
			if (this.Count < 1)
			{
				asn1Out.WriteEncodingDL(withID, Asn1Tags.Constructed | Asn1Tags.Sequence, Asn1OctetString.EmptyOctets);
				return;
			}

			// TODO Intermediate buffer could be avoided if we could calculate expected length
			var bOut = new MemoryStream();
			var dOut = Asn1OutputStream.Create(bOut, Der);
			dOut.WriteElements(this.elements);
			dOut.Flush();

#if PORTABLE || NETFX_CORE
            byte[] bytes = bOut.ToArray();
            int length = bytes.Length;
#else
			var bytes  = bOut.GetBuffer();
			var length = (int)bOut.Position;
#endif

			asn1Out.WriteEncodingDL(withID, Asn1Tags.Constructed | Asn1Tags.Sequence, bytes, 0, length);

			Platform.Dispose(dOut);
		}
	}
}
#pragma warning restore
#endif
