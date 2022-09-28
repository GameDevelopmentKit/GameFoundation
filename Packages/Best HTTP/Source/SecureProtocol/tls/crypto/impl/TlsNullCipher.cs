#if !BESTHTTP_DISABLE_ALTERNATE_SSL && (!UNITY_WEBGL || UNITY_EDITOR)
#pragma warning disable
namespace BestHTTP.SecureProtocol.Org.BouncyCastle.Tls.Crypto.Impl
{
    using System;
    using System.IO;

    /// <summary>The NULL cipher.</summary>
    public class TlsNullCipher
        : TlsCipher
    {
        protected readonly TlsCryptoParameters m_cryptoParams;
        protected readonly TlsSuiteHmac        m_readMac, m_writeMac;

        /// <exception cref="IOException" />
        public TlsNullCipher(TlsCryptoParameters cryptoParams, TlsHmac clientMac, TlsHmac serverMac)
        {
            if (TlsImplUtilities.IsTlsV13(cryptoParams))
                throw new TlsFatalAlert(AlertDescription.internal_error);

            this.m_cryptoParams = cryptoParams;

            var key_block_size = clientMac.MacLength + serverMac.MacLength;
            var key_block      = TlsImplUtilities.CalculateKeyBlock(cryptoParams, key_block_size);

            var offset = 0;

            clientMac.SetKey(key_block, offset, clientMac.MacLength);
            offset += clientMac.MacLength;
            serverMac.SetKey(key_block, offset, serverMac.MacLength);
            offset += serverMac.MacLength;

            if (offset != key_block_size)
                throw new TlsFatalAlert(AlertDescription.internal_error);

            if (cryptoParams.IsServer)
            {
                this.m_writeMac = new TlsSuiteHmac(cryptoParams, serverMac);
                this.m_readMac  = new TlsSuiteHmac(cryptoParams, clientMac);
            }
            else
            {
                this.m_writeMac = new TlsSuiteHmac(cryptoParams, clientMac);
                this.m_readMac  = new TlsSuiteHmac(cryptoParams, serverMac);
            }
        }

        public virtual int GetCiphertextDecodeLimit(int plaintextLimit) { return plaintextLimit + this.m_writeMac.Size; }

        public virtual int GetCiphertextEncodeLimit(int plaintextLength, int plaintextLimit) { return plaintextLength + this.m_writeMac.Size; }

        public virtual int GetPlaintextLimit(int ciphertextLimit) { return ciphertextLimit - this.m_writeMac.Size; }

        public virtual TlsEncodeResult EncodePlaintext(long seqNo, short contentType, ProtocolVersion recordVersion,
            int headerAllocation, byte[] plaintext, int offset, int len)
        {
            var mac        = this.m_writeMac.CalculateMac(seqNo, contentType, plaintext, offset, len);
            var ciphertext = new byte[headerAllocation + len + mac.Length];
            Array.Copy(plaintext, offset, ciphertext, headerAllocation, len);
            Array.Copy(mac, 0, ciphertext, headerAllocation + len, mac.Length);
            return new TlsEncodeResult(ciphertext, 0, ciphertext.Length, contentType);
        }

        public virtual TlsDecodeResult DecodeCiphertext(long seqNo, short recordType, ProtocolVersion recordVersion,
            byte[] ciphertext, int offset, int len)
        {
            var macSize = this.m_readMac.Size;
            if (len < macSize)
                throw new TlsFatalAlert(AlertDescription.decode_error);

            var macInputLen = len - macSize;

            var expectedMac = this.m_readMac.CalculateMac(seqNo, recordType, ciphertext, offset, macInputLen);

            var badMac = !TlsUtilities.ConstantTimeAreEqual(macSize, expectedMac, 0, ciphertext, offset + macInputLen);
            if (badMac)
                throw new TlsFatalAlert(AlertDescription.bad_record_mac);

            return new TlsDecodeResult(ciphertext, offset, macInputLen, recordType);
        }

        public virtual void RekeyDecoder() { throw new TlsFatalAlert(AlertDescription.internal_error); }

        public virtual void RekeyEncoder() { throw new TlsFatalAlert(AlertDescription.internal_error); }

        public virtual bool UsesOpaqueRecordType => false;
    }
}
#pragma warning restore
#endif