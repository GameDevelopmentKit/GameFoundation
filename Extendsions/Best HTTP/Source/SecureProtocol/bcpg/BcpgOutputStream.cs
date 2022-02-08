#if !BESTHTTP_DISABLE_ALTERNATE_SSL && (!UNITY_WEBGL || UNITY_EDITOR)
#pragma warning disable
namespace BestHTTP.SecureProtocol.Org.BouncyCastle.Bcpg
{
    using System;
    using System.IO;
    using BestHTTP.SecureProtocol.Org.BouncyCastle.Utilities;
    using BestHTTP.SecureProtocol.Org.BouncyCastle.Utilities.IO;

    /// <remarks>Basic output stream.</remarks>
    public class BcpgOutputStream
        : BaseOutputStream
    {
        internal static BcpgOutputStream Wrap(
            Stream outStr)
        {
            if (outStr is BcpgOutputStream) return (BcpgOutputStream)outStr;

            return new BcpgOutputStream(outStr);
        }

        private readonly Stream outStr;
        private          byte[] partialBuffer;
        private readonly int    partialBufferLength;
        private readonly int    partialPower;
        private          int    partialOffset;
        private const    int    BufferSizePower = 16; // 2^16 size buffer on long files

        /// <summary>Create a stream representing a general packet.</summary>
        /// <param name="outStr">Output stream to write to.</param>
        public BcpgOutputStream(
            Stream outStr)
        {
            if (outStr == null)
                throw new ArgumentNullException("outStr");

            this.outStr = outStr;
        }

        /// <summary>Create a stream representing an old style partial object.</summary>
        /// <param name="outStr">Output stream to write to.</param>
        /// <param name="tag">The packet tag for the object.</param>
        public BcpgOutputStream(
            Stream outStr,
            PacketTag tag)
        {
            if (outStr == null)
                throw new ArgumentNullException("outStr");

            this.outStr = outStr;
            this.WriteHeader(tag, true, true, 0);
        }

        /// <summary>Create a stream representing a general packet.</summary>
        /// <param name="outStr">Output stream to write to.</param>
        /// <param name="tag">Packet tag.</param>
        /// <param name="length">Size of chunks making up the packet.</param>
        /// <param name="oldFormat">If true, the header is written out in old format.</param>
        public BcpgOutputStream(
            Stream outStr,
            PacketTag tag,
            long length,
            bool oldFormat)
        {
            if (outStr == null)
                throw new ArgumentNullException("outStr");

            this.outStr = outStr;

            if (length > 0xFFFFFFFFL)
            {
                this.WriteHeader(tag, false, true, 0);
                this.partialBufferLength = 1 << BufferSizePower;
                this.partialBuffer       = new byte[this.partialBufferLength];
                this.partialPower        = BufferSizePower;
                this.partialOffset       = 0;
            }
            else
            {
                this.WriteHeader(tag, oldFormat, false, length);
            }
        }

        /// <summary>Create a new style partial input stream buffered into chunks.</summary>
        /// <param name="outStr">Output stream to write to.</param>
        /// <param name="tag">Packet tag.</param>
        /// <param name="length">Size of chunks making up the packet.</param>
        public BcpgOutputStream(
            Stream outStr,
            PacketTag tag,
            long length)
        {
            if (outStr == null)
                throw new ArgumentNullException("outStr");

            this.outStr = outStr;
            this.WriteHeader(tag, false, false, length);
        }

        /// <summary>Create a new style partial input stream buffered into chunks.</summary>
        /// <param name="outStr">Output stream to write to.</param>
        /// <param name="tag">Packet tag.</param>
        /// <param name="buffer">Buffer to use for collecting chunks.</param>
        public BcpgOutputStream(
            Stream outStr,
            PacketTag tag,
            byte[] buffer)
        {
            if (outStr == null)
                throw new ArgumentNullException("outStr");

            this.outStr = outStr;
            this.WriteHeader(tag, false, true, 0);

            this.partialBuffer = buffer;

            var length                                                           = (uint)this.partialBuffer.Length;
            for (this.partialPower = 0; length != 1; this.partialPower++) length >>= 1;

            if (this.partialPower > 30) throw new IOException("Buffer cannot be greater than 2^30 in length.");
            this.partialBufferLength = 1 << this.partialPower;
            this.partialOffset       = 0;
        }

        private void WriteNewPacketLength(
            long bodyLen)
        {
            if (bodyLen < 192)
            {
                this.outStr.WriteByte((byte)bodyLen);
            }
            else if (bodyLen <= 8383)
            {
                bodyLen -= 192;

                this.outStr.WriteByte((byte)(((bodyLen >> 8) & 0xff) + 192));
                this.outStr.WriteByte((byte)bodyLen);
            }
            else
            {
                this.outStr.WriteByte(0xff);
                this.outStr.WriteByte((byte)(bodyLen >> 24));
                this.outStr.WriteByte((byte)(bodyLen >> 16));
                this.outStr.WriteByte((byte)(bodyLen >> 8));
                this.outStr.WriteByte((byte)bodyLen);
            }
        }

        private void WriteHeader(
            PacketTag tag,
            bool oldPackets,
            bool partial,
            long bodyLen)
        {
            var hdr = 0x80;

            if (this.partialBuffer != null)
            {
                this.PartialFlush(true);
                this.partialBuffer = null;
            }

            if (oldPackets)
            {
                hdr |= (int)tag << 2;

                if (partial)
                {
                    this.WriteByte((byte)(hdr | 0x03));
                }
                else
                {
                    if (bodyLen <= 0xff)
                    {
                        this.WriteByte((byte)hdr);
                        this.WriteByte((byte)bodyLen);
                    }
                    else if (bodyLen <= 0xffff)
                    {
                        this.WriteByte((byte)(hdr | 0x01));
                        this.WriteByte((byte)(bodyLen >> 8));
                        this.WriteByte((byte)bodyLen);
                    }
                    else
                    {
                        this.WriteByte((byte)(hdr | 0x02));
                        this.WriteByte((byte)(bodyLen >> 24));
                        this.WriteByte((byte)(bodyLen >> 16));
                        this.WriteByte((byte)(bodyLen >> 8));
                        this.WriteByte((byte)bodyLen);
                    }
                }
            }
            else
            {
                hdr |= 0x40 | (int)tag;
                this.WriteByte((byte)hdr);

                if (partial)
                    this.partialOffset = 0;
                else
                    this.WriteNewPacketLength(bodyLen);
            }
        }

        private void PartialFlush(
            bool isLast)
        {
            if (isLast)
            {
                this.WriteNewPacketLength(this.partialOffset);
                this.outStr.Write(this.partialBuffer, 0, this.partialOffset);
            }
            else
            {
                this.outStr.WriteByte((byte)(0xE0 | this.partialPower));
                this.outStr.Write(this.partialBuffer, 0, this.partialBufferLength);
            }

            this.partialOffset = 0;
        }

        private void WritePartial(
            byte b)
        {
            if (this.partialOffset == this.partialBufferLength) this.PartialFlush(false);

            this.partialBuffer[this.partialOffset++] = b;
        }

        private void WritePartial(
            byte[] buffer,
            int off,
            int len)
        {
            if (this.partialOffset == this.partialBufferLength) this.PartialFlush(false);

            if (len <= this.partialBufferLength - this.partialOffset)
            {
                Array.Copy(buffer, off, this.partialBuffer, this.partialOffset, len);
                this.partialOffset += len;
            }
            else
            {
                var diff = this.partialBufferLength - this.partialOffset;
                Array.Copy(buffer, off, this.partialBuffer, this.partialOffset, diff);
                off += diff;
                len -= diff;
                this.PartialFlush(false);
                while (len > this.partialBufferLength)
                {
                    Array.Copy(buffer, off, this.partialBuffer, 0, this.partialBufferLength);
                    off += this.partialBufferLength;
                    len -= this.partialBufferLength;
                    this.PartialFlush(false);
                }

                Array.Copy(buffer, off, this.partialBuffer, 0, len);
                this.partialOffset += len;
            }
        }
        public override void WriteByte(
            byte value)
        {
            if (this.partialBuffer != null)
                this.WritePartial(value);
            else
                this.outStr.WriteByte(value);
        }
        public override void Write(
            byte[] buffer,
            int offset,
            int count)
        {
            if (this.partialBuffer != null)
                this.WritePartial(buffer, offset, count);
            else
                this.outStr.Write(buffer, offset, count);
        }

        // Additional helper methods to write primitive types
        internal virtual void WriteShort(
            short n)
        {
            this.Write(
                (byte)(n >> 8),
                (byte)n);
        }
        internal virtual void WriteInt(
            int n)
        {
            this.Write(
                (byte)(n >> 24),
                (byte)(n >> 16),
                (byte)(n >> 8),
                (byte)n);
        }
        internal virtual void WriteLong(
            long n)
        {
            this.Write(
                (byte)(n >> 56),
                (byte)(n >> 48),
                (byte)(n >> 40),
                (byte)(n >> 32),
                (byte)(n >> 24),
                (byte)(n >> 16),
                (byte)(n >> 8),
                (byte)n);
        }

        public void WritePacket(
            ContainedPacket p)
        {
            p.Encode(this);
        }

        internal void WritePacket(
            PacketTag tag,
            byte[] body,
            bool oldFormat)
        {
            this.WriteHeader(tag, oldFormat, false, body.Length);
            this.Write(body);
        }

        public void WriteObject(
            BcpgObject bcpgObject)
        {
            bcpgObject.Encode(this);
        }

        public void WriteObjects(
            params BcpgObject[] v)
        {
            foreach (var o in v) o.Encode(this);
        }

        /// <summary>Flush the underlying stream.</summary>
        public override void Flush() { this.outStr.Flush(); }

        /// <summary>Finish writing out the current packet without closing the underlying stream.</summary>
        public void Finish()
        {
            if (this.partialBuffer != null)
            {
                this.PartialFlush(true);
                Array.Clear(this.partialBuffer, 0, this.partialBuffer.Length);
                this.partialBuffer = null;
            }
        }

#if PORTABLE || NETFX_CORE
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
			    this.Finish();
			    outStr.Flush();
                BestHTTP.SecureProtocol.Org.BouncyCastle.Utilities.Platform.Dispose(outStr);
            }
            base.Dispose(disposing);
        }
#else
        public override void Close()
        {
            this.Finish();
            this.outStr.Flush();
            Platform.Dispose(this.outStr);
            base.Close();
        }
#endif
    }
}
#pragma warning restore
#endif