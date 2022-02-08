#if !BESTHTTP_DISABLE_ALTERNATE_SSL && (!UNITY_WEBGL || UNITY_EDITOR)
#pragma warning disable
namespace BestHTTP.SecureProtocol.Org.BouncyCastle.Tls.Crypto.Impl.BC
{
    using BestHTTP.SecureProtocol.Org.BouncyCastle.Crypto;
    using BestHTTP.SecureProtocol.Org.BouncyCastle.Crypto.Parameters;

    internal sealed class BcTlsBlockCipherImpl
        : TlsBlockCipherImpl
    {
        private readonly bool         m_isEncrypting;
        private readonly IBlockCipher m_cipher;

        private KeyParameter key;

        internal BcTlsBlockCipherImpl(IBlockCipher cipher, bool isEncrypting)
        {
            this.m_cipher       = cipher;
            this.m_isEncrypting = isEncrypting;
        }

        public void SetKey(byte[] key, int keyOff, int keyLen) { this.key = new KeyParameter(key, keyOff, keyLen); }

        public void Init(byte[] iv, int ivOff, int ivLen) { this.m_cipher.Init(this.m_isEncrypting, new ParametersWithIV(this.key, iv, ivOff, ivLen)); }

        public int DoFinal(byte[] input, int inputOffset, int inputLength, byte[] output, int outputOffset)
        {
            var blockSize = this.m_cipher.GetBlockSize();

            for (var i = 0; i < inputLength; i += blockSize) this.m_cipher.ProcessBlock(input, inputOffset + i, output, outputOffset + i);

            return inputLength;
        }

        public int GetBlockSize() { return this.m_cipher.GetBlockSize(); }
    }
}
#pragma warning restore
#endif