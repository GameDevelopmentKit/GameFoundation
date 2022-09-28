#if !BESTHTTP_DISABLE_ALTERNATE_SSL && (!UNITY_WEBGL || UNITY_EDITOR)
#pragma warning disable
namespace BestHTTP.SecureProtocol.Org.BouncyCastle.Tls.Crypto.Impl
{
    using System;
    using System.IO;
    using BestHTTP.SecureProtocol.Org.BouncyCastle.Crypto.Utilities;
    using BestHTTP.SecureProtocol.Org.BouncyCastle.Utilities;

    /// <summary>A generic TLS 1.0-1.2 block cipher. This can be used for AES or 3DES for example.</summary>
    public class TlsBlockCipher
        : TlsCipher
    {
        protected readonly TlsCryptoParameters m_cryptoParams;
        protected readonly byte[]              m_randomData;
        protected readonly bool                m_encryptThenMac;
        protected readonly bool                m_useExplicitIV;
        protected readonly bool                m_acceptExtraPadding;
        protected readonly bool                m_useExtraPadding;

        protected readonly TlsBlockCipherImpl m_decryptCipher, m_encryptCipher;
        protected readonly TlsSuiteMac        m_readMac,       m_writeMac;

        /// <exception cref="IOException" />
        public TlsBlockCipher(TlsCryptoParameters cryptoParams, TlsBlockCipherImpl encryptCipher,
            TlsBlockCipherImpl decryptCipher, TlsHmac clientMac, TlsHmac serverMac, int cipherKeySize)
        {
            var securityParameters = cryptoParams.SecurityParameters;
            var negotiatedVersion  = securityParameters.NegotiatedVersion;

            if (TlsImplUtilities.IsTlsV13(negotiatedVersion))
                throw new TlsFatalAlert(AlertDescription.internal_error);

            this.m_cryptoParams = cryptoParams;
            this.m_randomData   = cryptoParams.NonceGenerator.GenerateNonce(256);

            this.m_encryptThenMac = securityParameters.IsEncryptThenMac;
            this.m_useExplicitIV  = TlsImplUtilities.IsTlsV11(negotiatedVersion);

            this.m_acceptExtraPadding = !negotiatedVersion.IsSsl;

            /*
             * Don't use variable-length padding with truncated MACs.
             * 
             * See "Tag Size Does Matter: Attacks and Proofs for the TLS Record Protocol", Paterson,
             * Ristenpart, Shrimpton.
             *
             * TODO[DTLS] Consider supporting in DTLS (without exceeding send limit though)
             */
            this.m_useExtraPadding = securityParameters.IsExtendedPadding
                                     && ProtocolVersion.TLSv10.IsEqualOrEarlierVersionOf(negotiatedVersion)
                                     && (this.m_encryptThenMac || !securityParameters.IsTruncatedHmac);

            this.m_encryptCipher = encryptCipher;
            this.m_decryptCipher = decryptCipher;

            TlsBlockCipherImpl clientCipher, serverCipher;
            if (cryptoParams.IsServer)
            {
                clientCipher = decryptCipher;
                serverCipher = encryptCipher;
            }
            else
            {
                clientCipher = encryptCipher;
                serverCipher = decryptCipher;
            }

            var key_block_size = 2 * cipherKeySize + clientMac.MacLength + serverMac.MacLength;

            // From TLS 1.1 onwards, block ciphers don't need IVs from the key_block
            if (!this.m_useExplicitIV) key_block_size += clientCipher.GetBlockSize() + serverCipher.GetBlockSize();

            var key_block = TlsImplUtilities.CalculateKeyBlock(cryptoParams, key_block_size);

            var offset = 0;

            clientMac.SetKey(key_block, offset, clientMac.MacLength);
            offset += clientMac.MacLength;
            serverMac.SetKey(key_block, offset, serverMac.MacLength);
            offset += serverMac.MacLength;

            clientCipher.SetKey(key_block, offset, cipherKeySize);
            offset += cipherKeySize;
            serverCipher.SetKey(key_block, offset, cipherKeySize);
            offset += cipherKeySize;

            var clientIVLength = clientCipher.GetBlockSize();
            var serverIVLength = serverCipher.GetBlockSize();

            if (this.m_useExplicitIV)
            {
                clientCipher.Init(new byte[clientIVLength], 0, clientIVLength);
                serverCipher.Init(new byte[serverIVLength], 0, serverIVLength);
            }
            else
            {
                clientCipher.Init(key_block, offset, clientIVLength);
                offset += clientIVLength;
                serverCipher.Init(key_block, offset, serverIVLength);
                offset += serverIVLength;
            }

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

        public virtual int GetCiphertextDecodeLimit(int plaintextLimit)
        {
            var blockSize  = this.m_decryptCipher.GetBlockSize();
            var macSize    = this.m_readMac.Size;
            var maxPadding = 256;

            return this.GetCiphertextLength(blockSize, macSize, maxPadding, plaintextLimit);
        }

        public virtual int GetCiphertextEncodeLimit(int plaintextLength, int plaintextLimit)
        {
            var blockSize  = this.m_encryptCipher.GetBlockSize();
            var macSize    = this.m_writeMac.Size;
            var maxPadding = this.m_useExtraPadding ? 256 : blockSize;

            return this.GetCiphertextLength(blockSize, macSize, maxPadding, plaintextLength);
        }

        public virtual int GetPlaintextLimit(int ciphertextLimit)
        {
            var blockSize = this.m_encryptCipher.GetBlockSize();
            var macSize   = this.m_writeMac.Size;

            var plaintextLimit = ciphertextLimit;

            // Leave room for the MAC, and require block-alignment
            if (this.m_encryptThenMac)
            {
                plaintextLimit -= macSize;
                plaintextLimit -= plaintextLimit % blockSize;
            }
            else
            {
                plaintextLimit -= plaintextLimit % blockSize;
                plaintextLimit -= macSize;
            }

            // Minimum 1 byte of padding
            --plaintextLimit;

            // An explicit IV consumes 1 block
            if (this.m_useExplicitIV) plaintextLimit -= blockSize;

            return plaintextLimit;
        }

        public virtual TlsEncodeResult EncodePlaintext(long seqNo, short contentType, ProtocolVersion recordVersion,
            int headerAllocation, byte[] plaintext, int offset, int len)
        {
            var blockSize = this.m_encryptCipher.GetBlockSize();
            var macSize   = this.m_writeMac.Size;

            var enc_input_length                         = len;
            if (!this.m_encryptThenMac) enc_input_length += macSize;

            var padding_length = blockSize - enc_input_length % blockSize;
            if (this.m_useExtraPadding)
            {
                // Add a random number of extra blocks worth of padding
                var maxExtraPadBlocks    = (256 - padding_length) / blockSize;
                var actualExtraPadBlocks = this.ChooseExtraPadBlocks(maxExtraPadBlocks);
                padding_length += actualExtraPadBlocks * blockSize;
            }

            var totalSize                       = len + macSize + padding_length;
            if (this.m_useExplicitIV) totalSize += blockSize;

            var outBuf = new byte[headerAllocation + totalSize];
            var outOff = headerAllocation;

            if (this.m_useExplicitIV)
            {
                // Technically the explicit IV will be the encryption of this nonce
                var explicitIV = this.m_cryptoParams.NonceGenerator.GenerateNonce(blockSize);
                Array.Copy(explicitIV, 0, outBuf, outOff, blockSize);
                outOff += blockSize;
            }

            Array.Copy(plaintext, offset, outBuf, outOff, len);
            outOff += len;

            if (!this.m_encryptThenMac)
            {
                var mac = this.m_writeMac.CalculateMac(seqNo, contentType, plaintext, offset, len);
                Array.Copy(mac, 0, outBuf, outOff, mac.Length);
                outOff += mac.Length;
            }

            var padByte                                               = (byte)(padding_length - 1);
            for (var i = 0; i < padding_length; ++i) outBuf[outOff++] = padByte;

            this.m_encryptCipher.DoFinal(outBuf, headerAllocation, outOff - headerAllocation, outBuf, headerAllocation);

            if (this.m_encryptThenMac)
            {
                var mac = this.m_writeMac.CalculateMac(seqNo, contentType, outBuf, headerAllocation,
                    outOff - headerAllocation);
                Array.Copy(mac, 0, outBuf, outOff, mac.Length);
                outOff += mac.Length;
            }

            if (outOff != outBuf.Length)
                throw new TlsFatalAlert(AlertDescription.internal_error);

            return new TlsEncodeResult(outBuf, 0, outBuf.Length, contentType);
        }

        public virtual TlsDecodeResult DecodeCiphertext(long seqNo, short recordType, ProtocolVersion recordVersion,
            byte[] ciphertext, int offset, int len)
        {
            var blockSize = this.m_decryptCipher.GetBlockSize();
            var macSize   = this.m_readMac.Size;

            var minLen = blockSize;
            if (this.m_encryptThenMac)
                minLen += macSize;
            else
                minLen = Math.Max(minLen, macSize + 1);

            if (this.m_useExplicitIV) minLen += blockSize;

            if (len < minLen)
                throw new TlsFatalAlert(AlertDescription.decode_error);

            var blocks_length                        = len;
            if (this.m_encryptThenMac) blocks_length -= macSize;

            if (blocks_length % blockSize != 0)
                throw new TlsFatalAlert(AlertDescription.decryption_failed);

            if (this.m_encryptThenMac)
            {
                var expectedMac = this.m_readMac.CalculateMac(seqNo, recordType, ciphertext, offset, len - macSize);

                var checkMac = TlsUtilities.ConstantTimeAreEqual(macSize, expectedMac, 0, ciphertext,
                    offset + len - macSize);
                if (!checkMac)
                    /*
                         * RFC 7366 3. The MAC SHALL be evaluated before any further processing such as
                         * decryption is performed, and if the MAC verification fails, then processing SHALL
                         * terminate immediately. For TLS, a fatal bad_record_mac MUST be generated [2]. For
                         * DTLS, the record MUST be discarded, and a fatal bad_record_mac MAY be generated
                         * [4]. This immediate response to a bad MAC eliminates any timing channels that may
                         * be available through the use of manipulated packet data.
                         */
                    throw new TlsFatalAlert(AlertDescription.bad_record_mac);
            }

            this.m_decryptCipher.DoFinal(ciphertext, offset, blocks_length, ciphertext, offset);

            if (this.m_useExplicitIV)
            {
                offset        += blockSize;
                blocks_length -= blockSize;
            }

            // If there's anything wrong with the padding, this will return zero
            var totalPad = this.CheckPaddingConstantTime(ciphertext, offset, blocks_length, blockSize, this.m_encryptThenMac ? 0 : macSize);
            var badMac   = totalPad == 0;

            var dec_output_length = blocks_length - totalPad;

            if (!this.m_encryptThenMac)
            {
                dec_output_length -= macSize;

                var expectedMac = this.m_readMac.CalculateMacConstantTime(seqNo, recordType, ciphertext, offset,
                    dec_output_length, blocks_length - macSize, this.m_randomData);

                badMac |= !TlsUtilities.ConstantTimeAreEqual(macSize, expectedMac, 0, ciphertext,
                    offset + dec_output_length);
            }

            if (badMac)
                throw new TlsFatalAlert(AlertDescription.bad_record_mac);

            return new TlsDecodeResult(ciphertext, offset, dec_output_length, recordType);
        }

        public virtual void RekeyDecoder() { throw new TlsFatalAlert(AlertDescription.internal_error); }

        public virtual void RekeyEncoder() { throw new TlsFatalAlert(AlertDescription.internal_error); }

        public virtual bool UsesOpaqueRecordType => false;

        protected virtual int CheckPaddingConstantTime(byte[] buf, int off, int len, int blockSize, int macSize)
        {
            var end      = off + len;
            var lastByte = buf[end - 1];
            var padlen   = lastByte & 0xff;
            var totalPad = padlen + 1;

            var  dummyIndex = 0;
            byte padDiff    = 0;

            var totalPadLimit = Math.Min(this.m_acceptExtraPadding ? 256 : blockSize, len - macSize);

            if (totalPad > totalPadLimit)
            {
                totalPad = 0;
            }
            else
            {
                var padPos = end - totalPad;
                do
                {
                    padDiff |= (byte)(buf[padPos++] ^ lastByte);
                } while (padPos < end);

                dummyIndex = totalPad;

                if (padDiff != 0) totalPad = 0;
            }

            // Run some extra dummy checks so the number of checks is always constant
            {
                var dummyPad                     = this.m_randomData;
                while (dummyIndex < 256) padDiff |= (byte)(dummyPad[dummyIndex++] ^ lastByte);
                // Ensure the above loop is not eliminated
                dummyPad[0] ^= padDiff;
            }

            return totalPad;
        }

        protected virtual int ChooseExtraPadBlocks(int max)
        {
            var random = this.m_cryptoParams.NonceGenerator.GenerateNonce(4);
            var x      = (int)Pack.LE_To_UInt32(random, 0);
            var n      = Integers.NumberOfTrailingZeros(x);
            return Math.Min(n, max);
        }

        protected virtual int GetCiphertextLength(int blockSize, int macSize, int maxPadding, int plaintextLength)
        {
            var ciphertextLength = plaintextLength;

            // An explicit IV consumes 1 block
            if (this.m_useExplicitIV) ciphertextLength += blockSize;

            // Leave room for the MAC and (block-aligning) padding

            ciphertextLength += maxPadding;

            if (this.m_encryptThenMac)
            {
                ciphertextLength -= ciphertextLength % blockSize;
                ciphertextLength += macSize;
            }
            else
            {
                ciphertextLength += macSize;
                ciphertextLength -= ciphertextLength % blockSize;
            }

            return ciphertextLength;
        }
    }
}
#pragma warning restore
#endif