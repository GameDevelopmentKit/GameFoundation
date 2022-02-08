#if !BESTHTTP_DISABLE_ALTERNATE_SSL && (!UNITY_WEBGL || UNITY_EDITOR)
#pragma warning disable
namespace BestHTTP.SecureProtocol.Org.BouncyCastle.Tls.Crypto.Impl.BC
{
    using BestHTTP.SecureProtocol.Org.BouncyCastle.Crypto;
    using BestHTTP.SecureProtocol.Org.BouncyCastle.Crypto.Modes;
    using BestHTTP.SecureProtocol.Org.BouncyCastle.Crypto.Parameters;

    internal sealed class BcTlsAeadCipherImpl
        : TlsAeadCipherImpl
    {
        private readonly bool             m_isEncrypting;
        private readonly IAeadBlockCipher m_cipher;

        private KeyParameter key;

        internal BcTlsAeadCipherImpl(IAeadBlockCipher cipher, bool isEncrypting)
        {
            this.m_cipher       = cipher;
            this.m_isEncrypting = isEncrypting;
        }

        public void SetKey(byte[] key, int keyOff, int keyLen) { this.key = new KeyParameter(key, keyOff, keyLen); }

        public void Init(byte[] nonce, int macSize, byte[] additionalData) { this.m_cipher.Init(this.m_isEncrypting, new AeadParameters(this.key, macSize * 8, nonce, additionalData)); }

        public int GetOutputSize(int inputLength) { return this.m_cipher.GetOutputSize(inputLength); }

        public int DoFinal(byte[] input, int inputOffset, int inputLength, byte[] output, int outputOffset)
        {
            var len = this.m_cipher.ProcessBytes(input, inputOffset, inputLength, output, outputOffset);

            try
            {
                len += this.m_cipher.DoFinal(output, outputOffset + len);
            }
            catch (InvalidCipherTextException e)
            {
                throw new TlsFatalAlert(AlertDescription.bad_record_mac, e);
            }

            return len;
        }
    }
}
#pragma warning restore
#endif