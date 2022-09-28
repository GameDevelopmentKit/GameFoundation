#if !BESTHTTP_DISABLE_ALTERNATE_SSL && (!UNITY_WEBGL || UNITY_EDITOR)
#pragma warning disable
namespace BestHTTP.SecureProtocol.Org.BouncyCastle.Tls.Crypto.Impl.BC
{
    using BestHTTP.SecureProtocol.Org.BouncyCastle.Crypto;
    using BestHTTP.SecureProtocol.Org.BouncyCastle.Utilities;

    /// <summary>HMAC implementation based on original internet draft for HMAC (RFC 2104).</summary>
    /// <remarks>
    ///     The difference is that padding is concatenated versus XORed with the key, e.g:
    ///     <code>H(K + opad, H(K + ipad, text))</code>
    /// </remarks>
    internal class BcSsl3Hmac
        : TlsHmac
    {
        private const byte IPAD_BYTE = 0x36;
        private const byte OPAD_BYTE = 0x5C;

        private static readonly byte[] IPAD = GenPad(IPAD_BYTE, 48);
        private static readonly byte[] OPAD = GenPad(OPAD_BYTE, 48);

        private readonly IDigest m_digest;
        private readonly int     m_padLength;

        private byte[] m_secret;

        /// <summary>
        ///     Base constructor for one of the standard digest algorithms for which the byteLength is known.
        /// </summary>
        /// <remarks>
        ///     Behaviour is undefined for digests other than MD5 or SHA1.
        /// </remarks>
        /// <param name="digest">the digest.</param>
        internal BcSsl3Hmac(IDigest digest)
        {
            this.m_digest = digest;

            if (digest.GetDigestSize() == 20)
                this.m_padLength = 40;
            else
                this.m_padLength = 48;
        }

        public virtual void SetKey(byte[] key, int keyOff, int keyLen)
        {
            this.m_secret = TlsUtilities.CopyOfRangeExact(key, keyOff, keyOff + keyLen);

            this.Reset();
        }

        public virtual void Update(byte[] input, int inOff, int len) { this.m_digest.BlockUpdate(input, inOff, len); }

        public virtual byte[] CalculateMac()
        {
            var result = new byte[this.m_digest.GetDigestSize()];
            this.DoFinal(result, 0);
            return result;
        }

        public virtual void CalculateMac(byte[] output, int outOff) { this.DoFinal(output, outOff); }

        public virtual int InternalBlockSize => this.m_digest.GetByteLength();

        public virtual int MacLength => this.m_digest.GetDigestSize();

        /**
         * Reset the mac generator.
         */
        public virtual void Reset()
        {
            this.m_digest.Reset();
            this.m_digest.BlockUpdate(this.m_secret, 0, this.m_secret.Length);
            this.m_digest.BlockUpdate(IPAD, 0, this.m_padLength);
        }

        private void DoFinal(byte[] output, int outOff)
        {
            var tmp = new byte[this.m_digest.GetDigestSize()];
            this.m_digest.DoFinal(tmp, 0);

            this.m_digest.BlockUpdate(this.m_secret, 0, this.m_secret.Length);
            this.m_digest.BlockUpdate(OPAD, 0, this.m_padLength);
            this.m_digest.BlockUpdate(tmp, 0, tmp.Length);

            this.m_digest.DoFinal(output, outOff);

            this.Reset();
        }

        private static byte[] GenPad(byte b, int count)
        {
            var padding = new byte[count];
            Arrays.Fill(padding, b);
            return padding;
        }
    }
}
#pragma warning restore
#endif