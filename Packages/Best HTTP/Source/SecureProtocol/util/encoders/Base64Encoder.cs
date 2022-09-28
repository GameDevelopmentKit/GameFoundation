#if !BESTHTTP_DISABLE_ALTERNATE_SSL && (!UNITY_WEBGL || UNITY_EDITOR)
#pragma warning disable
namespace BestHTTP.SecureProtocol.Org.BouncyCastle.Utilities.Encoders
{
    using System;
    using System.IO;

    public class Base64Encoder
        : IEncoder
    {
        protected readonly byte[] encodingTable =
        {
            (byte)'A', (byte)'B', (byte)'C', (byte)'D', (byte)'E', (byte)'F', (byte)'G',
            (byte)'H', (byte)'I', (byte)'J', (byte)'K', (byte)'L', (byte)'M', (byte)'N',
            (byte)'O', (byte)'P', (byte)'Q', (byte)'R', (byte)'S', (byte)'T', (byte)'U',
            (byte)'V', (byte)'W', (byte)'X', (byte)'Y', (byte)'Z',
            (byte)'a', (byte)'b', (byte)'c', (byte)'d', (byte)'e', (byte)'f', (byte)'g',
            (byte)'h', (byte)'i', (byte)'j', (byte)'k', (byte)'l', (byte)'m', (byte)'n',
            (byte)'o', (byte)'p', (byte)'q', (byte)'r', (byte)'s', (byte)'t', (byte)'u',
            (byte)'v',
            (byte)'w', (byte)'x', (byte)'y', (byte)'z',
            (byte)'0', (byte)'1', (byte)'2', (byte)'3', (byte)'4', (byte)'5', (byte)'6',
            (byte)'7', (byte)'8', (byte)'9',
            (byte)'+', (byte)'/'
        };

        protected byte padding = (byte)'=';

        /*
        * set up the decoding table.
        */
        protected readonly byte[] decodingTable = new byte[128];

        protected void InitialiseDecodingTable()
        {
            Arrays.Fill(decodingTable, (byte)0xff);

            for (int i = 0; i < encodingTable.Length; i++)
            {
                decodingTable[encodingTable[i]] = (byte)i;
            }
        }

        public Base64Encoder()
        {
            InitialiseDecodingTable();
        }

        public int Encode(byte[] inBuf, int inOff, int inLen, byte[] outBuf, int outOff)
        {
            var inPos  = inOff;
            var inEnd  = inOff + inLen - 2;
            var outPos = outOff;

            while (inPos < inEnd)
            {
                uint a1 = inBuf[inPos++];
                uint a2 = inBuf[inPos++];
                uint a3 = inBuf[inPos++];

                outBuf[outPos++] = this.encodingTable[(a1 >> 2) & 0x3F];
                outBuf[outPos++] = this.encodingTable[((a1 << 4) | (a2 >> 4)) & 0x3F];
                outBuf[outPos++] = this.encodingTable[((a2 << 2) | (a3 >> 6)) & 0x3F];
                outBuf[outPos++] = this.encodingTable[a3 & 0x3F];
            }

            switch (inLen - (inPos - inOff))
            {
                case 1:
                {
                    uint a1 = inBuf[inPos++];

                    outBuf[outPos++] = this.encodingTable[(a1 >> 2) & 0x3F];
                    outBuf[outPos++] = this.encodingTable[(a1 << 4) & 0x3F];
                    outBuf[outPos++] = this.padding;
                    outBuf[outPos++] = this.padding;
                    break;
                }
                case 2:
                {
                    uint a1 = inBuf[inPos++];
                    uint a2 = inBuf[inPos++];

                    outBuf[outPos++] = this.encodingTable[(a1 >> 2) & 0x3F];
                    outBuf[outPos++] = this.encodingTable[((a1 << 4) | (a2 >> 4)) & 0x3F];
                    outBuf[outPos++] = this.encodingTable[(a2 << 2) & 0x3F];
                    outBuf[outPos++] = this.padding;
                    break;
            }
            }

            return outPos - outOff;
        }

        /**
         * encode the input data producing a base 64 output stream.
         * 
         * @return the number of bytes produced.
         */
        public int Encode(byte[] buf, int off, int len, Stream outStream)
        {
            if (len < 0)
                return 0;

            var tmp       = new byte[72];
            var remaining = len;
            while (remaining > 0)
            {
                var inLen  = Math.Min(54, remaining);
                var outLen = this.Encode(buf, off, inLen, tmp, 0);
                outStream.Write(tmp, 0, outLen);
                off       += inLen;
                remaining -= inLen;
            }

            return (len + 2) / 3 * 4;
        }

        private bool Ignore(char c)
        {
            return c == '\n' || c == '\r' || c == '\t' || c == ' ';
        }

        /**
        * decode the base 64 encoded byte data writing it to the given output stream,
        * whitespace characters will be ignored.
        *
        * @return the number of bytes produced.
        */
        public int Decode(byte[] data, int off, int length, Stream outStream)
        {
            byte b1, b2, b3, b4;
            var  outBuffer = new byte[54]; // S/MIME standard
            var  bufOff    = 0;
            int  outLen    = 0;
            int  end       = off + length;

            while (end > off)
            {
                if (!this.Ignore((char)data[end - 1]))
                    break;

                end--;
            }

            int  i = off;
            int  finish = end - 4;

            i = this.NextI(data, i, finish);

            while (i < finish)
            {
                b1 = decodingTable[data[i++]];

                i = this.NextI(data, i, finish);

                b2 = decodingTable[data[i++]];

                i = this.NextI(data, i, finish);

                b3 = decodingTable[data[i++]];

                i = this.NextI(data, i, finish);

                b4 = decodingTable[data[i++]];

                if ((b1 | b2 | b3 | b4) >= 0x80)
                    throw new IOException("invalid characters encountered in base64 data");

                outBuffer[bufOff++] = (byte)((b1 << 2) | (b2 >> 4));
                outBuffer[bufOff++] = (byte)((b2 << 4) | (b3 >> 2));
                outBuffer[bufOff++] = (byte)((b3 << 6) | b4);

                if (bufOff == outBuffer.Length)
                {
                    outStream.Write(outBuffer, 0, bufOff);
                    bufOff = 0;
                }

                outLen += 3;

                i = this.NextI(data, i, finish);
            }

            if (bufOff > 0) outStream.Write(outBuffer, 0, bufOff);

            var e0 = this.NextI(data, i, end);
            var e1 = this.NextI(data, e0 + 1, end);
            var e2 = this.NextI(data, e1 + 1, end);
            var e3 = this.NextI(data, e2 + 1, end);

            outLen += this.DecodeLastBlock(outStream, (char)data[e0], (char)data[e1], (char)data[e2], (char)data[e3]);

            return outLen;
        }

        private int NextI(
            byte[]	data,
            int		i,
            int		finish)
        {
            while (i < finish && this.Ignore((char)data[i]))
            {
                i++;
            }
            return i;
        }

        /**
        * decode the base 64 encoded string data writing it to the given output stream,
        * whitespace characters will be ignored.
        *
        * @return the number of bytes produced.
        */
        public int DecodeString(string data, Stream outStream)
        {
            // Platform Implementation
//			byte[] bytes = Convert.FromBase64String(data);
//			outStream.Write(bytes, 0, bytes.Length);
//			return bytes.Length;

            byte b1, b2, b3, b4;
            int length = 0;

            int end = data.Length;

            while (end > 0)
            {
                if (!this.Ignore(data[end - 1]))
                    break;

                end--;
            }

            int  i = 0;
            int  finish = end - 4;

            i = this.NextI(data, i, finish);

            while (i < finish)
            {
                b1 = decodingTable[data[i++]];

                i = this.NextI(data, i, finish);

                b2 = decodingTable[data[i++]];

                i = this.NextI(data, i, finish);

                b3 = decodingTable[data[i++]];

                i = this.NextI(data, i, finish);

                b4 = decodingTable[data[i++]];

                if ((b1 | b2 | b3 | b4) >= 0x80)
                    throw new IOException("invalid characters encountered in base64 data");

                outStream.WriteByte((byte)((b1 << 2) | (b2 >> 4)));
                outStream.WriteByte((byte)((b2 << 4) | (b3 >> 2)));
                outStream.WriteByte((byte)((b3 << 6) | b4));

                length += 3;

                i = this.NextI(data, i, finish);
            }

            length += this.DecodeLastBlock(outStream, data[end - 4], data[end - 3], data[end - 2], data[end - 1]);

            return length;
        }

        private int DecodeLastBlock(
            Stream	outStream,
            char	c1,
            char	c2,
            char	c3,
            char	c4)
        {
            if (c3 == padding)
            {
                if (c4 != padding)
                    throw new IOException("invalid characters encountered at end of base64 data");

                byte b1 = decodingTable[c1];
                byte b2 = decodingTable[c2];

                if ((b1 | b2) >= 0x80)
                    throw new IOException("invalid characters encountered at end of base64 data");

                outStream.WriteByte((byte)((b1 << 2) | (b2 >> 4)));

                return 1;
            }

            if (c4 == padding)
            {
                byte b1 = decodingTable[c1];
                byte b2 = decodingTable[c2];
                byte b3 = decodingTable[c3];

                if ((b1 | b2 | b3) >= 0x80)
                    throw new IOException("invalid characters encountered at end of base64 data");

                outStream.WriteByte((byte)((b1 << 2) | (b2 >> 4)));
                outStream.WriteByte((byte)((b2 << 4) | (b3 >> 2)));

                return 2;
            }

            {
                byte b1 = decodingTable[c1];
                byte b2 = decodingTable[c2];
                byte b3 = decodingTable[c3];
                byte b4 = decodingTable[c4];

                if ((b1 | b2 | b3 | b4) >= 0x80)
                    throw new IOException("invalid characters encountered at end of base64 data");

                outStream.WriteByte((byte)((b1 << 2) | (b2 >> 4)));
                outStream.WriteByte((byte)((b2 << 4) | (b3 >> 2)));
                outStream.WriteByte((byte)((b3 << 6) | b4));

                return 3;
            }
        }

        private int NextI(string data, int i, int finish)
        {
            while (i < finish && this.Ignore(data[i]))
            {
                i++;
            }
            return i;
        }
    }
}
#pragma warning restore
#endif
