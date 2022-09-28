#if !BESTHTTP_DISABLE_ALTERNATE_SSL && (!UNITY_WEBGL || UNITY_EDITOR)
#pragma warning disable
namespace BestHTTP.SecureProtocol.Org.BouncyCastle.Asn1
{
    using System.IO;

    public class Asn1OutputStream
        : DerOutputStream
    {
        public static Asn1OutputStream Create(Stream output) { return new Asn1OutputStream(output); }

        public static Asn1OutputStream Create(Stream output, string encoding)
        {
            if (Asn1Encodable.Der.Equals(encoding))
                return new DerOutputStreamNew(output);
            return new Asn1OutputStream(output);
        }


        public Asn1OutputStream(Stream os)
            : base(os)
        {
        }

        public override void WriteObject(Asn1Encodable encodable)
        {
            if (null == encodable)
                throw new IOException("null object detected");

            this.WritePrimitive(encodable.ToAsn1Object(), true);
            this.FlushInternal();
        }

        public override void WriteObject(Asn1Object primitive)
        {
            if (null == primitive)
                throw new IOException("null object detected");

            this.WritePrimitive(primitive, true);
            this.FlushInternal();
        }

        internal void FlushInternal()
        {
            // Placeholder to support future internal buffering
        }

        internal virtual bool IsBer => true;

        internal void WriteDL(int length)
        {
            if (length < 128)
            {
                this.WriteByte((byte)length);
            }
            else
            {
                var stack = new byte[5];
                var pos   = stack.Length;

                do
                {
                    stack[--pos] =   (byte)length;
                    length       >>= 8;
                } while (length > 0);

                var count = stack.Length - pos;
                stack[--pos] = (byte)(0x80 | count);

                this.Write(stack, pos, count + 1);
            }
        }

        internal virtual void WriteElements(Asn1Encodable[] elements)
        {
            for (int i = 0, count = elements.Length; i < count; ++i) elements[i].ToAsn1Object().Encode(this, true);
        }

        internal void WriteEncodingDL(bool withID, int identifier, byte contents)
        {
            this.WriteIdentifier(withID, identifier);
            this.WriteDL(1);
            this.WriteByte(contents);
        }

        internal void WriteEncodingDL(bool withID, int identifier, byte[] contents)
        {
            this.WriteIdentifier(withID, identifier);
            this.WriteDL(contents.Length);
            this.Write(contents, 0, contents.Length);
        }

        internal void WriteEncodingDL(bool withID, int identifier, byte[] contents, int contentsOff, int contentsLen)
        {
            this.WriteIdentifier(withID, identifier);
            this.WriteDL(contentsLen);
            this.Write(contents, contentsOff, contentsLen);
        }

        internal void WriteEncodingDL(bool withID, int identifier, byte contentsPrefix, byte[] contents,
            int contentsOff, int contentsLen)
        {
            this.WriteIdentifier(withID, identifier);
            this.WriteDL(1 + contentsLen);
            this.WriteByte(contentsPrefix);
            this.Write(contents, contentsOff, contentsLen);
        }

        internal void WriteEncodingDL(bool withID, int identifier, byte[] contents, int contentsOff, int contentsLen,
            byte contentsSuffix)
        {
            this.WriteIdentifier(withID, identifier);
            this.WriteDL(contentsLen + 1);
            this.Write(contents, contentsOff, contentsLen);
            this.WriteByte(contentsSuffix);
        }

        internal void WriteEncodingDL(bool withID, int flags, int tag, byte[] contents)
        {
            this.WriteIdentifier(withID, flags, tag);
            this.WriteDL(contents.Length);
            this.Write(contents, 0, contents.Length);
        }

        internal void WriteEncodingIL(bool withID, int identifier, Asn1Encodable[] elements)
        {
            this.WriteIdentifier(withID, identifier);
            this.WriteByte(0x80);
            this.WriteElements(elements);
            this.WriteByte(0x00);
            this.WriteByte(0x00);
        }

        internal void WriteIdentifier(bool withID, int identifier)
        {
            if (withID) this.WriteByte((byte)identifier);
        }

        internal void WriteIdentifier(bool withID, int flags, int tag)
        {
            if (!withID)
            {
                // Don't write the identifier
            }
            else if (tag < 31)
            {
                this.WriteByte((byte)(flags | tag));
            }
            else
            {
                var stack = new byte[6];
                var pos   = stack.Length;

                stack[--pos] = (byte)(tag & 0x7F);
                while (tag > 127)
                {
                    tag          >>= 7;
                    stack[--pos] =   (byte)((tag & 0x7F) | 0x80);
                }

                stack[--pos] = (byte)(flags | 0x1F);

                this.Write(stack, pos, stack.Length - pos);
            }
        }

        internal virtual void WritePrimitive(Asn1Object primitive, bool withID) { primitive.Encode(this, withID); }

        internal virtual void WritePrimitives(Asn1Object[] primitives)
        {
            for (int i = 0, count = primitives.Length; i < count; ++i) this.WritePrimitive(primitives[i], true);
        }

        internal static int GetLengthOfDL(int dl)
        {
            if (dl < 128)
                return 1;

            var length = 2;
            while ((dl >>= 8) > 0) ++length;
            return length;
        }

        internal static int GetLengthOfEncodingDL(bool withID, int contentsLength) { return (withID ? 1 : 0) + GetLengthOfDL(contentsLength) + contentsLength; }

        internal static int GetLengthOfEncodingDL(bool withID, int tag, int contentsLength) { return (withID ? GetLengthOfIdentifier(tag) : 0) + GetLengthOfDL(contentsLength) + contentsLength; }

        internal static int GetLengthOfIdentifier(int tag)
        {
            if (tag < 31)
                return 1;

            var length = 2;
            while ((tag >>= 7) > 0) ++length;
            return length;
        }
    }
}
#pragma warning restore
#endif
