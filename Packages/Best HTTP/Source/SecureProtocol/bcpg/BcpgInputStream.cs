#if !BESTHTTP_DISABLE_ALTERNATE_SSL && (!UNITY_WEBGL || UNITY_EDITOR)
#pragma warning disable
namespace BestHTTP.SecureProtocol.Org.BouncyCastle.Bcpg
{
    using System;
    using System.IO;
    using BestHTTP.SecureProtocol.Org.BouncyCastle.Utilities;
    using BestHTTP.SecureProtocol.Org.BouncyCastle.Utilities.IO;

    /// <remarks>Reader for PGP objects.</remarks>
    public class BcpgInputStream
        : BaseInputStream
    {
        private readonly Stream m_in;
        private          bool   next;
        private          int    nextB;

        internal static BcpgInputStream Wrap(
            Stream inStr)
        {
            if (inStr is BcpgInputStream) return (BcpgInputStream)inStr;

            return new BcpgInputStream(inStr);
        }

        private BcpgInputStream(
            Stream inputStream)
        {
            this.m_in = inputStream;
        }

        public override int ReadByte()
        {
            if (this.next)
            {
                this.next = false;
                return this.nextB;
            }

            return this.m_in.ReadByte();
        }

        public override int Read(
            byte[] buffer,
            int offset,
            int count)
        {
            // Strangely, when count == 0, we should still attempt to read a byte
//			if (count == 0)
//				return 0;

            if (!this.next)
                return this.m_in.Read(buffer, offset, count);

            // We have next byte waiting, so return it

            if (this.nextB < 0)
                return 0; // EndOfStream

            if (buffer == null)
                throw new ArgumentNullException("buffer");

            buffer[offset] = (byte)this.nextB;
            this.next      = false;

            return 1;
        }

        public byte[] ReadAll() { return Streams.ReadAll(this); }

        public void ReadFully(
            byte[] buffer,
            int off,
            int len)
        {
            if (Streams.ReadFully(this, buffer, off, len) < len)
                throw new EndOfStreamException();
        }

        public void ReadFully(
            byte[] buffer)
        {
            this.ReadFully(buffer, 0, buffer.Length);
        }

        /// <summary>Returns the next packet tag in the stream.</summary>
        public PacketTag NextPacketTag()
        {
            if (!this.next)
            {
                try
                {
                    this.nextB = this.m_in.ReadByte();
                }
                catch (EndOfStreamException)
                {
                    this.nextB = -1;
                }

                this.next = true;
            }

            if (this.nextB < 0)
                return (PacketTag)this.nextB;

            var maskB = this.nextB & 0x3f;
            if ((this.nextB & 0x40) == 0) // old
                maskB >>= 2;
            return (PacketTag)maskB;
        }

        public Packet ReadPacket()
        {
            var hdr = this.ReadByte();

            if (hdr < 0) return null;

            if ((hdr & 0x80) == 0) throw new IOException("invalid header encountered");

            var       newPacket = (hdr & 0x40) != 0;
            PacketTag tag       = 0;
            var       bodyLen   = 0;
            var       partial   = false;

            if (newPacket)
            {
                tag = (PacketTag)(hdr & 0x3f);

                var l = this.ReadByte();

                if (l < 192)
                {
                    bodyLen = l;
                }
                else if (l <= 223)
                {
                    var b = this.m_in.ReadByte();
                    bodyLen = ((l - 192) << 8) + b + 192;
                }
                else if (l == 255)
                {
                    bodyLen = (this.m_in.ReadByte() << 24) | (this.m_in.ReadByte() << 16)
                                                           | (this.m_in.ReadByte() << 8) | this.m_in.ReadByte();
                }
                else
                {
                    partial = true;
                    bodyLen = 1 << (l & 0x1f);
                }
            }
            else
            {
                var lengthType = hdr & 0x3;

                tag = (PacketTag)((hdr & 0x3f) >> 2);

                switch (lengthType)
                {
                    case 0:
                        bodyLen = this.ReadByte();
                        break;
                    case 1:
                        bodyLen = (this.ReadByte() << 8) | this.ReadByte();
                        break;
                    case 2:
                        bodyLen = (this.ReadByte() << 24) | (this.ReadByte() << 16)
                                                          | (this.ReadByte() << 8) | this.ReadByte();
                        break;
                    case 3:
                        partial = true;
                        break;
                    default:
                        throw new IOException("unknown length type encountered");
                }
            }

            BcpgInputStream objStream;
            if (bodyLen == 0 && partial)
            {
                objStream = this;
            }
            else
            {
                var pis = new PartialInputStream(this, partial, bodyLen);
#if NETCF_1_0 || NETCF_2_0 || SILVERLIGHT || PORTABLE || NETFX_CORE
                Stream buf = pis;
#else
                Stream buf = new BufferedStream(pis);
#endif
                objStream = new BcpgInputStream(buf);
            }

            switch (tag)
            {
                case PacketTag.Reserved:
                    return new InputStreamPacket(objStream);
                case PacketTag.PublicKeyEncryptedSession:
                    return new PublicKeyEncSessionPacket(objStream);
                case PacketTag.Signature:
                    return new SignaturePacket(objStream);
                case PacketTag.SymmetricKeyEncryptedSessionKey:
                    return new SymmetricKeyEncSessionPacket(objStream);
                case PacketTag.OnePassSignature:
                    return new OnePassSignaturePacket(objStream);
                case PacketTag.SecretKey:
                    return new SecretKeyPacket(objStream);
                case PacketTag.PublicKey:
                    return new PublicKeyPacket(objStream);
                case PacketTag.SecretSubkey:
                    return new SecretSubkeyPacket(objStream);
                case PacketTag.CompressedData:
                    return new CompressedDataPacket(objStream);
                case PacketTag.SymmetricKeyEncrypted:
                    return new SymmetricEncDataPacket(objStream);
                case PacketTag.Marker:
                    return new MarkerPacket(objStream);
                case PacketTag.LiteralData:
                    return new LiteralDataPacket(objStream);
                case PacketTag.Trust:
                    return new TrustPacket(objStream);
                case PacketTag.UserId:
                    return new UserIdPacket(objStream);
                case PacketTag.UserAttribute:
                    return new UserAttributePacket(objStream);
                case PacketTag.PublicSubkey:
                    return new PublicSubkeyPacket(objStream);
                case PacketTag.SymmetricEncryptedIntegrityProtected:
                    return new SymmetricEncIntegrityPacket(objStream);
                case PacketTag.ModificationDetectionCode:
                    return new ModDetectionCodePacket(objStream);
                case PacketTag.Experimental1:
                case PacketTag.Experimental2:
                case PacketTag.Experimental3:
                case PacketTag.Experimental4:
                    return new ExperimentalPacket(tag, objStream);
                default:
                    throw new IOException("unknown packet type encountered: " + tag);
            }
        }

        public PacketTag SkipMarkerPackets()
        {
            PacketTag tag;
            while ((tag = this.NextPacketTag()) == PacketTag.Marker) this.ReadPacket();

            return tag;
        }

#if PORTABLE || NETFX_CORE
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                BestHTTP.SecureProtocol.Org.BouncyCastle.Utilities.Platform.Dispose(m_in);
            }
            base.Dispose(disposing);
        }
