#if !BESTHTTP_DISABLE_ALTERNATE_SSL && (!UNITY_WEBGL || UNITY_EDITOR)
#pragma warning disable
using System;
using System.IO;

using BestHTTP.PlatformSupport.Memory;
using BestHTTP.SecureProtocol.Org.BouncyCastle.Utilities;

namespace BestHTTP.SecureProtocol.Org.BouncyCastle.Crypto.Tls
{
    /// <summary>
    /// A NULL CipherSuite, with optional MAC.
    /// </summary>
    public class TlsNullCipher
        :   TlsCipher
    {
        protected readonly TlsContext context;

        protected readonly TlsMac writeMac;
        protected readonly TlsMac readMac;

        public TlsNullCipher(TlsContext context)
        {
            this.context = context;
            this.writeMac = null;
            this.readMac = null;
        }

        /// <exception cref="IOException"></exception>
        public TlsNullCipher(TlsContext context, IDigest clientWriteDigest, IDigest serverWriteDigest)
        {
            if ((clientWriteDigest == null) != (serverWriteDigest == null))
                throw new TlsFatalAlert(AlertDescription.internal_error);

            this.context = context;

            TlsMac clientWriteMac = null, serverWriteMac = null;

            if (clientWriteDigest != null)
            {
                int key_block_size = clientWriteDigest.GetDigestSize()
                    + serverWriteDigest.GetDigestSize();
                byte[] key_block = TlsUtilities.CalculateKeyBlock(context, key_block_size);

                int offset = 0;

                clientWriteMac = new TlsMac(context, clientWriteDigest, key_block, offset,
                    clientWriteDigest.GetDigestSize());
                offset += clientWriteDigest.GetDigestSize();

                serverWriteMac = new TlsMac(context, serverWriteDigest, key_block, offset,
                    serverWriteDigest.GetDigestSize());
                offset += serverWriteDigest.GetDigestSize();

                if (offset != key_block_size)
                {
                    throw new TlsFatalAlert(AlertDescription.internal_error);
                }
            }

            if (context.IsServer)
            {
                writeMac = serverWriteMac;
                readMac = clientWriteMac;
            }
            else
            {
                writeMac = clientWriteMac;
                readMac = serverWriteMac;
            }
        }

        public virtual int GetPlaintextLimit(int ciphertextLimit)
        {
            int result = ciphertextLimit;
            if (writeMac != null)
            {
                result -= writeMac.Size;
            }
            return result;
        }

        /// <exception cref="IOException"></exception>
        public virtual BufferSegment EncodePlaintext(long seqNo, byte type, byte[] plaintext, int offset, int len)
        {
            if (writeMac == null)
            {
                byte[] output = Arrays.CopyOfRange(plaintext, offset, offset + len);
                return new BufferSegment(output, 0, output.Length);
            }

            BufferSegment mac = writeMac.CalculateMac(seqNo, type, plaintext, offset, len);
            int ciphertextLength = len + mac.Count;
            byte[] ciphertext = BufferPool.Get(len + mac.Count, true);
            Array.Copy(plaintext, offset, ciphertext, 0, len);
            Array.Copy(mac.Data, 0, ciphertext, len, mac.Count);

            BufferPool.Release(mac);

            return new BufferSegment(ciphertext, 0, ciphertextLength);
        }

        /// <exception cref="IOException"></exception>
        public virtual BufferSegment DecodeCiphertext(long seqNo, byte type, byte[] ciphertext, int offset, int len)
        {
            byte[] output;
            if (readMac == null)
            {
                output = Arrays.CopyOfRange(ciphertext, offset, offset + len);
                return new BufferSegment(output, 0, output.Length);
            }

            int macSize = readMac.Size;
            if (len < macSize)
                throw new TlsFatalAlert(AlertDescription.decode_error);

            int macInputLen = len - macSize;

            BufferSegment receivedMac = new BufferSegment(ciphertext, offset + macInputLen, offset + len);
            BufferSegment computedMac = readMac.CalculateMac(seqNo, type, ciphertext, offset, macInputLen);

            if (!Arrays.ConstantTimeAreEqual(receivedMac, computedMac))
                throw new TlsFatalAlert(AlertDescription.bad_record_mac);

            BufferPool.Release(computedMac);

            output = Arrays.CopyOfRange(ciphertext, offset, offset + macInputLen);
            return new BufferSegment(output, 0, output.Length);
        }
    }
}
#pragma warning restore
#endif
