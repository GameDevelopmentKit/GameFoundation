#if !BESTHTTP_DISABLE_ALTERNATE_SSL && (!UNITY_WEBGL || UNITY_EDITOR)
#pragma warning disable
namespace BestHTTP.SecureProtocol.Org.BouncyCastle.Tls
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using BestHTTP.SecureProtocol.Org.BouncyCastle.Tls.Crypto;
    using BestHTTP.SecureProtocol.Org.BouncyCastle.Utilities;

    /// <summary>An implementation of the TLS 1.0/1.1/1.2 record layer.</summary>
    internal sealed class RecordStream
    {
        private const int DefaultPlaintextLimit = 1 << 14;

        private readonly Record         m_inputRecord = new();
        private readonly SequenceNumber m_readSeqNo   = new(), m_writeSeqNo = new();

        private readonly TlsProtocol m_handler;
        private readonly Stream      m_input;
        private readonly Stream      m_output;

        private TlsCipher m_pendingCipher;
        private TlsCipher m_readCipher = TlsNullNullCipher.Instance;
        private TlsCipher m_readCipherDeferred;
        private TlsCipher m_writeCipher = TlsNullNullCipher.Instance;

        private ProtocolVersion m_writeVersion;

        private int  m_ciphertextLimit = DefaultPlaintextLimit;
        private bool m_ignoreChangeCipherSpec;

        internal RecordStream(TlsProtocol handler, Stream input, Stream output)
        {
            this.m_handler = handler;
            this.m_input   = input;
            this.m_output  = output;
        }

        internal int PlaintextLimit { get; private set; } = DefaultPlaintextLimit;

        internal void SetPlaintextLimit(int plaintextLimit)
        {
            this.PlaintextLimit    = plaintextLimit;
            this.m_ciphertextLimit = this.m_readCipher.GetCiphertextDecodeLimit(plaintextLimit);
        }

        internal void SetWriteVersion(ProtocolVersion writeVersion) { this.m_writeVersion = writeVersion; }

        internal void SetIgnoreChangeCipherSpec(bool ignoreChangeCipherSpec) { this.m_ignoreChangeCipherSpec = ignoreChangeCipherSpec; }

        internal void SetPendingCipher(TlsCipher tlsCipher) { this.m_pendingCipher = tlsCipher; }

        /// <exception cref="IOException" />
        internal void NotifyChangeCipherSpecReceived()
        {
            if (this.m_pendingCipher == null)
                throw new TlsFatalAlert(AlertDescription.unexpected_message, "No pending cipher");

            this.EnablePendingCipherRead(false);
        }

        /// <exception cref="IOException" />
        internal void EnablePendingCipherRead(bool deferred)
        {
            if (this.m_pendingCipher == null)
                throw new TlsFatalAlert(AlertDescription.internal_error);

            if (this.m_readCipherDeferred != null)
                throw new TlsFatalAlert(AlertDescription.internal_error);

            if (deferred)
            {
                this.m_readCipherDeferred = this.m_pendingCipher;
            }
            else
            {
                this.m_readCipher      = this.m_pendingCipher;
                this.m_ciphertextLimit = this.m_readCipher.GetCiphertextDecodeLimit(this.PlaintextLimit);
                this.m_readSeqNo.Reset();
            }
        }

        /// <exception cref="IOException" />
        internal void EnablePendingCipherWrite()
        {
            if (this.m_pendingCipher == null)
                throw new TlsFatalAlert(AlertDescription.internal_error);

            this.m_writeCipher = this.m_pendingCipher;
            this.m_writeSeqNo.Reset();
        }

        /// <exception cref="IOException" />
        internal void FinaliseHandshake()
        {
            if (this.m_readCipher != this.m_pendingCipher || this.m_writeCipher != this.m_pendingCipher)
                throw new TlsFatalAlert(AlertDescription.handshake_failure);

            this.m_pendingCipher = null;
        }

        internal bool NeedsKeyUpdate() { return this.m_writeSeqNo.CurrentValue >= 1L << 20; }

        /// <exception cref="IOException" />
        internal void NotifyKeyUpdateReceived()
        {
            this.m_readCipher.RekeyDecoder();
            this.m_readSeqNo.Reset();
        }

        /// <exception cref="IOException" />
        internal void NotifyKeyUpdateSent()
        {
            this.m_writeCipher.RekeyEncoder();
            this.m_writeSeqNo.Reset();
        }

        /// <exception cref="IOException" />
        internal RecordPreview PreviewRecordHeader(byte[] recordHeader)
        {
            var recordType = this.CheckRecordType(recordHeader, RecordFormat.TypeOffset);

            //ProtocolVersion recordVersion = TlsUtilities.ReadVersion(recordHeader, RecordFormat.VersionOffset);

            var length = TlsUtilities.ReadUint16(recordHeader, RecordFormat.LengthOffset);

            CheckLength(length, this.m_ciphertextLimit, AlertDescription.record_overflow);

            var recordSize           = RecordFormat.FragmentOffset + length;
            var applicationDataLimit = 0;

            // NOTE: For TLS 1.3, this only MIGHT be application data
            if (ContentType.application_data == recordType && this.m_handler.IsApplicationDataReady)
                applicationDataLimit = Math.Max(0, Math.Min(this.PlaintextLimit, this.m_readCipher.GetPlaintextLimit(length)));

            return new RecordPreview(recordSize, applicationDataLimit);
        }

        internal RecordPreview PreviewOutputRecord(int contentLength)
        {
            var contentLimit = Math.Max(0, Math.Min(this.PlaintextLimit, contentLength));
            var recordSize   = this.PreviewOutputRecordSize(contentLimit);
            return new RecordPreview(recordSize, contentLimit);
        }

        internal int PreviewOutputRecordSize(int contentLength)
        {
            Debug.Assert(contentLength <= this.PlaintextLimit);

            return RecordFormat.FragmentOffset + this.m_writeCipher.GetCiphertextEncodeLimit(contentLength, this.PlaintextLimit);
        }

        /// <exception cref="IOException" />
        internal bool ReadFullRecord(byte[] input, int inputOff, int inputLen)
        {
            if (inputLen < RecordFormat.FragmentOffset)
                return false;

            var length = TlsUtilities.ReadUint16(input, inputOff + RecordFormat.LengthOffset);
            if (inputLen != RecordFormat.FragmentOffset + length)
                return false;

            var recordType = this.CheckRecordType(input, inputOff + RecordFormat.TypeOffset);

            var recordVersion = TlsUtilities.ReadVersion(input, inputOff + RecordFormat.VersionOffset);

            CheckLength(length, this.m_ciphertextLimit, AlertDescription.record_overflow);

            if (this.m_ignoreChangeCipherSpec && ContentType.change_cipher_spec == recordType)
            {
                this.CheckChangeCipherSpec(input, inputOff + RecordFormat.FragmentOffset, length);
                return true;
            }

            var decoded = this.DecodeAndVerify(recordType, recordVersion, input,
                inputOff + RecordFormat.FragmentOffset, length);

            this.m_handler.ProcessRecord(decoded.contentType, decoded.buf, decoded.off, decoded.len);
            return true;
        }

        /// <exception cref="IOException" />
        internal bool ReadRecord()
        {
            if (!this.m_inputRecord.ReadHeader(this.m_input))
                return false;

            var recordType = this.CheckRecordType(this.m_inputRecord.m_buf, RecordFormat.TypeOffset);

            var recordVersion = TlsUtilities.ReadVersion(this.m_inputRecord.m_buf, RecordFormat.VersionOffset);

            var length = TlsUtilities.ReadUint16(this.m_inputRecord.m_buf, RecordFormat.LengthOffset);

            CheckLength(length, this.m_ciphertextLimit, AlertDescription.record_overflow);

            this.m_inputRecord.ReadFragment(this.m_input, length);

            TlsDecodeResult decoded;
            try
            {
                if (this.m_ignoreChangeCipherSpec && ContentType.change_cipher_spec == recordType)
                {
                    this.CheckChangeCipherSpec(this.m_inputRecord.m_buf, RecordFormat.FragmentOffset, length);
                    return true;
                }

                decoded = this.DecodeAndVerify(recordType, recordVersion, this.m_inputRecord.m_buf, RecordFormat.FragmentOffset,
                    length);
            }
            finally
            {
                this.m_inputRecord.Reset();
            }

            this.m_handler.ProcessRecord(decoded.contentType, decoded.buf, decoded.off, decoded.len);
            return true;
        }

        /// <exception cref="IOException" />
        internal TlsDecodeResult DecodeAndVerify(short recordType, ProtocolVersion recordVersion, byte[] ciphertext,
            int off, int len)
        {
            var seqNo = this.m_readSeqNo.NextValue(AlertDescription.unexpected_message);
            var decoded = this.m_readCipher.DecodeCiphertext(seqNo, recordType, recordVersion, ciphertext, off,
                len);

            CheckLength(decoded.len, this.PlaintextLimit, AlertDescription.record_overflow);

            /*
             * RFC 5246 6.2.1 Implementations MUST NOT send zero-length fragments of Handshake, Alert,
             * or ChangeCipherSpec content types.
             */
            if (decoded.len < 1 && decoded.contentType != ContentType.application_data)
                throw new TlsFatalAlert(AlertDescription.illegal_parameter);

            return decoded;
        }

        /// <exception cref="IOException" />
        internal void WriteRecord(short contentType, byte[] plaintext, int plaintextOffset, int plaintextLength)
        {
            // Never send anything until a valid ClientHello has been received
            if (this.m_writeVersion == null)
                return;

            /*
             * RFC 5246 6.2.1 The length should not exceed 2^14.
             */
            CheckLength(plaintextLength, this.PlaintextLimit, AlertDescription.internal_error);

            /*
             * RFC 5246 6.2.1 Implementations MUST NOT send zero-length fragments of Handshake, Alert,
             * or ChangeCipherSpec content types.
             */
            if (plaintextLength < 1 && contentType != ContentType.application_data)
                throw new TlsFatalAlert(AlertDescription.internal_error);

            var seqNo         = this.m_writeSeqNo.NextValue(AlertDescription.internal_error);
            var recordVersion = this.m_writeVersion;

            var encoded = this.m_writeCipher.EncodePlaintext(seqNo, contentType, recordVersion,
                RecordFormat.FragmentOffset, plaintext, plaintextOffset, plaintextLength);

            var ciphertextLength = encoded.len - RecordFormat.FragmentOffset;
            TlsUtilities.CheckUint16(ciphertextLength);

            TlsUtilities.WriteUint8(encoded.recordType, encoded.buf, encoded.off + RecordFormat.TypeOffset);
            TlsUtilities.WriteVersion(recordVersion, encoded.buf, encoded.off + RecordFormat.VersionOffset);
            TlsUtilities.WriteUint16(ciphertextLength, encoded.buf, encoded.off + RecordFormat.LengthOffset);

            // TODO[tls-port] Can we support interrupted IO on .NET?
            //try
            //{
            this.m_output.Write(encoded.buf, encoded.off, encoded.len);
            //}
            //catch (InterruptedIOException e)
            //{
            //    throw new TlsFatalAlert(AlertDescription.internal_error, e);
            //}

            this.m_output.Flush();
        }

        /// <exception cref="IOException" />
        internal void Close()
        {
            this.m_inputRecord.Reset();

            IOException io = null;
            try
            {
                Platform.Dispose(this.m_input);
            }
            catch (IOException e)
            {
                io = e;
            }

            try
            {
                Platform.Dispose(this.m_output);
            }
            catch (IOException e)
            {
                if (io == null)
                {
                    io = e;
                }
            }

            if (io != null)
                throw io;
        }

        /// <exception cref="IOException" />
        private void CheckChangeCipherSpec(byte[] buf, int off, int len)
        {
            if (1 != len || (byte)ChangeCipherSpec.change_cipher_spec != buf[off])
                throw new TlsFatalAlert(AlertDescription.unexpected_message,
                    "Malformed " + ContentType.GetText(ContentType.change_cipher_spec));
        }

        /// <exception cref="IOException" />
        private short CheckRecordType(byte[] buf, int off)
        {
            var recordType = TlsUtilities.ReadUint8(buf, off);

            if (null != this.m_readCipherDeferred && recordType == ContentType.application_data)
            {
                this.m_readCipher         = this.m_readCipherDeferred;
                this.m_readCipherDeferred = null;
                this.m_ciphertextLimit    = this.m_readCipher.GetCiphertextDecodeLimit(this.PlaintextLimit);
                this.m_readSeqNo.Reset();
            }
            else if (this.m_readCipher.UsesOpaqueRecordType)
            {
                if (ContentType.application_data != recordType)
                {
                    if (this.m_ignoreChangeCipherSpec && ContentType.change_cipher_spec == recordType)
                    {
                        // See RFC 8446 D.4.
                    }
                    else
                    {
                        throw new TlsFatalAlert(AlertDescription.unexpected_message,
                            "Opaque " + ContentType.GetText(recordType));
                    }
                }
            }
            else
            {
                switch (recordType)
                {
                    case ContentType.application_data:
                    {
                        if (!this.m_handler.IsApplicationDataReady)
                            throw new TlsFatalAlert(AlertDescription.unexpected_message,
                                "Not ready for " + ContentType.GetText(ContentType.application_data));
                        break;
                    }
                    case ContentType.alert:
                    case ContentType.change_cipher_spec:
                    case ContentType.handshake:
                        //        case ContentType.heartbeat:
                        break;
                    default:
                        throw new TlsFatalAlert(AlertDescription.unexpected_message,
                            "Unsupported " + ContentType.GetText(recordType));
                }
            }

            return recordType;
        }

        /// <exception cref="IOException" />
        private static void CheckLength(int length, int limit, short alertDescription)
        {
            if (length > limit)
                throw new TlsFatalAlert(alertDescription);
        }

        private sealed class Record
        {
            private readonly byte[] m_header = new byte[RecordFormat.FragmentOffset];

            internal volatile byte[] m_buf;
            internal volatile int    m_pos;

            internal Record()
            {
                this.m_buf = this.m_header;
                this.m_pos = 0;
            }

            /// <exception cref="IOException" />
            internal void FillTo(Stream input, int length)
            {
                while (this.m_pos < length)
                {
                    // TODO[tls-port] Can we support interrupted IO on .NET?
                    //try
                    //{
                    var numRead = input.Read(this.m_buf, this.m_pos, length - this.m_pos);
                    if (numRead < 1)
                        break;

                    this.m_pos += numRead;
                    //}
                    //catch (InterruptedIOException e)
                    //{
                    //    /*
                    //     * Although modifying the bytesTransferred doesn't seem ideal, it's the simplest
                    //     * way to make sure we don't break client code that depends on the exact type,
                    //     * e.g. in Apache's httpcomponents-core-4.4.9, BHttpConnectionBase.isStale
                    //     * depends on the exception type being SocketTimeoutException (or a subclass).
                    //     *
                    //     * We can set to 0 here because the only relevant callstack (via
                    //     * TlsProtocol.readApplicationData) only ever processes one non-empty record (so
                    //     * interruption after partial output cannot occur).
                    //     */
                    //    m_pos += e.bytesTransferred;
                    //    e.bytesTransferred = 0;
                    //    throw e;
                    //}
                }
            }

            /// <exception cref="IOException" />
            internal void ReadFragment(Stream input, int fragmentLength)
            {
                var recordLength = RecordFormat.FragmentOffset + fragmentLength;
                this.Resize(recordLength);
                this.FillTo(input, recordLength);
                if (this.m_pos < recordLength)
                    throw new EndOfStreamException();
            }

            /// <exception cref="IOException" />
            internal bool ReadHeader(Stream input)
            {
                this.FillTo(input, RecordFormat.FragmentOffset);
                if (this.m_pos == 0)
                    return false;

                if (this.m_pos < RecordFormat.FragmentOffset)
                    throw new EndOfStreamException();

                return true;
            }

            internal void Reset()
            {
                this.m_buf = this.m_header;
                this.m_pos = 0;
            }

            private void Resize(int length)
            {
                if (this.m_buf.Length < length)
                {
                    var tmp = new byte[length];
                    Array.Copy(this.m_buf, 0, tmp, 0, this.m_pos);
                    this.m_buf = tmp;
                }
            }
        }

        private sealed class SequenceNumber
        {
            private long m_value;
            private bool m_exhausted;

            internal long CurrentValue
            {
                get
                {
                    lock (this)
                    {
                        return this.m_value;
                    }
                }
            }

            /// <exception cref="TlsFatalAlert" />
            internal long NextValue(short alertDescription)
            {
                lock (this)
                {
                    if (this.m_exhausted)
                        throw new TlsFatalAlert(alertDescription, "Sequence numbers exhausted");

                    var result                                 = this.m_value;
                    if (++this.m_value == 0L) this.m_exhausted = true;
                    return result;
                }
            }

            internal void Reset()
            {
                lock (this)
                {
                    this.m_value     = 0L;
                    this.m_exhausted = false;
                }
            }
        }
    }
}
#pragma warning restore
#endif