#if !BESTHTTP_DISABLE_ALTERNATE_SSL && (!UNITY_WEBGL || UNITY_EDITOR)
#pragma warning disable
namespace BestHTTP.SecureProtocol.Org.BouncyCastle.Tls
{
    using System;
    using System.IO;

    /// <summary>A queue for bytes. This file could be more optimized.</summary>
    public sealed class ByteQueue
    {
        /// <returns>The smallest number which can be written as 2^x which is bigger than i.</returns>
        public static int NextTwoPow(int i)
        {
            /*
             * This code is based of a lot of code I found on the Internet which mostly
             * referenced a book called "Hacking delight".
             */
            i |= i >> 1;
            i |= i >> 2;
            i |= i >> 4;
            i |= i >> 8;
            i |= i >> 16;
            return i + 1;
        }

        /// <summary>The buffer where we store our data.</summary>
        private byte[] m_databuf;

        /// <summary>How many bytes at the beginning of the buffer are skipped.</summary>
        private int m_skipped;

        /// <summary>How many bytes in the buffer are valid data.</summary>
        private int m_available;

        private readonly bool m_readOnlyBuf;

        public ByteQueue()
            : this(0)
        {
        }

        public ByteQueue(int capacity) { this.m_databuf = capacity == 0 ? TlsUtilities.EmptyBytes : new byte[capacity]; }

        public ByteQueue(byte[] buf, int off, int len)
        {
            this.m_databuf     = buf;
            this.m_skipped     = off;
            this.m_available   = len;
            this.m_readOnlyBuf = true;
        }

        /// <summary>Add some data to our buffer.</summary>
        /// <param name="buf">A byte-array to read data from.</param>
        /// <param name="off">How many bytes to skip at the beginning of the array.</param>
        /// <param name="len">How many bytes to read from the array.</param>
        public void AddData(byte[] buf, int off, int len)
        {
            if (this.m_readOnlyBuf)
                throw new InvalidOperationException("Cannot add data to read-only buffer");

            if (this.m_skipped + this.m_available + len > this.m_databuf.Length)
            {
                var desiredSize = NextTwoPow(this.m_available + len);
                if (desiredSize > this.m_databuf.Length)
                {
                    var tmp = new byte[desiredSize];
                    Array.Copy(this.m_databuf, this.m_skipped, tmp, 0, this.m_available);
                    this.m_databuf = tmp;
                }
                else
                {
                    Array.Copy(this.m_databuf, this.m_skipped, this.m_databuf, 0, this.m_available);
                }

                this.m_skipped = 0;
            }

            Array.Copy(buf, off, this.m_databuf, this.m_skipped + this.m_available, len);
            this.m_available += len;
        }

        /// <returns>The number of bytes which are available in this buffer.</returns>
        public int Available => this.m_available;

        /// <summary>Copy some bytes from the beginning of the data to the provided <see cref="Stream" />.</summary>
        /// <param name="output">The <see cref="Stream" /> to copy the bytes to.</param>
        /// <param name="length">How many bytes to copy.</param>
        public void CopyTo(Stream output, int length)
        {
            if (length > this.m_available)
                throw new InvalidOperationException("Cannot copy " + length + " bytes, only got " + this.m_available);

            output.Write(this.m_databuf, this.m_skipped, length);
        }

        /// <summary>Read data from the buffer.</summary>
        /// <param name="buf">The buffer where the read data will be copied to.</param>
        /// <param name="offset">How many bytes to skip at the beginning of buf.</param>
        /// <param name="len">How many bytes to read at all.</param>
        /// <param name="skip">How many bytes from our data to skip.</param>
        public void Read(byte[] buf, int offset, int len, int skip)
        {
            if (buf.Length - offset < len)
                throw new ArgumentException("Buffer size of " + buf.Length
                                                              + " is too small for a read of " + len + " bytes");
            if (this.m_available - skip < len) throw new InvalidOperationException("Not enough data to read");
            Array.Copy(this.m_databuf, this.m_skipped + skip, buf, offset, len);
        }

        /// <summary>
        ///     Return a <see cref="HandshakeMessageInput" /> over some bytes at the beginning of the data.
        /// </summary>
        /// <param name="length">How many bytes will be readable.</param>
        /// <returns>A <see cref="HandshakeMessageInput" /> over the data.</returns>
        internal HandshakeMessageInput ReadHandshakeMessage(int length)
        {
            if (length > this.m_available)
                throw new InvalidOperationException("Cannot read " + length + " bytes, only got " + this.m_available);

            var position = this.m_skipped;

            this.m_available -= length;
            this.m_skipped   += length;

            return new HandshakeMessageInput(this.m_databuf, position, length);
        }

        public int ReadInt32()
        {
            if (this.m_available < 4)
                throw new InvalidOperationException("Not enough data to read");

            return TlsUtilities.ReadInt32(this.m_databuf, this.m_skipped);
        }

        /// <summary>Remove some bytes from our data from the beginning.</summary>
        /// <param name="i">How many bytes to remove.</param>
        public void RemoveData(int i)
        {
            if (i > this.m_available)
                throw new InvalidOperationException("Cannot remove " + i + " bytes, only got " + this.m_available);

            /*
             * Skip the data.
             */
            this.m_available -= i;
            this.m_skipped   += i;
        }

        /// <summary>Remove data from the buffer.</summary>
        /// <param name="buf">The buffer where the removed data will be copied to.</param>
        /// <param name="off">How many bytes to skip at the beginning of buf.</param>
        /// <param name="len">How many bytes to read at all.</param>
        /// <param name="skip">How many bytes from our data to skip.</param>
        public void RemoveData(byte[] buf, int off, int len, int skip)
        {
            this.Read(buf, off, len, skip);
            this.RemoveData(skip + len);
        }

        public byte[] RemoveData(int len, int skip)
        {
            var buf = new byte[len];
            this.RemoveData(buf, 0, len, skip);
            return buf;
        }

        public void Shrink()
        {
            if (this.m_available == 0)
            {
                this.m_databuf = TlsUtilities.EmptyBytes;
                this.m_skipped = 0;
            }
            else
            {
                var desiredSize = NextTwoPow(this.m_available);
                if (desiredSize < this.m_databuf.Length)
                {
                    var tmp = new byte[desiredSize];
                    Array.Copy(this.m_databuf, this.m_skipped, tmp, 0, this.m_available);
                    this.m_databuf = tmp;
                    this.m_skipped = 0;
                }
            }
        }
    }
}
#pragma warning restore
#endif