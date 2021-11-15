#if !BESTHTTP_DISABLE_ALTERNATE_SSL && (!UNITY_WEBGL || UNITY_EDITOR)
#pragma warning disable
using System;
using System.IO;

using BestHTTP.PlatformSupport.Memory;
using BestHTTP.SecureProtocol.Org.BouncyCastle.Crypto.Engines;
using BestHTTP.SecureProtocol.Org.BouncyCastle.Crypto.Generators;
using BestHTTP.SecureProtocol.Org.BouncyCastle.Crypto.Macs;
using BestHTTP.SecureProtocol.Org.BouncyCastle.Crypto.Parameters;
using BestHTTP.SecureProtocol.Org.BouncyCastle.Crypto.Utilities;
using BestHTTP.SecureProtocol.Org.BouncyCastle.Security;
using BestHTTP.SecureProtocol.Org.BouncyCastle.Utilities;

namespace BestHTTP.SecureProtocol.Org.BouncyCastle.Crypto.Tls
{
    /**
     * draft-ietf-tls-chacha20-poly1305-04
     */
    [BestHTTP.PlatformSupport.IL2CPP.Il2CppSetOption(BestHTTP.PlatformSupport.IL2CPP.Option.NullChecks, false)]
    [BestHTTP.PlatformSupport.IL2CPP.Il2CppSetOption(BestHTTP.PlatformSupport.IL2CPP.Option.ArrayBoundsChecks, false)]
    [BestHTTP.PlatformSupport.IL2CPP.Il2CppSetOption(BestHTTP.PlatformSupport.IL2CPP.Option.DivideByZeroChecks, false)]
    [BestHTTP.PlatformSupport.IL2CPP.Il2CppEagerStaticClassConstructionAttribute]
    public sealed class Chacha20Poly1305
        :   TlsCipher
    {
        private static readonly byte[] Zeroes = new byte[15];

        protected readonly TlsContext context;

        protected readonly ChaCha7539Engine encryptCipher, decryptCipher;
        protected readonly byte[] encryptIV, decryptIV;

        /// <exception cref="IOException"></exception>
        public Chacha20Poly1305(TlsContext context)
        {
            if (!TlsUtilities.IsTlsV12(context))
                throw new TlsFatalAlert(AlertDescription.internal_error);

            this.context = context;

            int cipherKeySize = 32;
            // TODO SecurityParameters.fixed_iv_length
            int fixed_iv_length = 12;
            // TODO SecurityParameters.record_iv_length = 0

            int key_block_size = (2 * cipherKeySize) + (2 * fixed_iv_length);

            byte[] key_block = TlsUtilities.CalculateKeyBlock(context, key_block_size);

            int offset = 0;

            KeyParameter client_write_key = new KeyParameter(key_block, offset, cipherKeySize);
            offset += cipherKeySize;
            KeyParameter server_write_key = new KeyParameter(key_block, offset, cipherKeySize);
            offset += cipherKeySize;
            byte[] client_write_IV = Arrays.CopyOfRange(key_block, offset, offset + fixed_iv_length);
            offset += fixed_iv_length;
            byte[] server_write_IV = Arrays.CopyOfRange(key_block, offset, offset + fixed_iv_length);
            offset += fixed_iv_length;

            if (offset != key_block_size)
                throw new TlsFatalAlert(AlertDescription.internal_error);

            this.encryptCipher = new ChaCha7539Engine();
            this.decryptCipher = new ChaCha7539Engine();

            KeyParameter encryptKey, decryptKey;
            if (context.IsServer)
            {
                encryptKey = server_write_key;
                decryptKey = client_write_key;
                this.encryptIV = server_write_IV;
                this.decryptIV = client_write_IV;
            }
            else
            {
                encryptKey = client_write_key;
                decryptKey = server_write_key;
                this.encryptIV = client_write_IV;
                this.decryptIV = server_write_IV;
            }

            this.encryptCipher.Init(true, new ParametersWithIV(encryptKey, encryptIV));
            this.decryptCipher.Init(false, new ParametersWithIV(decryptKey, decryptIV));
        }

        public /*virtual */int GetPlaintextLimit(int ciphertextLimit)
        {
            return ciphertextLimit - 16;
        }

        /// <exception cref="IOException"></exception>
        public /*virtual */BufferSegment EncodePlaintext(long seqNo, byte type, byte[] plaintext, int offset, int len)
        {
            KeyParameter macKey = InitRecord(encryptCipher, true, seqNo, encryptIV);

            byte[] output = BufferPool.Get(len + 16, true);
            encryptCipher.ProcessBytes(plaintext, offset, len, output, 0);

            BufferSegment additionalData = GetAdditionalData(seqNo, type, len);
            BufferSegment mac = CalculateRecordMac(macKey, additionalData, output, 0, len);
            Array.Copy(mac.Data, mac.Offset, output, len, mac.Count);

            BufferPool.Release(additionalData);
            BufferPool.Release(mac);

            return new BufferSegment(output, 0, len + 16);
        }

        /// <exception cref="IOException"></exception>
        public /*virtual */BufferSegment DecodeCiphertext(long seqNo, byte type, byte[] ciphertext, int offset, int len)
        {
            if (GetPlaintextLimit(len) < 0)
                throw new TlsFatalAlert(AlertDescription.decode_error);

            KeyParameter macKey = InitRecord(decryptCipher, false, seqNo, decryptIV);

            int plaintextLength = len - 16;

            BufferSegment additionalData = GetAdditionalData(seqNo, type, plaintextLength);
            BufferSegment calculatedMac = CalculateRecordMac(macKey, additionalData, ciphertext, offset, plaintextLength);
            BufferSegment receivedMac = new BufferSegment(ciphertext, offset + plaintextLength, 16);

            if (!Arrays.ConstantTimeAreEqual(calculatedMac, receivedMac))
                throw new TlsFatalAlert(AlertDescription.bad_record_mac);

            BufferPool.Release(additionalData);
            BufferPool.Release(calculatedMac);

            byte[] output = BufferPool.Get(plaintextLength, true);
            decryptCipher.ProcessBytes(ciphertext, offset, plaintextLength, output, 0);
            return new BufferSegment(output, 0, plaintextLength);
        }

        protected /*virtual */KeyParameter InitRecord(IStreamCipher cipher, bool forEncryption, long seqNo, byte[] iv)
        {
            byte[] nonce = CalculateNonce(seqNo, iv);
            cipher.Init(forEncryption, new ParametersWithIV(null, nonce));
            return GenerateRecordMacKey(cipher);
        }

        protected /*virtual */byte[] CalculateNonce(long seqNo, byte[] iv)
        {
            byte[] nonce = new byte[12];
            TlsUtilities.WriteUint64(seqNo, nonce, 4);

            for (int i = 0; i < 12; ++i)
            {
                nonce[i] ^= iv[i];
            }

            return nonce;
        }

        protected /*virtual */KeyParameter GenerateRecordMacKey(IStreamCipher cipher)
        {
            byte[] firstBlock = new byte[64];
            cipher.ProcessBytes(firstBlock, 0, firstBlock.Length, firstBlock, 0);

            KeyParameter macKey = new KeyParameter(firstBlock, 0, 32);
            Arrays.Fill(firstBlock, (byte)0);
            return macKey;
        }

        protected /*virtual */BufferSegment CalculateRecordMac(KeyParameter macKey, BufferSegment additionalData, byte[] buf, int off, int len)
        {
            Poly1305 mac = new Poly1305();
            mac.Init(macKey);

            UpdateRecordMacText(mac, additionalData);
            UpdateRecordMacText(mac, new BufferSegment(buf, off, len));
            UpdateRecordMacLength(mac, additionalData.Count);
            UpdateRecordMacLength(mac, len);

            return MacUtilities.DoFinalOptimized(mac);
        }

        protected /*virtual */void UpdateRecordMacLength(Poly1305 mac, int len)
        {
            byte[] longLen = Pack.UInt64_To_LE((ulong)len);
            mac.BlockUpdate(longLen, 0, longLen.Length);
        }

        protected /*virtual */void UpdateRecordMacText(Poly1305 mac, BufferSegment buf)
        {
            mac.BlockUpdate(buf.Data, buf.Offset, buf.Count);

            int partial = buf.Count % 16;
            if (partial != 0)
            {
                mac.BlockUpdate(Zeroes, 0, 16 - partial);
            }
        }

        /// <exception cref="IOException"></exception>
        protected /*virtual */BufferSegment GetAdditionalData(long seqNo, byte type, int len)
        {
            /*
             * additional_data = seq_num + TLSCompressed.type + TLSCompressed.version +
             * TLSCompressed.length
             */
            byte[] additional_data = BufferPool.Get(13, true);
            TlsUtilities.WriteUint64(seqNo, additional_data, 0);
            TlsUtilities.WriteUint8(type, additional_data, 8);
            TlsUtilities.WriteVersion(context.ServerVersion, additional_data, 9);
            TlsUtilities.WriteUint16(len, additional_data, 11);

            return new BufferSegment(additional_data, 0, 13);
        }
    }
}
#pragma warning restore
#endif
