#if !BESTHTTP_DISABLE_ALTERNATE_SSL && (!UNITY_WEBGL || UNITY_EDITOR)
#pragma warning disable
namespace BestHTTP.SecureProtocol.Org.BouncyCastle.Tls.Crypto.Impl.BC
{
    using System;
    using BestHTTP.SecureProtocol.Org.BouncyCastle.Crypto.Engines;
    using BestHTTP.SecureProtocol.Org.BouncyCastle.Crypto.Macs;
    using BestHTTP.SecureProtocol.Org.BouncyCastle.Crypto.Parameters;
    using BestHTTP.SecureProtocol.Org.BouncyCastle.Crypto.Utilities;

    public sealed class BcChaCha20Poly1305
        : TlsAeadCipherImpl
    {
        private static readonly byte[] Zeroes = new byte[15];

        private readonly ChaCha7539Engine m_cipher = new();
        private readonly Poly1305         m_mac    = new();

        private readonly bool m_isEncrypting;

        private int m_additionalDataLength;

        public BcChaCha20Poly1305(bool isEncrypting) { this.m_isEncrypting = isEncrypting; }

        public int DoFinal(byte[] input, int inputOffset, int inputLength, byte[] output, int outputOffset)
        {
            if (this.m_isEncrypting)
            {
                var ciphertextLength = inputLength;

                this.m_cipher.ProcessBytes(input, inputOffset, inputLength, output, outputOffset);
                var outputLength = inputLength;

                if (ciphertextLength != outputLength)
                    throw new InvalidOperationException();

                this.UpdateMac(output, outputOffset, ciphertextLength);

                var lengths = new byte[16];
                Pack.UInt64_To_LE((ulong)this.m_additionalDataLength, lengths, 0);
                Pack.UInt64_To_LE((ulong)ciphertextLength, lengths, 8);
                this.m_mac.BlockUpdate(lengths, 0, 16);

                this.m_mac.DoFinal(output, outputOffset + ciphertextLength);

                return ciphertextLength + 16;
            }
            else
            {
                var ciphertextLength = inputLength - 16;

                this.UpdateMac(input, inputOffset, ciphertextLength);

                var expectedMac = new byte[16];
                Pack.UInt64_To_LE((ulong)this.m_additionalDataLength, expectedMac, 0);
                Pack.UInt64_To_LE((ulong)ciphertextLength, expectedMac, 8);
                this.m_mac.BlockUpdate(expectedMac, 0, 16);
                this.m_mac.DoFinal(expectedMac, 0);

                var badMac = !TlsUtilities.ConstantTimeAreEqual(16, expectedMac, 0, input, inputOffset + ciphertextLength);
                if (badMac)
                    throw new TlsFatalAlert(AlertDescription.bad_record_mac);

                this.m_cipher.ProcessBytes(input, inputOffset, ciphertextLength, output, outputOffset);
                var outputLength = ciphertextLength;

                if (ciphertextLength != outputLength)
                    throw new InvalidOperationException();

                return ciphertextLength;
            }
        }

        public int GetOutputSize(int inputLength) { return this.m_isEncrypting ? inputLength + 16 : inputLength - 16; }

        public void Init(byte[] nonce, int macSize, byte[] additionalData)
        {
            if (nonce == null || nonce.Length != 12 || macSize != 16)
                throw new TlsFatalAlert(AlertDescription.internal_error);

            this.m_cipher.Init(this.m_isEncrypting, new ParametersWithIV(null, nonce));
            this.InitMac();
            if (additionalData == null)
            {
                this.m_additionalDataLength = 0;
            }
            else
            {
                this.m_additionalDataLength = additionalData.Length;
                this.UpdateMac(additionalData, 0, additionalData.Length);
            }
        }

        public void SetKey(byte[] key, int keyOff, int keyLen)
        {
            var cipherKey = new KeyParameter(key, keyOff, keyLen);
            this.m_cipher.Init(this.m_isEncrypting, new ParametersWithIV(cipherKey, Zeroes, 0, 12));
        }

        private void InitMac()
        {
            var firstBlock = new byte[64];
            this.m_cipher.ProcessBytes(firstBlock, 0, 64, firstBlock, 0);
            this.m_mac.Init(new KeyParameter(firstBlock, 0, 32));
            Array.Clear(firstBlock, 0, firstBlock.Length);
        }

        private void UpdateMac(byte[] buf, int off, int len)
        {
            this.m_mac.BlockUpdate(buf, off, len);

            var partial = len % 16;
            if (partial != 0) this.m_mac.BlockUpdate(Zeroes, 0, 16 - partial);
        }
    }
}
#pragma warning restore
#endif