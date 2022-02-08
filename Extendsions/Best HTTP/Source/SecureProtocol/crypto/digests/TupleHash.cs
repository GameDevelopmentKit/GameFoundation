#if !BESTHTTP_DISABLE_ALTERNATE_SSL && (!UNITY_WEBGL || UNITY_EDITOR)
#pragma warning disable
namespace BestHTTP.SecureProtocol.Org.BouncyCastle.Crypto.Digests
{
    using BestHTTP.SecureProtocol.Org.BouncyCastle.Utilities;

    /// <summary>
    ///     TupleHash - a hash designed  to  simply  hash  a  tuple  of  input  strings,  any  or  all  of  which  may  be  empty  strings,
    ///     in  an  unambiguous way with an optional XOF mode.
    ///     <para>
    ///         From NIST Special Publication 800-185 - SHA-3 Derived Functions:cSHAKE, KMAC, TupleHash and ParallelHash
    ///     </para>
    /// </summary>
    public class TupleHash
        : IXof, IDigest
    {
        private static readonly byte[] N_TUPLE_HASH = Strings.ToByteArray("TupleHash");

        private readonly CShakeDigest cshake;
        private readonly int          bitLength;
        private readonly int          outputLength;

        private bool firstOutput;

        /**
         * Base constructor.
         * 
         * @param bitLength bit length of the underlying SHAKE function, 128 or 256.
         * @param S         the customization string - available for local use.
         */
        public TupleHash(int bitLength, byte[] S)
            : this(bitLength, S, bitLength * 2)
        {
        }

        public TupleHash(int bitLength, byte[] S, int outputSize)
        {
            this.cshake       = new CShakeDigest(bitLength, N_TUPLE_HASH, S);
            this.bitLength    = bitLength;
            this.outputLength = (outputSize + 7) / 8;

            this.Reset();
        }

        public TupleHash(TupleHash original)
        {
            this.cshake       = new CShakeDigest(original.cshake);
            this.bitLength    = this.cshake.fixedOutputLength;
            this.outputLength = this.bitLength * 2 / 8;
            this.firstOutput  = original.firstOutput;
        }

        public virtual string AlgorithmName => "TupleHash" + this.cshake.AlgorithmName.Substring(6);

        public virtual int GetByteLength() { return this.cshake.GetByteLength(); }

        public virtual int GetDigestSize() { return this.outputLength; }

        public virtual void Update(byte b)
        {
            var bytes = XofUtilities.Encode(b);
            this.cshake.BlockUpdate(bytes, 0, bytes.Length);
        }

        public virtual void BlockUpdate(byte[] inBuf, int inOff, int len)
        {
            var bytes = XofUtilities.Encode(inBuf, inOff, len);
            this.cshake.BlockUpdate(bytes, 0, bytes.Length);
        }

        private void wrapUp(int outputSize)
        {
            var encOut = XofUtilities.RightEncode(outputSize * 8);

            this.cshake.BlockUpdate(encOut, 0, encOut.Length);

            this.firstOutput = false;
        }

        public virtual int DoFinal(byte[] outBuf, int outOff)
        {
            if (this.firstOutput) this.wrapUp(this.GetDigestSize());

            var rv = this.cshake.DoFinal(outBuf, outOff, this.GetDigestSize());

            this.Reset();

            return rv;
        }

        public virtual int DoFinal(byte[] outBuf, int outOff, int outLen)
        {
            if (this.firstOutput) this.wrapUp(this.GetDigestSize());

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
            this.firstOutput = true;
        }
    }
}
#pragma warning restore
#endif