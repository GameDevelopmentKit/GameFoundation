#if !BESTHTTP_DISABLE_ALTERNATE_SSL && (!UNITY_WEBGL || UNITY_EDITOR)
#pragma warning disable
namespace BestHTTP.SecureProtocol.Org.BouncyCastle.Crypto.Digests
{
    using System;
    using BestHTTP.SecureProtocol.Org.BouncyCastle.Utilities;

    /// <summary>
    ///     ParallelHash - a hash designed  to  support the efficient hashing of very long strings, by taking advantage,
    ///     of the parallelism available in modern processors with an optional XOF mode.
    ///     <para>
    ///         From NIST Special Publication 800-185 - SHA-3 Derived Functions:cSHAKE, KMAC, TupleHash and ParallelHash
    ///     </para>
    /// </summary>
    public class ParallelHash
        : IXof, IDigest
    {
        private static readonly byte[] N_PARALLEL_HASH = Strings.ToByteArray("ParallelHash");

        private readonly CShakeDigest cshake;
        private readonly CShakeDigest compressor;
        private readonly int          bitLength;
        private readonly int          outputLength;
        private readonly int          B;
        private readonly byte[]       buffer;
        private readonly byte[]       compressorBuffer;

        private bool firstOutput;
        private int  nCount;
        private int  bufOff;

        /**
         * Base constructor.
         * 
         * @param bitLength bit length of the underlying SHAKE function, 128 or 256.
         * @param S the customization string - available for local use.
         * @param B the blocksize (in bytes) for hashing.
         */
        public ParallelHash(int bitLength, byte[] S, int B)
            : this(bitLength, S, B, bitLength * 2)
        {
        }

        public ParallelHash(int bitLength, byte[] S, int B, int outputSize)
        {
            this.cshake           = new CShakeDigest(bitLength, N_PARALLEL_HASH, S);
            this.compressor       = new CShakeDigest(bitLength, new byte[0], new byte[0]);
            this.bitLength        = bitLength;
            this.B                = B;
            this.outputLength     = (outputSize + 7) / 8;
            this.buffer           = new byte[B];
            this.compressorBuffer = new byte[bitLength * 2 / 8];

            this.Reset();
        }

        public ParallelHash(ParallelHash source)
        {
            this.cshake           = new CShakeDigest(source.cshake);
            this.compressor       = new CShakeDigest(source.compressor);
            this.bitLength        = source.bitLength;
            this.B                = source.B;
            this.outputLength     = source.outputLength;
            this.buffer           = Arrays.Clone(source.buffer);
            this.compressorBuffer = Arrays.Clone(source.compressorBuffer);
        }

        public virtual string AlgorithmName => "ParallelHash" + this.cshake.AlgorithmName.Substring(6);

        public virtual int GetByteLength() { return this.cshake.GetByteLength(); }

        public virtual int GetDigestSize() { return this.outputLength; }

        public virtual void Update(byte b)
        {
            this.buffer[this.bufOff++] = b;
            if (this.bufOff == this.buffer.Length) this.compress();
        }

        public virtual void BlockUpdate(byte[] inBuf, int inOff, int len)
        {
            len = Math.Max(0, len);

            //
            // fill the current word
            //
            var i = 0;
            if (this.bufOff != 0)
            {
                while (i < len && this.bufOff != this.buffer.Length) this.buffer[this.bufOff++] = inBuf[inOff + i++];

                if (this.bufOff == this.buffer.Length) this.compress();
            }

            if (i < len)
                while (len - i > this.B)
                {
                    this.compress(inBuf, inOff + i, this.B);
                    i += this.B;
                }

            while (i < len) this.Update(inBuf[inOff + i++]);
        }

        private void compress()
        {
            this.compress(this.buffer, 0, this.bufOff);
            this.bufOff = 0;
        }

        private void compress(byte[] buf, int offSet, int len)
        {
            this.compressor.BlockUpdate(buf, offSet, len);
            this.compressor.DoFinal(this.compressorBuffer, 0, this.compressorBuffer.Length);

            this.cshake.BlockUpdate(this.compressorBuffer, 0, this.compressorBuffer.Length);

            this.nCount++;
        }

        private void wrapUp(int outputSize)
        {
            if (this.bufOff != 0) this.compress();
            var nOut   = XofUtilities.RightEncode(this.nCount);
            var encOut = XofUtilities.RightEncode(outputSize * 8);

            this.cshake.BlockUpdate(nOut, 0, nOut.Length);
            this.cshake.BlockUpdate(encOut, 0, encOut.Length);

            this.firstOutput = false;
        }

        public virtual int DoFinal(byte[] outBuf, int outOff)
        {
            if (this.firstOutput) this.wrapUp(this.outputLength);

            var rv = this.cshake.DoFinal(outBuf, outOff, this.GetDigestSize());

            this.Reset();

            return rv;
        }

        public virtual int DoFinal(byte[] outBuf, int outOff, int outLen)
        {
            if (this.firstOutput) this.wrapUp(this.outputLength);

            var rv = this.cshake.DoFinal(outBuf, outOff, outLen);

            this.Reset();

            return rv;
        }

        public virtual int DoOutput(byte[] outBuf, int outOff, int outLen)
        {
            if (this.firstOutput) this.wrapUp(0);

            return this.cshake.DoOutput(outBuf, outOff, outLen);
        }

        public virtual void Reset()
        {
            this.cshake.Reset();
            Arrays.Clear(this.buffer);

            var hdr = XofUtilities.LeftEncode(this.B);
            this.cshake.BlockUpdate(hdr, 0, hdr.Length);

            this.nCount      = 0;
            this.bufOff      = 0;
            this.firstOutput = true;
        }
    }
}
#pragma warning restore
#endif