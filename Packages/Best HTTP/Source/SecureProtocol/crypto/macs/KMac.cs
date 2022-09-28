#if !BESTHTTP_DISABLE_ALTERNATE_SSL && (!UNITY_WEBGL || UNITY_EDITOR)
#pragma warning disable
namespace BestHTTP.SecureProtocol.Org.BouncyCastle.Crypto.Macs
{
    using System;
    using BestHTTP.SecureProtocol.Org.BouncyCastle.Crypto.Digests;
    using BestHTTP.SecureProtocol.Org.BouncyCastle.Crypto.Parameters;
    using BestHTTP.SecureProtocol.Org.BouncyCastle.Utilities;

    public class KMac
        : IMac, IXof
    {
        private static readonly byte[] padding = new byte[100];

        private readonly CShakeDigest cshake;
        private readonly int          bitLength;
        private readonly int          outputLength;

        private byte[] key;
        private bool   initialised;
        private bool   firstOutput;

        public KMac(int bitLength, byte[] S)
        {
            this.cshake       = new CShakeDigest(bitLength, Strings.ToAsciiByteArray("KMAC"), S);
            this.bitLength    = bitLength;
            this.outputLength = bitLength * 2 / 8;
        }

        public string AlgorithmName => "KMAC" + this.cshake.AlgorithmName.Substring(6);

        public void BlockUpdate(byte[] input, int inOff, int len)
        {
            if (!this.initialised)
                throw new InvalidOperationException("KMAC not initialized");

            this.cshake.BlockUpdate(input, inOff, len);
        }

        public int DoFinal(byte[] output, int outOff)
        {
            if (this.firstOutput)
            {
                if (!this.initialised)
                    throw new InvalidOperationException("KMAC not initialized");

                var encOut = XofUtilities.RightEncode(this.GetMacSize() * 8);

                this.cshake.BlockUpdate(encOut, 0, encOut.Length);
            }

            var rv = this.cshake.DoFinal(output, outOff, this.GetMacSize());

            this.Reset();

            return rv;
        }

        public int DoFinal(byte[] output, int outOff, int outLen)
        {
            if (this.firstOutput)
            {
                if (!this.initialised)
                    throw new InvalidOperationException("KMAC not initialized");

                var encOut = XofUtilities.RightEncode(outLen * 8);

                this.cshake.BlockUpdate(encOut, 0, encOut.Length);
            }

            var rv = this.cshake.DoFinal(output, outOff, outLen);

            this.Reset();

            return rv;
        }

        public int DoOutput(byte[] output, int outOff, int outLen)
        {
            if (this.firstOutput)
            {
                if (!this.initialised)
                    throw new InvalidOperationException("KMAC not initialized");

                var encOut = XofUtilities.RightEncode(0);

                this.cshake.BlockUpdate(encOut, 0, encOut.Length);

                this.firstOutput = false;
            }

            return this.cshake.DoOutput(output, outOff, outLen);
        }

        public int GetByteLength() { return this.cshake.GetByteLength(); }

        public int GetDigestSize() { return this.outputLength; }

        public int GetMacSize() { return this.outputLength; }

        public void Init(ICipherParameters parameters)
        {
            var kParam = (KeyParameter)parameters;
            this.key         = Arrays.Clone(kParam.GetKey());
            this.initialised = true;
            this.Reset();
        }

        public void Reset()
        {
            this.cshake.Reset();

            if (this.key != null)
            {
                if (this.bitLength == 128)
                    this.bytePad(this.key, 168);
                else
                    this.bytePad(this.key, 136);
            }

            this.firstOutput = true;
        }

        private void bytePad(byte[] X, int w)
        {
            var bytes = XofUtilities.LeftEncode(w);
            this.BlockUpdate(bytes, 0, bytes.Length);
            var encX = encode(X);
            this.BlockUpdate(encX, 0, encX.Length);

            var required = w - (bytes.Length + encX.Length) % w;

            if (required > 0 && required != w)
            {
                while (required > padding.Length)
                {
                    this.BlockUpdate(padding, 0, padding.Length);
                    required -= padding.Length;
                }

                this.BlockUpdate(padding, 0, required);
            }
        }

        private static byte[] encode(byte[] X) { return Arrays.Concatenate(XofUtilities.LeftEncode(X.Length * 8), X); }

        public void Update(byte input)
        {
            if (!this.initialised)
                throw new InvalidOperationException("KMAC not initialized");

            this.cshake.Update(input);
        }
    }
}
#pragma warning restore
#endif