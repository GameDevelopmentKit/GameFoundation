#if !BESTHTTP_DISABLE_ALTERNATE_SSL && (!UNITY_WEBGL || UNITY_EDITOR)
#pragma warning disable
namespace BestHTTP.SecureProtocol.Org.BouncyCastle.Tls.Crypto.Impl
{
    using System;
    using System.IO;

    /// <summary>A generic TLS 1.2 AEAD cipher.</summary>
    public class TlsAeadCipher
        : TlsCipher
    {
        public const int AEAD_CCM               = 1;
        public const int AEAD_CHACHA20_POLY1305 = 2;
        public const int AEAD_GCM               = 3;

        private const int NONCE_RFC5288 = 1;
        private const int NONCE_RFC7905 = 2;

        protected readonly TlsCryptoParameters m_cryptoParams;
        protected readonly int                 m_keySize;
        protected readonly int                 m_macSize;
        protected readonly int                 m_fixed_iv_length;
        protected readonly int                 m_record_iv_length;

        protected readonly TlsAeadCipherImpl m_decryptCipher, m_encryptCipher;
        protected readonly byte[]            m_decryptNonce,  m_encryptNonce;

        protected readonly bool m_isTlsV13;
        protected readonly int  m_nonceMode;

        /// <exception cref="IOException" />
        public TlsAeadCipher(TlsCryptoParameters cryptoParams, TlsAeadCipherImpl encryptCipher,
            TlsAeadCipherImpl decryptCipher, int keySize, int macSize, int aeadType)
        {
            var securityParameters = cryptoParams.SecurityParameters;
            var negotiatedVersion  = securityParameters.NegotiatedVersion;

            if (!TlsImplUtilities.IsTlsV12(negotiatedVersion))
                throw new TlsFatalAlert(AlertDescription.internal_error);

            this.m_isTlsV13  = TlsImplUtilities.IsTlsV13(negotiatedVersion);
            this.m_nonceMode = GetNonceMode(this.m_isTlsV13, aeadType);

            switch (this.m_nonceMode)
            {
                case NONCE_RFC5288:
                    this.m_fixed_iv_length  = 4;
                    this.m_record_iv_length = 8;
                    break;
                case NONCE_RFC7905:
                    this.m_fixed_iv_length  = 12;
                    this.m_record_iv_length = 0;
                    break;
                default:
                    throw new TlsFatalAlert(AlertDescription.internal_error);
            }

            this.m_cryptoParams = cryptoParams;
            this.m_keySize      = keySize;
            this.m_macSize      = macSize;

            this.m_decryptCipher = decryptCipher;
            this.m_encryptCipher = encryptCipher;

            this.m_decryptNonce = new byte[this.m_fixed_iv_length];
            this.m_encryptNonce = new byte[this.m_fixed_iv_length];

            var isServer = cryptoParams.IsServer;
            if (this.m_isTlsV13)
            {
                this.RekeyCipher(securityParameters, decryptCipher, this.m_decryptNonce, !isServer);
                this.RekeyCipher(securityParameters, encryptCipher, this.m_encryptNonce, isServer);
                return;
            }

            var keyBlockSize = 2 * keySize + 2 * this.m_fixed_iv_length;
            var keyBlock     = TlsImplUtilities.CalculateKeyBlock(cryptoParams, keyBlockSize);
            var pos          = 0;

            if (isServer)
            {
                decryptCipher.SetKey(keyBlock, pos, keySize);
                pos += keySize;
                encryptCipher.SetKey(keyBlock, pos, keySize);
                pos += keySize;

                Array.Copy(keyBlock, pos, this.m_decryptNonce, 0, this.m_fixed_iv_length);
                pos += this.m_fixed_iv_length;
                Array.Copy(keyBlock, pos, this.m_encryptNonce, 0, this.m_fixed_iv_length);
                pos += this.m_fixed_iv_length;
            }
            else
            {
                encryptCipher.SetKey(keyBlock, pos, keySize);
                pos += keySize;
                decryptCipher.SetKey(keyBlock, pos, keySize);
                pos += keySize;

                Array.Copy(keyBlock, pos, this.m_encryptNonce, 0, this.m_fixed_iv_length);
                pos += this.m_fixed_iv_length;
                Array.Copy(keyBlock, pos, this.m_decryptNonce, 0, this.m_fixed_iv_length);
                pos += this.m_fixed_iv_length;
            }

            if (keyBlockSize != pos)
                throw new TlsFatalAlert(AlertDescription.internal_error);

            var nonceLength = this.m_fixed_iv_length + this.m_record_iv_length;

            // NOTE: Ensure dummy nonce is not part of the generated sequence(s)
            var dummyNonce = new byte[nonceLength];
            dummyNonce[0] = (byte)~this.m_encryptNonce[0];
            dummyNonce[1] = (byte)~this.m_decryptNonce[1];

            encryptCipher.Init(dummyNonce, macSize, null);
            decryptCipher.Init(dummyNonce, macSize, null);
        }

        public virtual int GetCiphertextDecodeLimit(int plaintextLimit) { return plaintextLimit + this.m_macSize + this.m_record_iv_length + (this.m_isTlsV13 ? 1 : 0); }

        public virtual int GetCiphertextEncodeLimit(int plaintextLength, int plaintextLimit)
        {
            var innerPlaintextLimit = plaintextLength;
            if (this.m_isTlsV13)
            {
                // TODO[tls13] Add support for padding
                var maxPadding = 0;

                innerPlaintextLimit = 1 + Math.Min(plaintextLimit, plaintextLength + maxPadding);
            }

            return innerPlaintextLimit + this.m_macSize + this.m_record_iv_length;
        }

        public virtual int GetPlaintextLimit(int ciphertextLimit) { return ciphertextLimit - this.m_macSize - this.m_record_iv_length - (this.m_isTlsV13 ? 1 : 0); }

        public virtual TlsEncodeResult EncodePlaintext(long seqNo, short contentType, ProtocolVersion recordVersion,
            int headerAllocation, byte[] plaintext, int plaintextOffset, int plaintextLength)
        {
            var nonce = new byte[this.m_encryptNonce.Length + this.m_record_iv_length];

            switch (this.m_nonceMode)
            {
                case NONCE_RFC5288:
                    Array.Copy(this.m_encryptNonce, 0, nonce, 0, this.m_encryptNonce.Length);
                    // RFC 5288/6655: The nonce_explicit MAY be the 64-bit sequence number.
                    TlsUtilities.WriteUint64(seqNo, nonce, this.m_encryptNonce.Length);
                    break;
                case NONCE_RFC7905:
                    TlsUtilities.WriteUint64(seqNo, nonce, nonce.Length - 8);
                    for (var i = 0; i < this.m_encryptNonce.Length; ++i) nonce[i] ^= this.m_encryptNonce[i];
                    break;
                default:
                    throw new TlsFatalAlert(AlertDescription.internal_error);
            }

            var extraLength = this.m_isTlsV13 ? 1 : 0;

            // TODO[tls13] If we support adding padding to TLSInnerPlaintext, this will need review
            var encryptionLength = this.m_encryptCipher.GetOutputSize(plaintextLength + extraLength);
            var ciphertextLength = this.m_record_iv_length + encryptionLength;

            var output    = new byte[headerAllocation + ciphertextLength];
            var outputPos = headerAllocation;

            if (this.m_record_iv_length != 0)
            {
                Array.Copy(nonce, nonce.Length - this.m_record_iv_length, output, outputPos, this.m_record_iv_length);
                outputPos += this.m_record_iv_length;
            }

            var recordType = this.m_isTlsV13 ? ContentType.application_data : contentType;

            var additionalData = this.GetAdditionalData(seqNo, recordType, recordVersion, ciphertextLength,
                plaintextLength);

            try
            {
                this.m_encryptCipher.Init(nonce, this.m_macSize, additionalData);

                Array.Copy(plaintext, plaintextOffset, output, outputPos, plaintextLength);
                if (this.m_isTlsV13) output[outputPos + plaintextLength] = (byte)contentType;

                outputPos += this.m_encryptCipher.DoFinal(output, outputPos, plaintextLength + extraLength, output,
                    outputPos);
            }
            catch (IOException e)
            {
                throw e;
            }
            catch (Exception e)
            {
                throw new TlsFatalAlert(AlertDescription.internal_error, e);
            }

            if (outputPos != output.Length)
                // NOTE: The additional data mechanism for AEAD ciphers requires exact output size prediction.
                throw new TlsFatalAlert(AlertDescription.internal_error);

            return new TlsEncodeResult(output, 0, output.Length, recordType);
        }

        public virtual TlsDecodeResult DecodeCiphertext(long seqNo, short recordType, ProtocolVersion recordVersion,
            byte[] ciphertext, int ciphertextOffset, int ciphertextLength)
        {
            if (this.GetPlaintextLimit(ciphertextLength) < 0)
                throw new TlsFatalAlert(AlertDescription.decode_error);

            var nonce = new byte[this.m_decryptNonce.Length + this.m_record_iv_length];

            switch (this.m_nonceMode)
            {
                case NONCE_RFC5288:
                    Array.Copy(this.m_decryptNonce, 0, nonce, 0, this.m_decryptNonce.Length);
                    Array.Copy(ciphertext, ciphertextOffset, nonce, nonce.Length - this.m_record_iv_length, this.m_record_iv_length);
                    break;
                case NONCE_RFC7905:
                    TlsUtilities.WriteUint64(seqNo, nonce, nonce.Length - 8);
                    for (var i = 0; i < this.m_decryptNonce.Length; ++i) nonce[i] ^= this.m_decryptNonce[i];
                    break;
                default:
                    throw new TlsFatalAlert(AlertDescription.internal_error);
            }

            var encryptionOffset = ciphertextOffset + this.m_record_iv_length;
            var encryptionLength = ciphertextLength - this.m_record_iv_length;
            var plaintextLength  = this.m_decryptCipher.GetOutputSize(encryptionLength);

            var additionalData = this.GetAdditionalData(seqNo, recordType, recordVersion, ciphertextLength,
                plaintextLength);

            int outputPos;
            try
            {
                this.m_decryptCipher.Init(nonce, this.m_macSize, additionalData);
                outputPos = this.m_decryptCipher.DoFinal(ciphertext, encryptionOffset, encryptionLength, ciphertext,
                    encryptionOffset);
            }
            catch (IOException e)
            {
                throw e;
            }
            catch (Exception e)
            {
                throw new TlsFatalAlert(AlertDescription.bad_record_mac, e);
            }

            if (outputPos != plaintextLength)
                // NOTE: The additional data mechanism for AEAD ciphers requires exact output size prediction.
                throw new TlsFatalAlert(AlertDescription.internal_error);

            var contentType = recordType;
            if (this.m_isTlsV13)
            {
                // Strip padding and read true content type from TLSInnerPlaintext
                var pos = plaintextLength;
                for (;;)
                {
                    if (--pos < 0)
                        throw new TlsFatalAlert(AlertDescription.unexpected_message);

                    var octet = ciphertext[encryptionOffset + pos];
                    if (0 != octet)
                    {
                        contentType     = (short)(octet & 0xFF);
                        plaintextLength = pos;
                        break;
                    }
                }
            }

            return new TlsDecodeResult(ciphertext, encryptionOffset, plaintextLength, contentType);
        }

        public virtual void RekeyDecoder() { this.RekeyCipher(this.m_cryptoParams.SecurityParameters, this.m_decryptCipher, this.m_decryptNonce, !this.m_cryptoParams.IsServer); }

        public virtual void RekeyEncoder() { this.RekeyCipher(this.m_cryptoParams.SecurityParameters, this.m_encryptCipher, this.m_encryptNonce, this.m_cryptoParams.IsServer); }

        public virtual bool UsesOpaqueRecordType => this.m_isTlsV13;

        protected virtual byte[] GetAdditionalData(long seqNo, short recordType, ProtocolVersion recordVersion,
            int ciphertextLength, int plaintextLength)
        {
            if (this.m_isTlsV13)
            {
                /*
                 * TLSCiphertext.opaque_type || TLSCiphertext.legacy_record_version || TLSCiphertext.length
                 */
                var additional_data = new byte[5];
                TlsUtilities.WriteUint8(recordType, additional_data, 0);
                TlsUtilities.WriteVersion(recordVersion, additional_data, 1);
                TlsUtilities.WriteUint16(ciphertextLength, additional_data, 3);
                return additional_data;
            }
            else
            {
                /*
                 * seq_num + TLSCompressed.type + TLSCompressed.version + TLSCompressed.length
                 */
                var additional_data = new byte[13];
                TlsUtilities.WriteUint64(seqNo, additional_data, 0);
                TlsUtilities.WriteUint8(recordType, additional_data, 8);
                TlsUtilities.WriteVersion(recordVersion, additional_data, 9);
                TlsUtilities.WriteUint16(plaintextLength, additional_data, 11);
                return additional_data;
            }
        }

        protected virtual void RekeyCipher(SecurityParameters securityParameters, TlsAeadCipherImpl cipher,
            byte[] nonce, bool serverSecret)
        {
            if (!this.m_isTlsV13)
                throw new TlsFatalAlert(AlertDescription.internal_error);

            var secret = serverSecret
                ? securityParameters.TrafficSecretServer
                : securityParameters.TrafficSecretClient;

            // TODO[tls13] For early data, have to disable server->client
            if (null == secret)
                throw new TlsFatalAlert(AlertDescription.internal_error);

            this.Setup13Cipher(cipher, nonce, secret, securityParameters.PrfCryptoHashAlgorithm);
        }

        protected virtual void Setup13Cipher(TlsAeadCipherImpl cipher, byte[] nonce, TlsSecret secret,
            int cryptoHashAlgorithm)
        {
            var key = TlsCryptoUtilities.HkdfExpandLabel(secret, cryptoHashAlgorithm, "key",
                TlsUtilities.EmptyBytes, this.m_keySize).Extract();
            var iv = TlsCryptoUtilities.HkdfExpandLabel(secret, cryptoHashAlgorithm, "iv", TlsUtilities.EmptyBytes, this.m_fixed_iv_length).Extract();

            cipher.SetKey(key, 0, this.m_keySize);
            Array.Copy(iv, 0, nonce, 0, this.m_fixed_iv_length);

            // NOTE: Ensure dummy nonce is not part of the generated sequence(s)
            iv[0] ^= 0x80;
            cipher.Init(iv, this.m_macSize, null);
        }

        private static int GetNonceMode(bool isTLSv13, int aeadType)
        {
            switch (aeadType)
            {
                case AEAD_CCM:
                case AEAD_GCM:
                    return isTLSv13 ? NONCE_RFC7905 : NONCE_RFC5288;

                case AEAD_CHACHA20_POLY1305:
                    return NONCE_RFC7905;

                default:
                    throw new TlsFatalAlert(AlertDescription.internal_error);
            }
        }
    }
}
#pragma warning restore
#endif