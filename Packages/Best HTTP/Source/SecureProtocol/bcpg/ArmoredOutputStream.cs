#if !BESTHTTP_DISABLE_ALTERNATE_SSL && (!UNITY_WEBGL || UNITY_EDITOR)
#pragma warning disable

#if PORTABLE || NETFX_CORE
using System.Linq;
#endif

namespace BestHTTP.SecureProtocol.Org.BouncyCastle.Bcpg
{
    using System.Collections;
    using System.Diagnostics;
    using System.IO;
    using BestHTTP.SecureProtocol.Org.BouncyCastle.Utilities;
    using BestHTTP.SecureProtocol.Org.BouncyCastle.Utilities.IO;

    /**
    * Basic output stream.
    */
    public class ArmoredOutputStream
        : BaseOutputStream
    {
        public static readonly string HeaderVersion = "Version";

        private static readonly byte[] encodingTable =
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

        /**
         * encode the input data producing a base 64 encoded byte array.
         */
        private static void Encode(
            Stream outStream,
            int[] data,
            int len)
        {
            Debug.Assert(len > 0);
            Debug.Assert(len < 4);

            var bs = new byte[4];
            var d1 = data[0];
            bs[0] = encodingTable[(d1 >> 2) & 0x3f];

            switch (len)
            {
                case 1:
                {
                    bs[1] = encodingTable[(d1 << 4) & 0x3f];
                    bs[2] = (byte)'=';
                    bs[3] = (byte)'=';
                    break;
                }
                case 2:
                {
                    var d2 = data[1];
                    bs[1] = encodingTable[((d1 << 4) | (d2 >> 4)) & 0x3f];
                    bs[2] = encodingTable[(d2 << 2) & 0x3f];
                    bs[3] = (byte)'=';
                    break;
                }
                case 3:
                {
                    var d2 = data[1];
                    var d3 = data[2];
                    bs[1] = encodingTable[((d1 << 4) | (d2 >> 4)) & 0x3f];
                    bs[2] = encodingTable[((d2 << 2) | (d3 >> 6)) & 0x3f];
                    bs[3] = encodingTable[d3 & 0x3f];
                    break;
                }
            }

            outStream.Write(bs, 0, bs.Length);
        }

        private readonly Stream outStream;
        private readonly int[]  buf = new int[3];
        private          int    bufPtr;
        private readonly Crc24  crc = new();
        private          int    chunkCount;
        private          int    lastb;

        private bool start = true;
        private bool clearText;
        private bool newLine;

        private string type;

        private static readonly string nl          = Platform.NewLine;
        private static readonly string headerStart = "-----BEGIN PGP ";
        private static readonly string headerTail  = "-----";
        private static readonly string footerStart = "-----END PGP ";
        private static readonly string footerTail  = "-----";

        private static readonly string Version = "BCPG C# v" + HTTPManager.UserAgent;

        private readonly IDictionary headers;

        public ArmoredOutputStream(Stream outStream)
        {
            this.outStream = outStream;
            this.headers   = Platform.CreateHashtable(1);
            this.SetHeader(HeaderVersion, Version);
        }

        public ArmoredOutputStream(Stream outStream, IDictionary headers)
            : this(outStream)
        {
            foreach (string header in headers.Keys)
            {
                var headerList = Platform.CreateArrayList(1);
                headerList.Add(headers[header]);

                this.headers[header] = headerList;
            }
        }

        /**
         * Set an additional header entry. Any current value(s) under the same name will be
         * replaced by the new one. A null value will clear the entry for name.         *
         * @param name the name of the header entry.
         * @param v the value of the header entry.
         */
        public void SetHeader(string name, string val)
        {
            if (val == null)
            {
                this.headers.Remove(name);
            }
            else
            {
                var valueList = (IList)this.headers[name];
                if (valueList == null)
                {
                    valueList          = Platform.CreateArrayList(1);
                    this.headers[name] = valueList;
                }
                else
                {
                    valueList.Clear();
                }

                valueList.Add(val);
            }
        }

        /**
         * Set an additional header entry. The current value(s) will continue to exist together
         * with the new one. Adding a null value has no effect.
         * 
         * @param name the name of the header entry.
         * @param value the value of the header entry.
         */
        public void AddHeader(string name, string val)
        {
            if (val == null || name == null)
                return;

            var valueList = (IList)this.headers[name];
            if (valueList == null)
            {
                valueList          = Platform.CreateArrayList(1);
                this.headers[name] = valueList;
            }

            valueList.Add(val);
        }

        /**
         * Reset the headers to only contain a Version string (if one is present).
         */
        public void ResetHeaders()
        {
            var versions = (IList)this.headers[HeaderVersion];

            this.headers.Clear();

            if (versions != null) this.headers[HeaderVersion] = versions;
        }

        /**
         * Start a clear text signed message.
         * @param hashAlgorithm
         */
        public void BeginClearText(
            HashAlgorithmTag hashAlgorithm)
        {
            string hash;

            switch (hashAlgorithm)
            {
                case HashAlgorithmTag.Sha1:
                    hash = "SHA1";
                    break;
                case HashAlgorithmTag.Sha256:
                    hash = "SHA256";
                    break;
                case HashAlgorithmTag.Sha384:
                    hash = "SHA384";
                    break;
                case HashAlgorithmTag.Sha512:
                    hash = "SHA512";
                    break;
                case HashAlgorithmTag.MD2:
                    hash = "MD2";
                    break;
                case HashAlgorithmTag.MD5:
                    hash = "MD5";
                    break;
                case HashAlgorithmTag.RipeMD160:
                    hash = "RIPEMD160";
                    break;
                default:
                    throw new IOException("unknown hash algorithm tag in beginClearText: " + hashAlgorithm);
            }

            this.DoWrite("-----BEGIN PGP SIGNED MESSAGE-----" + nl);
            this.DoWrite("Hash: " + hash + nl + nl);

            this.clearText = true;
            this.newLine   = true;
            this.lastb     = 0;
        }

        public void EndClearText() { this.clearText = false; }

        public override void WriteByte(
            byte b)
        {
            if (this.clearText)
            {
                this.outStream.WriteByte(b);

                if (this.newLine)
                {
                    if (!(b == '\n' && this.lastb == '\r')) this.newLine = false;
                    if (b == '-')
                    {
                        this.outStream.WriteByte((byte)' ');
                        this.outStream.WriteByte((byte)'-'); // dash escape
                    }
                }

                if (b == '\r' || b == '\n' && this.lastb != '\r') this.newLine = true;
                this.lastb = b;
                return;
            }

            if (this.start)
            {
                var newPacket = (b & 0x40) != 0;

                int tag;
                if (newPacket)
                    tag = b & 0x3f;
                else
                    tag = (b & 0x3f) >> 2;

                switch ((PacketTag)tag)
                {
                    case PacketTag.PublicKey:
                        this.type = "PUBLIC KEY BLOCK";
                        break;
                    case PacketTag.SecretKey:
                        this.type = "PRIVATE KEY BLOCK";
                        break;
                    case PacketTag.Signature:
                        this.type = "SIGNATURE";
                        break;
                    default:
                        this.type = "MESSAGE";
                        break;
                }

                this.DoWrite(headerStart + this.type + headerTail + nl);

                {
                    var versionHeaders = (IList)this.headers[HeaderVersion];
                    if (versionHeaders != null) this.WriteHeaderEntry(HeaderVersion, versionHeaders[0].ToString());
                }

                foreach (DictionaryEntry de in this.headers)
                {
                    var k = (string)de.Key;
                    if (k != HeaderVersion)
                    {
                        var values = (IList)de.Value;
                        foreach (string v in values) this.WriteHeaderEntry(k, v);
                    }
                }

                this.DoWrite(nl);

                this.start = false;
            }

            if (this.bufPtr == 3)
            {
                Encode(this.outStream, this.buf, this.bufPtr);
                this.bufPtr = 0;
                if ((++this.chunkCount & 0xf) == 0) this.DoWrite(nl);
            }

            this.crc.Update(b);
            this.buf[this.bufPtr++] = b & 0xff;
        }

        /**
         * <b>Note</b>
         * : Close() does not close the underlying stream. So it is possible to write
         * multiple objects using armoring to a single stream.
         */
#if PORTABLE || NETFX_CORE
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (type == null)
                    return;

                DoClose();

                type = null;
                start = true;
            }
            base.Dispose(disposing);
        }
#else
        public override void Close()
        {
            if (this.type == null)
                return;

            this.DoClose();

            this.type  = null;
            this.start = true;

            base.Close();
        }
#endif

        private void DoClose()
        {
            if (this.bufPtr > 0) Encode(this.outStream, this.buf, this.bufPtr);

            this.DoWrite(nl + '=');

            var crcV = this.crc.Value;

            this.buf[0] = (crcV >> 16) & 0xff;
            this.buf[1] = (crcV >> 8) & 0xff;
            this.buf[2] = crcV & 0xff;

            Encode(this.outStream, this.buf, 3);

            this.DoWrite(nl);
            this.DoWrite(footerStart);
            this.DoWrite(this.type);
            this.DoWrite(footerTail);
            this.DoWrite(nl);

            this.outStream.Flush();
        }

        private void WriteHeaderEntry(
            string name,
            string v)
        {
            this.DoWrite(name + ": " + v + nl);
        }

        private void DoWrite(
            string s)
        {
            var bs = Strings.ToAsciiByteArray(s);
            this.outStream.Write(bs, 0, bs.Length);
        }
    }
}
#pragma warning restore
#endif