#else
        public override void Close()
        {
            Platform.Dispose(this.m_in);
            base.Close();
        }
#endif

        /// <summary>
        ///     A stream that overlays our input stream, allowing the user to only read a segment of it.
        ///     NB: dataLength will be negative if the segment length is in the upper range above 2**31.
        /// </summary>
        private class PartialInputStream
            : BaseInputStream
        {
            private readonly BcpgInputStream m_in;
            private          bool            partial;
            private          int             dataLength;

            internal PartialInputStream(
                BcpgInputStream bcpgIn,
                bool partial,
                int dataLength)
            {
                this.m_in       = bcpgIn;
                this.partial    = partial;
                this.dataLength = dataLength;
            }

            public override int ReadByte()
            {
                do
                {
                    if (this.dataLength != 0)
                    {
                        var ch = this.m_in.ReadByte();
                        if (ch < 0) throw new EndOfStreamException("Premature end of stream in PartialInputStream");
                        this.dataLength--;
                        return ch;
                    }
                } while (this.partial && this.ReadPartialDataLength() >= 0);

                return -1;
            }

            public override int Read(byte[] buffer, int offset, int count)
            {
                do
                {
                    if (this.dataLength != 0)
                    {
                        var readLen = this.dataLength > count || this.dataLength < 0 ? count : this.dataLength;
                        var len     = this.m_in.Read(buffer, offset, readLen);
                        if (len < 1) throw new EndOfStreamException("Premature end of stream in PartialInputStream");
                        this.dataLength -= len;
                        return len;
                    }
                } while (this.partial && this.ReadPartialDataLength() >= 0);

                return 0;
            }

            private int ReadPartialDataLength()
            {
                var l = this.m_in.ReadByte();

                if (l < 0) return -1;

                this.partial = false;

                if (l < 192)
                {
                    this.dataLength = l;
                }
                else if (l <= 223)
                {
                    this.dataLength = ((l - 192) << 8) + this.m_in.ReadByte() + 192;
                }
                else if (l == 255)
                {
                    this.dataLength = (this.m_in.ReadByte() << 24) | (this.m_in.ReadByte() << 16)
                                                                   | (this.m_in.ReadByte() << 8) | this.m_in.ReadByte();
                }
                else
                {
                    this.partial    = true;
                    this.dataLength = 1 << (l & 0x1f);
                }

                return 0;
            }
        }
    }
}
#pragma warning restore
#endif