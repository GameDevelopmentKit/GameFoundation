#if !BESTHTTP_DISABLE_ALTERNATE_SSL && (!UNITY_WEBGL || UNITY_EDITOR)
#pragma warning disable
namespace BestHTTP.SecureProtocol.Org.BouncyCastle.Crypto.Digests
{
    using BestHTTP.SecureProtocol.Org.BouncyCastle.Utilities;

    /// <summary>
    ///     Customizable SHAKE function.
    /// </summary>
    public class CShakeDigest
        : ShakeDigest
    {
        private static readonly byte[] padding = new byte[100];

        private static byte[] EncodeString(byte[] str)
        {
            if (Arrays.IsNullOrEmpty(str)) return XofUtilities.LeftEncode(0L);

            return Arrays.Concatenate(XofUtilities.LeftEncode(str.Length * 8L), str);
        }

        private readonly byte[] diff;

        /// <summary>
        ///     Base constructor
        /// </summary>
        /// <param name="bitLength">bit length of the underlying SHAKE function, 128 or 256.</param>
        /// <param name="N">the function name string, note this is reserved for use by NIST. Avoid using it if not required.</param>
        /// <param name="S">the customization string - available for local use.</param>
        public CShakeDigest(int bitLength, byte[] N, byte[] S)
            : base(bitLength)
        {
            if ((N == null || N.Length == 0) && (S == null || S.Length == 0))
            {
                this.diff = null;
            }
            else
            {
                this.diff = Arrays.ConcatenateAll(XofUtilities.LeftEncode(this.rate / 8), EncodeString(N), EncodeString(S));
                this.DiffPadAndAbsorb();
            }
        }

        public CShakeDigest(CShakeDigest source)
            : base(source)
        {
            this.diff = Arrays.Clone(source.diff);
        }

        // bytepad in SP 800-185
        private void DiffPadAndAbsorb()
        {
            var blockSize = this.rate / 8;
            this.Absorb(this.diff, 0, this.diff.Length);

            var delta = this.diff.Length % blockSize;

            // only add padding if needed
            if (delta != 0)
            {
                var required = blockSize - delta;

                while (required > padding.Length)
                {
                    this.Absorb(padding, 0, padding.Length);
                    required -= padding.Length;
                }

                this.Absorb(padding, 0, required);
            }
        }

        public override string AlgorithmName => "CSHAKE" + this.fixedOutputLength;

        public override int DoOutput(byte[] output, int outOff, int outLen)
        {
            if (this.diff == null) return base.DoOutput(output, outOff, outLen);

            if (!this.squeezing) this.AbsorbBits(0x00, 2);

            this.Squeeze(output, outOff, (long)outLen << 3);

            return outLen;
        }

        public override void Reset()
        {
            base.Reset();

            if (this.diff != null) this.DiffPadAndAbsorb();
        }
    }
}
#pragma warning restore
#endif