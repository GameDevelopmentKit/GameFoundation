#if !BESTHTTP_DISABLE_ALTERNATE_SSL && (!UNITY_WEBGL || UNITY_EDITOR)
#pragma warning disable
namespace BestHTTP.SecureProtocol.Org.BouncyCastle.Bcpg
{
    using System;
    using System.Collections;
    using System.IO;
    using System.Text;
    using BestHTTP.SecureProtocol.Org.BouncyCastle.Utilities;
    using BestHTTP.SecureProtocol.Org.BouncyCastle.Utilities.IO;

    /**
     * reader for Base64 armored objects - read the headers and then start returning
     * bytes when the data is reached. An IOException is thrown if the CRC check
     * is detected and fails.
     * <p>
     *     By default a missing CRC will not cause an exception. To force CRC detection use:
     *     <pre>
     *         ArmoredInputStream aIn = ...
     *         aIn.setDetectMissingCRC(true);
     *     </pre>
     * </p>
     */
    public class ArmoredInputStream
        : BaseInputStream
    {
        /*
        * set up the decoding table.
        */
        private static readonly byte[] decodingTable;
        static ArmoredInputStream()
        {
            decodingTable = new byte[128];
            Arrays.Fill(decodingTable, 0xff);
            for (int i = 'A'; i <= 'Z'; i++) decodingTable[i] = (byte)(i - 'A');
            for (int i = 'a'; i <= 'z'; i++) decodingTable[i] = (byte)(i - 'a' + 26);
            for (int i = '0'; i <= '9'; i++) decodingTable[i] = (byte)(i - '0' + 52);
            decodingTable['+'] = 62;
            decodingTable['/'] = 63;
        }

        /**
         * decode the base 64 encoded input data.
         * 
         * @return the offset the data starts in out.
         */
        private static int Decode(int in0, int in1, int in2, int in3, int[] result)
        {
            if (in3 < 0)
                throw new EndOfStreamException("unexpected end of file in armored stream.");

            int b1, b2, b3, b4;
            if (in2 == '=')
            {
                b1 = decodingTable[in0];
                b2 = decodingTable[in1];
                if ((b1 | b2) >= 128)
                    throw new IOException("invalid armor");

                result[2] = ((b1 << 2) | (b2 >> 4)) & 0xff;
                return 2;
            }

            if (in3 == '=')
            {
                b1 = decodingTable[in0];
                b2 = decodingTable[in1];
                b3 = decodingTable[in2];
                if ((b1 | b2 | b3) >= 128)
                    throw new IOException("invalid armor");

                result[1] = ((b1 << 2) | (b2 >> 4)) & 0xff;
                result[2] = ((b2 << 4) | (b3 >> 2)) & 0xff;
                return 1;
            }

            b1 = decodingTable[in0];
            b2 = decodingTable[in1];
            b3 = decodingTable[in2];
            b4 = decodingTable[in3];
            if ((b1 | b2 | b3 | b4) >= 128)
                throw new IOException("invalid armor");

            result[0] = ((b1 << 2) | (b2 >> 4)) & 0xff;
            result[1] = ((b2 << 4) | (b3 >> 2)) & 0xff;
            result[2] = ((b3 << 6) | b4) & 0xff;
            return 0;
        }

        /*
         * Ignore missing CRC checksums.
         * https://tests.sequoia-pgp.org/#ASCII_Armor suggests that missing CRC sums do not invalidate the message.
         */
        private bool detectMissingChecksum;

        private readonly Stream input;
        private          bool   start  = true;
        private readonly int[]  outBuf = new int[3];
        private          int    bufPtr = 3;
        private readonly Crc24  crc    = new();
        private          bool   crcFound;
        private readonly bool   hasHeaders = true;
        private          string header;
        private          bool   newLineFound;
        private          bool   clearText;
        private          bool   restart;
        private          IList  headerList = Platform.CreateArrayList();
        private          int    lastC;
        private          bool   isEndOfStream;

        /**
         * Create a stream for reading a PGP armoured message, parsing up to a header
         * and then reading the data that follows.
         * 
         * @param input
         */
        public ArmoredInputStream(Stream input)
            : this(input, true)
        {
        }

        /**
         * Create an armoured input stream which will assume the data starts
         * straight away, or parse for headers first depending on the value of
         * hasHeaders.
         * 
         * @param input
         * @param hasHeaders true if headers are to be looked for, false otherwise.
         */
        public ArmoredInputStream(Stream input, bool hasHeaders)
        {
            this.input      = input;
            this.hasHeaders = hasHeaders;

            if (hasHeaders) this.ParseHeaders();

            this.start = false;
        }

        private bool ParseHeaders()
        {
            this.header = null;

            int c;
            var last        = 0;
            var headerFound = false;

            this.headerList = Platform.CreateArrayList();

            //
            // if restart we already have a header
            //
            if (this.restart)
                headerFound = true;
            else
                while ((c = this.input.ReadByte()) >= 0)
                {
                    if (c == '-' && (last == 0 || last == '\n' || last == '\r'))
                    {
                        headerFound = true;
                        break;
                    }

                    last = c;
                }

            if (headerFound)
            {
                var buf        = new StringBuilder("-");
                var eolReached = false;
                var crLf       = false;

                if (this.restart) // we've had to look ahead two '-'
                    buf.Append('-');

                while ((c = this.input.ReadByte()) >= 0)
                {
                    if (last == '\r' && c == '\n') crLf = true;
                    if (eolReached && last != '\r' && c == '\n') break;
                    if (eolReached && c == '\r') break;
                    if (c == '\r' || last != '\r' && c == '\n')
                    {
                        var line = buf.ToString();
                        if (line.Trim().Length < 1)
                            break;

                        if (this.headerList.Count > 0 && line.IndexOf(':') < 0)
                            throw new IOException("invalid armor header");

                        this.headerList.Add(line);
                        buf.Length = 0;
                    }

                    if (c != '\n' && c != '\r')
                    {
                        buf.Append((char)c);
                        eolReached = false;
                    }
                    else
                    {
                        if (c == '\r' || last != '\r' && c == '\n') eolReached = true;
                    }

                    last = c;
                }

                if (crLf) this.input.ReadByte(); // skip last \n
            }

            if (this.headerList.Count > 0) this.header = (string)this.headerList[0];

            this.clearText    = "-----BEGIN PGP SIGNED MESSAGE-----".Equals(this.header);
            this.newLineFound = true;

            return headerFound;
        }

        /**
         * @return true if we are inside the clear text section of a PGP
         * signed message.
         */
        public bool IsClearText() { return this.clearText; }

        /**
		 * @return true if the stream is actually at end of file.
		 */
        public bool IsEndOfStream() { return this.isEndOfStream; }

        /**
         * Return the armor header line (if there is one)
         * @return the armor header line, null if none present.
         */
        public string GetArmorHeaderLine() { return this.header; }

        /**
         * Return the armor headers (the lines after the armor header line),
         * @return an array of armor headers, null if there aren't any.
         */
        public string[] GetArmorHeaders()
        {
            if (this.headerList.Count <= 1)
                return null;

            var hdrs                                       = new string[this.headerList.Count - 1];
            for (var i = 0; i != hdrs.Length; i++) hdrs[i] = (string)this.headerList[i + 1];

            return hdrs;
        }

        private int ReadIgnoreSpace()
        {
            int c;
            do
            {
                c = this.input.ReadByte();
            } while (c == ' ' || c == '\t' || c == '\f' || c == '\u000B'); // \u000B ~ \v

            if (c >= 128)
                throw new IOException("invalid armor");

            return c;
        }

        public override int ReadByte()
        {
            if (this.start)
            {
                if (this.hasHeaders) this.ParseHeaders();

                this.crc.Reset();
                this.start = false;
            }

            int c;

            if (this.clearText)
            {
                c = this.input.ReadByte();

                if (c == '\r' || c == '\n' && this.lastC != '\r')
                {
                    this.newLineFound = true;
                }
                else if (this.newLineFound && c == '-')
                {
                    c = this.input.ReadByte();
                    if (c == '-') // a header, not dash escaped
                    {
                        this.clearText = false;
                        this.start     = true;
                        this.restart   = true;
                    }
                    else // a space - must be a dash escape
                    {
                        c = this.input.ReadByte();
                    }

                    this.newLineFound = false;
                }
                else
                {
                    if (c != '\n' && this.lastC != '\r') this.newLineFound = false;
                }

                this.lastC = c;

                if (c < 0) this.isEndOfStream = true;

                return c;
            }

            if (this.bufPtr > 2 || this.crcFound)
            {
                c = this.ReadIgnoreSpace();

                if (c == '\r' || c == '\n')
                {
                    c = this.ReadIgnoreSpace();

                    while (c == '\n' || c == '\r') c = this.ReadIgnoreSpace();

                    if (c < 0) // EOF
                    {
                        this.isEndOfStream = true;
                        return -1;
                    }

                    if (c == '=') // crc reached
                    {
                        this.bufPtr = Decode(this.ReadIgnoreSpace(), this.ReadIgnoreSpace(), this.ReadIgnoreSpace(), this.ReadIgnoreSpace(), this.outBuf);
                        if (this.bufPtr == 0)
                        {
                            var i = ((this.outBuf[0] & 0xff) << 16)
                                    | ((this.outBuf[1] & 0xff) << 8)
                                    | (this.outBuf[2] & 0xff);

                            this.crcFound = true;

                            if (i != this.crc.Value) throw new IOException("crc check failed in armored message.");
                            return this.ReadByte();
                        }

                        if (this.detectMissingChecksum) throw new IOException("no crc found in armored message");
                    }
                    else if (c == '-') // end of record reached
                    {
                        while ((c = this.input.ReadByte()) >= 0)
                            if (c == '\n' || c == '\r')
                                break;

                        if (!this.crcFound && this.detectMissingChecksum) throw new IOException("crc check not found");

                        this.crcFound = false;
                        this.start    = true;
                        this.bufPtr   = 3;

                        if (c < 0) this.isEndOfStream = true;

                        return -1;
                    }
                    else // data
                    {
                        this.bufPtr = Decode(c, this.ReadIgnoreSpace(), this.ReadIgnoreSpace(), this.ReadIgnoreSpace(), this.outBuf);
                    }
                }
                else
                {
                    if (c >= 0)
                    {
                        this.bufPtr = Decode(c, this.ReadIgnoreSpace(), this.ReadIgnoreSpace(), this.ReadIgnoreSpace(), this.outBuf);
                    }
                    else
                    {
                        this.isEndOfStream = true;
                        return -1;
                    }
                }
            }

            c = this.outBuf[this.bufPtr++];

            this.crc.Update(c);

            return c;
        }

        /**
         * Reads up to
         * <code>len</code>
         * bytes of data from the input stream into
         * an array of bytes.  An attempt is made to read as many as
         * <code>len</code>
         * bytes, but a smaller number may be read.
         * The number of bytes actually read is returned as an integer.
         * 
         * The first byte read is stored into element
         * <code>b[off]</code>
         * , the
         * next one into
         * <code>b[off+1]</code>
         * , and so on. The number of bytes read
         * is, at most, equal to
         * <code>len</code>
         * .
         * 
         * NOTE: We need to override the custom behavior of Java's {@link InputStream#read(byte[], int, int)},
         * as the upstream method silently swallows {@link IOException IOExceptions}.
         * This would cause CRC checksum errors to go unnoticed.
         * 
         * @see
         * <a href="https://github.com/bcgit/bc-java/issues/998">Related BC bug report</a>
         * @param b byte array
         * @param off offset at which we start writing data to the array
         * @param len number of bytes we write into the array
         * @return total number of bytes read into the buffer
         * 
         * @throws IOException if an exception happens AT ANY POINT
         */
        public override int Read(byte[] b, int off, int len)
        {
            this.CheckIndexSize(b.Length, off, len);

            var pos = 0;
            while (pos < len)
            {
                var c = this.ReadByte();
                if (c < 0)
                    break;

                b[off + pos++] = (byte)c;
            }

            return pos;
        }

        private void CheckIndexSize(int size, int off, int len)
        {
            if (off < 0 || len < 0)
                throw new IndexOutOfRangeException("Offset and length cannot be negative.");
            if (off > size - len)
                throw new IndexOutOfRangeException("Invalid offset and length.");
        }

#if PORTABLE || NETFX_CORE
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                BestHTTP.SecureProtocol.Org.BouncyCastle.Utilities.Platform.Dispose(input);
            }
            base.Dispose(disposing);
        }
#else
        public override void Close()
        {
            Platform.Dispose(this.input);
            base.Close();
        }
#endif

        /**
         * Change how the stream should react if it encounters missing CRC checksum.
         * The default value is false (ignore missing CRC checksums). If the behavior is set to true,
         * an {@link IOException} will be thrown if a missing CRC checksum is encountered.
         * 
         * @param detectMissing ignore missing CRC sums
         */
        public virtual void SetDetectMissingCrc(bool detectMissing) { this.detectMissingChecksum = detectMissing; }
    }
}
#pragma warning restore
#endif