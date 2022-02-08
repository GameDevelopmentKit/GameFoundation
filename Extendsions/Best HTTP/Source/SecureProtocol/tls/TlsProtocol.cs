#if !BESTHTTP_DISABLE_ALTERNATE_SSL && (!UNITY_WEBGL || UNITY_EDITOR)
#pragma warning disable
namespace BestHTTP.SecureProtocol.Org.BouncyCastle.Tls
{
    using System;
    using System.Collections;
    using System.IO;
    using BestHTTP.SecureProtocol.Org.BouncyCastle.Tls.Crypto;
    using BestHTTP.SecureProtocol.Org.BouncyCastle.Utilities;

    public abstract class TlsProtocol
        : TlsCloseable
    {
        /*
         * Connection States.
         * 
         * NOTE: Redirection of handshake messages to TLS 1.3 handlers assumes CS_START, CS_CLIENT_HELLO
         * are lower than any of the other values.
         */
        protected const short CS_START                       = 0;
        protected const short CS_CLIENT_HELLO                = 1;
        protected const short CS_SERVER_HELLO_RETRY_REQUEST  = 2;
        protected const short CS_CLIENT_HELLO_RETRY          = 3;
        protected const short CS_SERVER_HELLO                = 4;
        protected const short CS_SERVER_ENCRYPTED_EXTENSIONS = 5;
        protected const short CS_SERVER_SUPPLEMENTAL_DATA    = 6;
        protected const short CS_SERVER_CERTIFICATE          = 7;
        protected const short CS_SERVER_CERTIFICATE_STATUS   = 8;
        protected const short CS_SERVER_CERTIFICATE_VERIFY   = 9;
        protected const short CS_SERVER_KEY_EXCHANGE         = 10;
        protected const short CS_SERVER_CERTIFICATE_REQUEST  = 11;
        protected const short CS_SERVER_HELLO_DONE           = 12;
        protected const short CS_CLIENT_END_OF_EARLY_DATA    = 13;
        protected const short CS_CLIENT_SUPPLEMENTAL_DATA    = 14;
        protected const short CS_CLIENT_CERTIFICATE          = 15;
        protected const short CS_CLIENT_KEY_EXCHANGE         = 16;
        protected const short CS_CLIENT_CERTIFICATE_VERIFY   = 17;
        protected const short CS_CLIENT_FINISHED             = 18;
        protected const short CS_SERVER_SESSION_TICKET       = 19;
        protected const short CS_SERVER_FINISHED             = 20;
        protected const short CS_END                         = 21;

        protected bool IsLegacyConnectionState()
        {
            switch (this.m_connectionState)
            {
                case CS_START:
                case CS_CLIENT_HELLO:
                case CS_SERVER_HELLO:
                case CS_SERVER_SUPPLEMENTAL_DATA:
                case CS_SERVER_CERTIFICATE:
                case CS_SERVER_CERTIFICATE_STATUS:
                case CS_SERVER_KEY_EXCHANGE:
                case CS_SERVER_CERTIFICATE_REQUEST:
                case CS_SERVER_HELLO_DONE:
                case CS_CLIENT_SUPPLEMENTAL_DATA:
                case CS_CLIENT_CERTIFICATE:
                case CS_CLIENT_KEY_EXCHANGE:
                case CS_CLIENT_CERTIFICATE_VERIFY:
                case CS_CLIENT_FINISHED:
                case CS_SERVER_SESSION_TICKET:
                case CS_SERVER_FINISHED:
                case CS_END:
                    return true;

                case CS_SERVER_HELLO_RETRY_REQUEST:
                case CS_CLIENT_HELLO_RETRY:
                case CS_SERVER_ENCRYPTED_EXTENSIONS:
                case CS_SERVER_CERTIFICATE_VERIFY:
                case CS_CLIENT_END_OF_EARLY_DATA:
                default:
                    return false;
            }
        }

        protected bool IsTlsV13ConnectionState()
        {
            switch (this.m_connectionState)
            {
                case CS_START:
                case CS_CLIENT_HELLO:
                case CS_SERVER_HELLO_RETRY_REQUEST:
                case CS_CLIENT_HELLO_RETRY:
                case CS_SERVER_HELLO:
                case CS_SERVER_ENCRYPTED_EXTENSIONS:
                case CS_SERVER_CERTIFICATE_REQUEST:
                case CS_SERVER_CERTIFICATE:
                case CS_SERVER_CERTIFICATE_VERIFY:
                case CS_SERVER_FINISHED:
                case CS_CLIENT_END_OF_EARLY_DATA:
                case CS_CLIENT_CERTIFICATE:
                case CS_CLIENT_CERTIFICATE_VERIFY:
                case CS_CLIENT_FINISHED:
                case CS_END:
                    return true;

                case CS_SERVER_SUPPLEMENTAL_DATA:
                case CS_SERVER_CERTIFICATE_STATUS:
                case CS_SERVER_KEY_EXCHANGE:
                case CS_SERVER_HELLO_DONE:
                case CS_CLIENT_SUPPLEMENTAL_DATA:
                case CS_CLIENT_KEY_EXCHANGE:
                case CS_SERVER_SESSION_TICKET:
                default:
                    return false;
            }
        }

        /*
         * Different modes to handle the known IV weakness
         */
        protected const short ADS_MODE_1_Nsub1       = 0; // 1/n-1 record splitting
        protected const short ADS_MODE_0_N           = 1; // 0/n record splitting
        protected const short ADS_MODE_0_N_FIRSTONLY = 2; // 0/n record splitting on first data fragment only

        /*
         * Queues for data from some protocols.
         */
        private readonly ByteQueue m_applicationDataQueue = new(0);
        private readonly ByteQueue m_alertQueue           = new(2);

        private readonly ByteQueue m_handshakeQueue = new(0);
        //private readonly ByteQueue m_heartbeatQueue = new ByteQueue(0);

        internal readonly RecordStream m_recordStream;
        internal readonly object       m_recordWriteLock = new();

        private int m_maxHandshakeMessageSize = -1;

        internal TlsHandshakeHash m_handshakeHash;

        private TlsStream m_tlsStream;

        private volatile bool m_closed;
        private volatile bool m_failedWithError;
        private volatile bool m_appDataReady;
        private volatile bool m_appDataSplitEnabled = true;

        private volatile bool m_keyUpdateEnabled;

        //private volatile bool m_keyUpdatePendingReceive = false;
        private volatile bool m_keyUpdatePendingSend;
        private volatile bool m_resumableHandshake;
        private volatile int  m_appDataSplitMode = ADS_MODE_1_Nsub1;

        protected TlsSession        m_tlsSession;
        protected SessionParameters m_sessionParameters;
        protected TlsSecret         m_sessionMasterSecret;

        protected byte[]      m_retryCookie;
        protected int         m_retryGroup = -1;
        protected IDictionary m_clientExtensions;
        protected IDictionary m_serverExtensions;

        protected short m_connectionState = CS_START;
        protected bool  m_resumedSession;
        protected bool  m_selectedPsk13;
        protected bool  m_receivedChangeCipherSpec;
        protected bool  m_expectSessionTicket;

        protected readonly bool                  m_blocking;
        protected readonly ByteQueueInputStream  m_inputBuffers;
        protected readonly ByteQueueOutputStream m_outputBuffer;

        protected TlsProtocol()
        {
            this.m_blocking     = false;
            this.m_inputBuffers = new ByteQueueInputStream();
            this.m_outputBuffer = new ByteQueueOutputStream();
            this.m_recordStream = new RecordStream(this, this.m_inputBuffers, this.m_outputBuffer);
        }

        public TlsProtocol(Stream stream)
            : this(stream, stream)
        {
        }

        public TlsProtocol(Stream input, Stream output)
        {
            this.m_blocking     = true;
            this.m_inputBuffers = null;
            this.m_outputBuffer = null;
            this.m_recordStream = new RecordStream(this, input, output);
        }

        /// <exception cref="IOException" />
        public virtual void ResumeHandshake()
        {
            if (!this.m_blocking)
                throw new InvalidOperationException("Cannot use ResumeHandshake() in non-blocking mode!");
            if (!this.IsHandshaking)
                throw new InvalidOperationException("No handshake in progress");

            this.BlockForHandshake();
        }

        /// <exception cref="IOException" />
        protected virtual void CloseConnection() { this.m_recordStream.Close(); }

        protected abstract TlsContext Context { get; }

        internal abstract AbstractTlsContext ContextAdmin { get; }

        protected abstract TlsPeer Peer { get; }

        /// <exception cref="IOException" />
        protected virtual void HandleAlertMessage(short alertLevel, short alertDescription)
        {
            this.Peer.NotifyAlertReceived(alertLevel, alertDescription);

            if (alertLevel == AlertLevel.warning)
            {
                this.HandleAlertWarningMessage(alertDescription);
            }
            else
            {
                this.HandleFailure();

                throw new TlsFatalAlertReceived(alertDescription);
            }
        }

        /// <exception cref="IOException" />
        protected virtual void HandleAlertWarningMessage(short alertDescription)
        {
            switch (alertDescription)
            {
                /*
                 * RFC 5246 7.2.1. The other party MUST respond with a close_notify alert of its own
                 * and close down the connection immediately, discarding any pending writes.
                 */
                case AlertDescription.close_notify:
                {
                    if (!this.m_appDataReady)
                        throw new TlsFatalAlert(AlertDescription.handshake_failure);

                    this.HandleClose(false);
                    break;
                }
                case AlertDescription.no_certificate:
                {
                    throw new TlsFatalAlert(AlertDescription.unexpected_message);
                }
                case AlertDescription.no_renegotiation:
                {
                    // TODO[reneg] Give peer the option to tolerate this
                    throw new TlsFatalAlert(AlertDescription.handshake_failure);
                }
            }
        }

        /// <exception cref="IOException" />
        protected virtual void HandleChangeCipherSpecMessage() { }

        /// <exception cref="IOException" />
        protected virtual void HandleClose(bool user_canceled)
        {
            if (!this.m_closed)
            {
                this.m_closed = true;

                if (!this.m_appDataReady)
                {
                    this.CleanupHandshake();

                    if (user_canceled) this.RaiseAlertWarning(AlertDescription.user_canceled, "User canceled handshake");
                }

                this.RaiseAlertWarning(AlertDescription.close_notify, "Connection closed");

                this.CloseConnection();
            }
        }

        /// <exception cref="IOException" />
        protected virtual void HandleException(short alertDescription, string message, Exception e)
        {
            // TODO[tls-port] Can we support interrupted IO on .NET?
            //if ((m_appDataReady || IsResumableHandshake()) && (e is InterruptedIOException))
            //    return;

            if (!this.m_closed)
            {
                this.RaiseAlertFatal(alertDescription, message, e);

                this.HandleFailure();
            }
        }

        /// <exception cref="IOException" />
        protected virtual void HandleFailure()
        {
            this.m_closed          = true;
            this.m_failedWithError = true;

            /*
             * RFC 2246 7.2.1. The session becomes unresumable if any connection is terminated
             * without proper close_notify messages with level equal to warning.
             */
            // TODO This isn't quite in the right place. Also, as of TLS 1.1 the above is obsolete.
            this.InvalidateSession();

            if (!this.m_appDataReady) this.CleanupHandshake();

            this.CloseConnection();
        }

        /// <exception cref="IOException" />
        protected abstract void HandleHandshakeMessage(short type, HandshakeMessageInput buf);

        /// <exception cref="IOException" />
        protected virtual void ApplyMaxFragmentLengthExtension(short maxFragmentLength)
        {
            if (maxFragmentLength >= 0)
            {
                if (!MaxFragmentLength.IsValid(maxFragmentLength))
                    throw new TlsFatalAlert(AlertDescription.internal_error);

                var plainTextLimit = 1 << (8 + maxFragmentLength);
                this.m_recordStream.SetPlaintextLimit(plainTextLimit);
            }
        }

        /// <exception cref="IOException" />
        protected virtual void CheckReceivedChangeCipherSpec(bool expected)
        {
            if (expected != this.m_receivedChangeCipherSpec)
                throw new TlsFatalAlert(AlertDescription.unexpected_message);
        }

        /// <exception cref="IOException" />
        protected virtual void BlockForHandshake()
        {
            while (this.m_connectionState != CS_END)
            {
                if (this.IsClosed)
                    // NOTE: Any close during the handshake should have raised an exception.
                    throw new TlsFatalAlert(AlertDescription.internal_error);

                this.SafeReadRecord();
            }
        }

        /// <exception cref="IOException" />
        protected virtual void BeginHandshake()
        {
            var context = this.ContextAdmin;
            var peer    = this.Peer;

            this.m_maxHandshakeMessageSize = Math.Max(1024, peer.GetMaxHandshakeMessageSize());

            this.m_handshakeHash   = new DeferredHash(context);
            this.m_connectionState = CS_START;
            this.m_resumedSession  = false;
            this.m_selectedPsk13   = false;

            context.HandshakeBeginning(peer);

            var securityParameters = context.SecurityParameters;

            securityParameters.m_extendedPadding = peer.ShouldUseExtendedPadding();
        }

        protected virtual void CleanupHandshake()
        {
            var context = this.Context;
            if (null != context)
            {
                var securityParameters = context.SecurityParameters;
                if (null != securityParameters) securityParameters.Clear();
            }

            this.m_tlsSession          = null;
            this.m_sessionParameters   = null;
            this.m_sessionMasterSecret = null;

            this.m_retryCookie      = null;
            this.m_retryGroup       = -1;
            this.m_clientExtensions = null;
            this.m_serverExtensions = null;

            this.m_resumedSession           = false;
            this.m_selectedPsk13            = false;
            this.m_receivedChangeCipherSpec = false;
            this.m_expectSessionTicket      = false;
        }

        /// <exception cref="IOException" />
        protected virtual void CompleteHandshake()
        {
            try
            {
                var context            = this.ContextAdmin;
                var securityParameters = context.SecurityParameters;

                if (!context.IsHandshaking ||
                    null == securityParameters.LocalVerifyData ||
                    null == securityParameters.PeerVerifyData)
                    throw new TlsFatalAlert(AlertDescription.internal_error);

                this.m_recordStream.FinaliseHandshake();
                this.m_connectionState = CS_END;

                // TODO Prefer to set to null, but would need guards elsewhere
                this.m_handshakeHash = new DeferredHash(context);

                this.m_alertQueue.Shrink();
                this.m_handshakeQueue.Shrink();

                var negotiatedVersion = securityParameters.NegotiatedVersion;

                this.m_appDataSplitEnabled = !TlsUtilities.IsTlsV11(negotiatedVersion);
                this.m_appDataReady        = true;

                this.m_keyUpdateEnabled = TlsUtilities.IsTlsV13(negotiatedVersion);

                if (this.m_blocking) this.m_tlsStream = new TlsStream(this);

                if (this.m_sessionParameters == null)
                {
                    this.m_sessionMasterSecret = securityParameters.MasterSecret;

                    this.m_sessionParameters = new SessionParameters.Builder()
                        .SetCipherSuite(securityParameters.CipherSuite)
                        .SetExtendedMasterSecret(securityParameters.IsExtendedMasterSecret)
                        .SetLocalCertificate(securityParameters.LocalCertificate)
                        .SetMasterSecret(context.Crypto.AdoptSecret(this.m_sessionMasterSecret))
                        .SetNegotiatedVersion(securityParameters.NegotiatedVersion)
                        .SetPeerCertificate(securityParameters.PeerCertificate)
                        .SetPskIdentity(securityParameters.PskIdentity)
                        .SetSrpIdentity(securityParameters.SrpIdentity)
                        // TODO Consider filtering extensions that aren't relevant to resumed sessions
                        .SetServerExtensions(this.m_serverExtensions)
                        .Build();

                    this.m_tlsSession = TlsUtilities.ImportSession(securityParameters.SessionID, this.m_sessionParameters);
                }
                else
                {
                    securityParameters.m_localCertificate = this.m_sessionParameters.LocalCertificate;
                    securityParameters.m_peerCertificate  = this.m_sessionParameters.PeerCertificate;
                    securityParameters.m_pskIdentity      = this.m_sessionParameters.PskIdentity;
                    securityParameters.m_srpIdentity      = this.m_sessionParameters.SrpIdentity;
                }

                context.HandshakeComplete(this.Peer, this.m_tlsSession);
            }
            finally
            {
                this.CleanupHandshake();
            }
        }

        /// <exception cref="IOException" />
        internal void ProcessRecord(short protocol, byte[] buf, int off, int len)
        {
            /*
             * Have a look at the protocol type, and add it to the correct queue.
             */
            switch (protocol)
            {
                case ContentType.alert:
                {
                    this.m_alertQueue.AddData(buf, off, len);
                    this.ProcessAlertQueue();
                    break;
                }
                case ContentType.application_data:
                {
                    if (!this.m_appDataReady)
                        throw new TlsFatalAlert(AlertDescription.unexpected_message);

                    this.m_applicationDataQueue.AddData(buf, off, len);
                    this.ProcessApplicationDataQueue();
                    break;
                }
                case ContentType.change_cipher_spec:
                {
                    this.ProcessChangeCipherSpec(buf, off, len);
                    break;
                }
                case ContentType.handshake:
                {
                    if (this.m_handshakeQueue.Available > 0)
                    {
                        this.m_handshakeQueue.AddData(buf, off, len);
                        this.ProcessHandshakeQueue(this.m_handshakeQueue);
                    }
                    else
                    {
                        var tmpQueue = new ByteQueue(buf, off, len);
                        this.ProcessHandshakeQueue(tmpQueue);
                        var remaining = tmpQueue.Available;
                        if (remaining > 0) this.m_handshakeQueue.AddData(buf, off + len - remaining, remaining);
                    }

                    break;
                }
                //case ContentType.heartbeat:
                //{
                //    if (!m_appDataReady)
                //        throw new TlsFatalAlert(AlertDescription.unexpected_message);

                //    // TODO[RFC 6520]
                //    m_heartbeatQueue.addData(buf, off, len);
                //    ProcessHeartbeatQueue();
                //    break;
                //}
                default:
                    throw new TlsFatalAlert(AlertDescription.unexpected_message);
            }
        }

        /// <exception cref="IOException" />
        private void ProcessHandshakeQueue(ByteQueue queue)
        {
            /*
             * We need the first 4 bytes, they contain type and length of the message.
             */
            while (queue.Available >= 4)
            {
                var header = queue.ReadInt32();

                var type = (short)((uint)header >> 24);
                if (!HandshakeType.IsRecognized(type))
                    throw new TlsFatalAlert(AlertDescription.unexpected_message,
                        "Handshake message of unrecognized type: " + type);

                var length = header & 0x00FFFFFF;
                if (length > this.m_maxHandshakeMessageSize)
                    throw new TlsFatalAlert(AlertDescription.internal_error,
                        "Handshake message length exceeds the maximum: " + HandshakeType.GetText(type) + ", " + length
                        + " > " + this.m_maxHandshakeMessageSize);

                var totalLength = 4 + length;
                if (queue.Available < totalLength)
                    // Not enough bytes in the buffer to read the full message.
                    break;

                /*
                 * Check ChangeCipherSpec status
                 */
                switch (type)
                {
                    case HandshakeType.hello_request:
                        break;

                    default:
                    {
                        var negotiatedVersion = this.Context.ServerVersion;
                        if (null != negotiatedVersion && TlsUtilities.IsTlsV13(negotiatedVersion))
                            break;

                        this.CheckReceivedChangeCipherSpec(HandshakeType.finished == type);
                        break;
                    }
                }

                var buf = queue.ReadHandshakeMessage(totalLength);

                switch (type)
                {
                    /*
                     * These message types aren't included in the transcript.
                     */
                    case HandshakeType.hello_request:
                    case HandshakeType.key_update:
                        break;

                    /*
                     * Not included in the transcript for (D)TLS 1.3+
                     */
                    case HandshakeType.new_session_ticket:
                    {
                        var negotiatedVersion = this.Context.ServerVersion;
                        if (null != negotiatedVersion && !TlsUtilities.IsTlsV13(negotiatedVersion)) buf.UpdateHash(this.m_handshakeHash);

                        break;
                    }

                    /*
                     * These message types are deferred to the handler to explicitly update the transcript.
                     */
                    case HandshakeType.certificate_verify:
                    case HandshakeType.client_hello:
                    case HandshakeType.finished:
                    case HandshakeType.server_hello:
                        break;

                    /*
                     * For all others we automatically update the transcript immediately. 
                     */
                    default:
                    {
                        buf.UpdateHash(this.m_handshakeHash);
                        break;
                    }
                }

                buf.Seek(4L, SeekOrigin.Current);

                this.HandleHandshakeMessage(type, buf);
            }
        }

        private void ProcessApplicationDataQueue()
        {
            /*
             * There is nothing we need to do here.
             * 
             * This function could be used for callbacks when application data arrives in the future.
             */
        }

        /// <exception cref="IOException" />
        private void ProcessAlertQueue()
        {
            while (this.m_alertQueue.Available >= 2)
            {
                /*
                 * An alert is always 2 bytes. Read the alert.
                 */
                var   alert            = this.m_alertQueue.RemoveData(2, 0);
                short alertLevel       = alert[0];
                short alertDescription = alert[1];

                this.HandleAlertMessage(alertLevel, alertDescription);
            }
        }

        /// <summary>This method is called, when a change cipher spec message is received.</summary>
        /// <exception cref="IOException">
        ///     If the message has an invalid content or the handshake is not in the correct
        ///     state.
        /// </exception>
        private void ProcessChangeCipherSpec(byte[] buf, int off, int len)
        {
            var negotiatedVersion = this.Context.ServerVersion;
            if (null == negotiatedVersion || TlsUtilities.IsTlsV13(negotiatedVersion))
                // See RFC 8446 D.4.
                throw new TlsFatalAlert(AlertDescription.unexpected_message);

            for (var i = 0; i < len; ++i)
            {
                var message = TlsUtilities.ReadUint8(buf, off + i);

                if (message != ChangeCipherSpec.change_cipher_spec)
                    throw new TlsFatalAlert(AlertDescription.decode_error);

                if (this.m_receivedChangeCipherSpec
                    || this.m_alertQueue.Available > 0
                    || this.m_handshakeQueue.Available > 0)
                    throw new TlsFatalAlert(AlertDescription.unexpected_message);

                this.m_recordStream.NotifyChangeCipherSpecReceived();

                this.m_receivedChangeCipherSpec = true;

                this.HandleChangeCipherSpecMessage();
            }
        }

        public virtual int ApplicationDataAvailable => this.m_applicationDataQueue.Available;

        /// <summary>Read data from the network.</summary>
        /// <remarks>
        ///     The method will return immediately, if there is still some data left in the buffer, or block until some
        ///     application data has been read from the network.
        /// </remarks>
        /// <param name="buf">The buffer where the data will be copied to.</param>
        /// <param name="off">The position where the data will be placed in the buffer.</param>
        /// <param name="len">The maximum number of bytes to read.</param>
        /// <returns>The number of bytes read.</returns>
        /// <exception cref="IOException">If something goes wrong during reading data.</exception>
        public virtual int ReadApplicationData(byte[] buf, int off, int len)
        {
            if (len < 1)
                return 0;

            while (this.m_applicationDataQueue.Available == 0)
            {
                if (this.m_closed)
                {
                    if (this.m_failedWithError)
                        throw new IOException("Cannot read application data on failed TLS connection");

                    return -1;
                }

                if (!this.m_appDataReady)
                    throw new InvalidOperationException("Cannot read application data until initial handshake completed.");

                /*
                 * NOTE: Only called more than once when empty records are received, so no special
                 * InterruptedIOException handling is necessary.
                 */
                this.SafeReadRecord();
            }

            len = Math.Min(len, this.m_applicationDataQueue.Available);
            this.m_applicationDataQueue.RemoveData(buf, off, len, 0);
            return len;
        }

        /// <exception cref="IOException" />
        protected virtual RecordPreview SafePreviewRecordHeader(byte[] recordHeader)
        {
            try
            {
                return this.m_recordStream.PreviewRecordHeader(recordHeader);
            }
            catch (TlsFatalAlert e)
            {
                this.HandleException(e.AlertDescription, "Failed to read record", e);
                throw e;
            }
            catch (IOException e)
            {
                this.HandleException(AlertDescription.internal_error, "Failed to read record", e);
                throw e;
            }
            catch (Exception e)
            {
                this.HandleException(AlertDescription.internal_error, "Failed to read record", e);
                throw new TlsFatalAlert(AlertDescription.internal_error, e);
            }
        }

        /// <exception cref="IOException" />
        protected virtual void SafeReadRecord()
        {
            try
            {
                if (this.m_recordStream.ReadRecord())
                    return;

                if (!this.m_appDataReady)
                    throw new TlsFatalAlert(AlertDescription.handshake_failure);

                if (!this.Peer.RequiresCloseNotify())
                {
                    this.HandleClose(false);
                    return;
                }
            }
            catch (TlsFatalAlertReceived e)
            {
                // Connection failure already handled at source
                throw e;
            }
            catch (TlsFatalAlert e)
            {
                this.HandleException(e.AlertDescription, "Failed to read record", e);
                throw e;
            }
            catch (IOException e)
            {
                this.HandleException(AlertDescription.internal_error, "Failed to read record", e);
                throw e;
            }
            catch (Exception e)
            {
                this.HandleException(AlertDescription.internal_error, "Failed to read record", e);
                throw new TlsFatalAlert(AlertDescription.internal_error, e);
            }

            this.HandleFailure();

            throw new TlsNoCloseNotifyException();
        }

        /// <exception cref="IOException" />
        protected virtual bool SafeReadFullRecord(byte[] input, int inputOff, int inputLen)
        {
            try
            {
                return this.m_recordStream.ReadFullRecord(input, inputOff, inputLen);
            }
            catch (TlsFatalAlert e)
            {
                this.HandleException(e.AlertDescription, "Failed to process record", e);
                throw e;
            }
            catch (IOException e)
            {
                this.HandleException(AlertDescription.internal_error, "Failed to process record", e);
                throw e;
            }
            catch (Exception e)
            {
                this.HandleException(AlertDescription.internal_error, "Failed to process record", e);
                throw new TlsFatalAlert(AlertDescription.internal_error, e);
            }
        }

        /// <exception cref="IOException" />
        protected virtual void SafeWriteRecord(short type, byte[] buf, int offset, int len)
        {
            try
            {
                this.m_recordStream.WriteRecord(type, buf, offset, len);
            }
            catch (TlsFatalAlert e)
            {
                this.HandleException(e.AlertDescription, "Failed to write record", e);
                throw e;
            }
            catch (IOException e)
            {
                this.HandleException(AlertDescription.internal_error, "Failed to write record", e);
                throw e;
            }
            catch (Exception e)
            {
                this.HandleException(AlertDescription.internal_error, "Failed to write record", e);
                throw new TlsFatalAlert(AlertDescription.internal_error, e);
            }
        }

        /// <summary>Write some application data.</summary>
        /// <remarks>
        ///     Fragmentation is handled internally. Usable in both blocking/non-blocking modes.<br /><br />
        ///     In blocking mode, the output will be automatically sent via the underlying transport. In non-blocking mode,
        ///     call <see cref="ReadOutput(byte[], int, int)" /> to get the output bytes to send to the peer.<br /><br />
        ///     This method must not be called until after the initial handshake is complete. Attempting to call it earlier
        ///     will result in an <see cref="InvalidOperationException" />.
        /// </remarks>
        /// <param name="buf">The buffer containing application data to send.</param>
        /// <param name="off">The offset at which the application data begins</param>
        /// <param name="len">The number of bytes of application data.</param>
        /// <exception cref="InvalidOperationException">
        ///     If called before the initial handshake has completed.
        /// </exception>
        /// <exception cref="IOException">
        ///     If connection is already closed, or for encryption or transport errors.
        /// </exception>
        public virtual void WriteApplicationData(byte[] buf, int off, int len)
        {
            if (!this.m_appDataReady)
                throw new InvalidOperationException(
                    "Cannot write application data until initial handshake completed.");

            lock (this.m_recordWriteLock)
            {
                while (len > 0)
                {
                    if (this.m_closed)
                        throw new IOException("Cannot write application data on closed/failed TLS connection");

                    /*
                     * RFC 5246 6.2.1. Zero-length fragments of Application data MAY be sent as they are
                     * potentially useful as a traffic analysis countermeasure.
                     * 
                     * NOTE: Actually, implementations appear to have settled on 1/n-1 record splitting.
                     */
                    if (this.m_appDataSplitEnabled)
                    {
                        /*
                         * Protect against known IV attack!
                         * 
                         * DO NOT REMOVE THIS CODE, EXCEPT YOU KNOW EXACTLY WHAT YOU ARE DOING HERE.
                         */
                        switch (this.m_appDataSplitMode)
                        {
                            case ADS_MODE_0_N_FIRSTONLY:
                            {
                                this.m_appDataSplitEnabled = false;
                                this.SafeWriteRecord(ContentType.application_data, TlsUtilities.EmptyBytes, 0, 0);
                                break;
                            }
                            case ADS_MODE_0_N:
                            {
                                this.SafeWriteRecord(ContentType.application_data, TlsUtilities.EmptyBytes, 0, 0);
                                break;
                            }
                            case ADS_MODE_1_Nsub1:
                            default:
                            {
                                if (len > 1)
                                {
                                    this.SafeWriteRecord(ContentType.application_data, buf, off, 1);
                                    ++off;
                                    --len;
                                }

                                break;
                            }
                        }
                    }
                    else if (this.m_keyUpdateEnabled)
                    {
                        if (this.m_keyUpdatePendingSend)
                            this.Send13KeyUpdate(false);
                        else if (this.m_recordStream.NeedsKeyUpdate()) this.Send13KeyUpdate(true);
                    }

                    // Fragment data according to the current fragment limit.
                    var toWrite = Math.Min(len, this.m_recordStream.PlaintextLimit);
                    this.SafeWriteRecord(ContentType.application_data, buf, off, toWrite);
                    off += toWrite;
                    len -= toWrite;
                }
            }
        }

        public virtual int AppDataSplitMode
        {
            get => this.m_appDataSplitMode;
            set
            {
                if (value < ADS_MODE_1_Nsub1 || value > ADS_MODE_0_N_FIRSTONLY)
                    throw new InvalidOperationException("Illegal appDataSplitMode mode: " + value);

                this.m_appDataSplitMode = value;
            }
        }

        public virtual bool IsResumableHandshake { get => this.m_resumableHandshake; set => this.m_resumableHandshake = value; }

        /// <exception cref="IOException" />
        internal void WriteHandshakeMessage(byte[] buf, int off, int len)
        {
            if (len < 4)
                throw new TlsFatalAlert(AlertDescription.internal_error);

            var type = TlsUtilities.ReadUint8(buf, off);
            switch (type)
            {
                /*
                 * These message types aren't included in the transcript.
                 */
                case HandshakeType.hello_request:
                case HandshakeType.key_update:
                    break;

                /*
                 * Not included in the transcript for (D)TLS 1.3+
                 */
                case HandshakeType.new_session_ticket:
                {
                    var negotiatedVersion = this.Context.ServerVersion;
                    if (null != negotiatedVersion && !TlsUtilities.IsTlsV13(negotiatedVersion)) this.m_handshakeHash.Update(buf, off, len);

                    break;
                }

                /*
                 * These message types are deferred to the writer to explicitly update the transcript.
                 */
                case HandshakeType.client_hello:
                    break;

                /*
                 * For all others we automatically update the transcript. 
                 */
                default:
                {
                    this.m_handshakeHash.Update(buf, off, len);
                    break;
                }
            }

            var total = 0;
            do
            {
                // Fragment data according to the current fragment limit.
                var toWrite = Math.Min(len - total, this.m_recordStream.PlaintextLimit);
                this.SafeWriteRecord(ContentType.handshake, buf, off + total, toWrite);
                total += toWrite;
            } while (total < len);
        }

        /// <summary>The secure bidirectional stream for this connection</summary>
        /// <remarks>Only allowed in blocking mode.</remarks>
        public virtual Stream Stream
        {
            get
            {
                if (!this.m_blocking)
                    throw new InvalidOperationException(
                        "Cannot use Stream in non-blocking mode! Use OfferInput()/OfferOutput() instead.");

                return this.m_tlsStream;
            }
        }

        /// <summary>Should be called in non-blocking mode when the input data reaches EOF.</summary>
        /// <exception cref="IOException" />
        public virtual void CloseInput()
        {
            if (this.m_blocking)
                throw new InvalidOperationException("Cannot use CloseInput() in blocking mode!");

            if (this.m_closed)
                return;

            if (this.m_inputBuffers.Available > 0)
                throw new EndOfStreamException();

            if (!this.m_appDataReady)
                throw new TlsFatalAlert(AlertDescription.handshake_failure);

            if (!this.Peer.RequiresCloseNotify())
            {
                this.HandleClose(false);
                return;
            }

            this.HandleFailure();

            throw new TlsNoCloseNotifyException();
        }

        /// <exception cref="IOException" />
        public virtual RecordPreview PreviewInputRecord(byte[] recordHeader)
        {
            if (this.m_blocking)
                throw new InvalidOperationException("Cannot use PreviewInputRecord() in blocking mode!");
            if (this.m_inputBuffers.Available != 0)
                throw new InvalidOperationException("Can only use PreviewInputRecord() for record-aligned input.");
            if (this.m_closed)
                throw new IOException("Connection is closed, cannot accept any more input");

            return this.SafePreviewRecordHeader(recordHeader);
        }

        /// <exception cref="IOException" />
        public virtual RecordPreview PreviewOutputRecord(int applicationDataSize)
        {
            if (!this.m_appDataReady)
                throw new InvalidOperationException(
                    "Cannot use PreviewOutputRecord() until initial handshake completed.");
            if (this.m_blocking)
                throw new InvalidOperationException("Cannot use PreviewOutputRecord() in blocking mode!");
            if (this.m_outputBuffer.Buffer.Available != 0)
                throw new InvalidOperationException("Can only use PreviewOutputRecord() for record-aligned output.");
            if (this.m_closed)
                throw new IOException("Connection is closed, cannot produce any more output");

            if (applicationDataSize < 1)
                return new RecordPreview(0, 0);

            if (this.m_appDataSplitEnabled)
                switch (this.m_appDataSplitMode)
                {
                    case ADS_MODE_0_N_FIRSTONLY:
                    case ADS_MODE_0_N:
                    {
                        var a = this.m_recordStream.PreviewOutputRecord(0);
                        var b = this.m_recordStream.PreviewOutputRecord(applicationDataSize);
                        return RecordPreview.CombineAppData(a, b);
                    }
                    case ADS_MODE_1_Nsub1:
                    default:
                    {
                        var a = this.m_recordStream.PreviewOutputRecord(1);
                        if (applicationDataSize > 1)
                        {
                            var b = this.m_recordStream.PreviewOutputRecord(applicationDataSize - 1);
                            a = RecordPreview.CombineAppData(a, b);
                        }

                        return a;
                    }
                }

            {
                var a = this.m_recordStream.PreviewOutputRecord(applicationDataSize);
                if (this.m_keyUpdateEnabled && (this.m_keyUpdatePendingSend || this.m_recordStream.NeedsKeyUpdate()))
                {
                    var keyUpdateLength = HandshakeMessageOutput.GetLength(1);
                    var recordSize      = this.m_recordStream.PreviewOutputRecordSize(keyUpdateLength);
                    a = RecordPreview.ExtendRecordSize(a, recordSize);
                }

                return a;
            }
        }

        /// <summary>Equivalent to <code>OfferInput(input, 0, input.Length)</code>.</summary>
        /// <param name="input">The input buffer to offer.</param>
        /// <exception cref="IOException" />
        /// <seealso cref="OfferInput(byte[], int, int)" />
        public virtual void OfferInput(byte[] input) { this.OfferInput(input, 0, input.Length); }

        /// <summary>Offer input from an arbitrary source.</summary>
        /// <remarks>
        ///     Only allowed in non-blocking mode.<br /><br />
        ///     This method will decrypt and process all records that are fully available. If only part of a record is
        ///     available, the buffer will be retained until the remainder of the record is offered.<br /><br />
        ///     If any records containing application data were processed, the decrypted data can be obtained using
        ///     <see cref="ReadInput(byte[], int, int)" />. If any records containing protocol data were processed, a
        ///     response may have been generated. You should always check to see if there is any available output after
        ///     calling this method by calling <see cref="GetAvailableOutputBytes" />.
        /// </remarks>
        /// <param name="input">The input buffer to offer.</param>
        /// <param name="inputOff">The offset within the input buffer that input begins.</param>
        /// <param name="inputLen">The number of bytes of input being offered.</param>
        /// <exception cref="IOException">If an error occurs while decrypting or processing a record.</exception>
        public virtual void OfferInput(byte[] input, int inputOff, int inputLen)
        {
            if (this.m_blocking)
                throw new InvalidOperationException("Cannot use OfferInput() in blocking mode! Use Stream instead.");
            if (this.m_closed)
                throw new IOException("Connection is closed, cannot accept any more input");

            // Fast path if the input is arriving one record at a time
            if (this.m_inputBuffers.Available == 0 && this.SafeReadFullRecord(input, inputOff, inputLen))
            {
                if (this.m_closed)
                    if (!this.m_appDataReady)
                        // NOTE: Any close during the handshake should have raised an exception.
                        throw new TlsFatalAlert(AlertDescription.internal_error);
                return;
            }

            this.m_inputBuffers.AddBytes(input, inputOff, inputLen);

            // loop while there are enough bytes to read the length of the next record
            while (this.m_inputBuffers.Available >= RecordFormat.FragmentOffset)
            {
                var recordHeader = new byte[RecordFormat.FragmentOffset];
                if (RecordFormat.FragmentOffset != this.m_inputBuffers.Peek(recordHeader))
                    throw new TlsFatalAlert(AlertDescription.internal_error);

                var preview = this.SafePreviewRecordHeader(recordHeader);
                if (this.m_inputBuffers.Available < preview.RecordSize)
                    // not enough bytes to read a whole record
                    break;

                // NOTE: This is actually reading from inputBuffers, so InterruptedIOException shouldn't be possible
                this.SafeReadRecord();

                if (this.m_closed)
                {
                    if (!this.m_appDataReady)
                        // NOTE: Any close during the handshake should have raised an exception.
                        throw new TlsFatalAlert(AlertDescription.internal_error);
                    break;
                }
            }
        }

        public virtual int ApplicationDataLimit => this.m_recordStream.PlaintextLimit;

        /// <summary>Gets the amount of received application data.</summary>
        /// <remarks>
        ///     A call to <see cref="readInput(byte[], int, int)" /> is guaranteed to be able to return at least
        ///     this much data.<br /><br />
        ///     Only allowed in non-blocking mode.
        /// </remarks>
        /// <returns>The number of bytes of available application data.</returns>
        public virtual int GetAvailableInputBytes()
        {
            if (this.m_blocking)
                throw new InvalidOperationException("Cannot use GetAvailableInputBytes() in blocking mode!");

            return this.ApplicationDataAvailable;
        }

        /// <summary>Retrieves received application data.</summary>
        /// <remarks>
        ///     Use <see cref="GetAvailableInputBytes" /> to check how much application data is currently available. This
        ///     method functions similarly to <see cref="Stream.Read(byte[], int, int)" />, except that it never blocks. If
        ///     no data is available, nothing will be copied and zero will be returned.<br /><br />
        ///     Only allowed in non-blocking mode.
        /// </remarks>
        /// <param name="buf">The buffer to hold the application data.</param>
        /// <param name="off">The start offset in the buffer at which the data is written.</param>
        /// <param name="len">The maximum number of bytes to read.</param>
        /// <returns>
        ///     The total number of bytes copied to the buffer. May be less than the length specified if the
        ///     length was greater than the amount of available data.
        /// </returns>
        public virtual int ReadInput(byte[] buf, int off, int len)
        {
            if (this.m_blocking)
                throw new InvalidOperationException("Cannot use ReadInput() in blocking mode! Use Stream instead.");

            len = Math.Min(len, this.ApplicationDataAvailable);
            if (len < 1)
                return 0;

            this.m_applicationDataQueue.RemoveData(buf, off, len, 0);
            return len;
        }

        /// <summary>Gets the amount of encrypted data available to be sent.</summary>
        /// <remarks>
        ///     A call to <see cref="ReadOutput(byte[], int, int)" /> is guaranteed to be able to return at least this much
        ///     data. Only allowed in non-blocking mode.
        /// </remarks>
        /// <returns>The number of bytes of available encrypted data.</returns>
        public virtual int GetAvailableOutputBytes()
        {
            if (this.m_blocking)
                throw new InvalidOperationException("Cannot use GetAvailableOutputBytes() in blocking mode! Use Stream instead.");

            return this.m_outputBuffer.Buffer.Available;
        }

        /// <summary>Retrieves encrypted data to be sent.</summary>
        /// <remarks>
        ///     Use <see cref="GetAvailableOutputBytes" /> to check how much encrypted data is currently available. This
        ///     method functions similarly to <see cref="Stream.Read(byte[], int, int)" />, except that it never blocks. If
        ///     no data is available, nothing will be copied and zero will be returned. Only allowed in non-blocking mode.
        /// </remarks>
        /// <param name="buffer">The buffer to hold the encrypted data.</param>
        /// <param name="offset">The start offset in the buffer at which the data is written.</param>
        /// <param name="length">The maximum number of bytes to read.</param>
        /// <returns>
        ///     The total number of bytes copied to the buffer. May be less than the length specified if the
        ///     length was greater than the amount of available data.
        /// </returns>
        public virtual int ReadOutput(byte[] buffer, int offset, int length)
        {
            if (this.m_blocking)
                throw new InvalidOperationException("Cannot use ReadOutput() in blocking mode! Use 'Stream() instead.");

            var bytesToRead = Math.Min(this.GetAvailableOutputBytes(), length);
            this.m_outputBuffer.Buffer.RemoveData(buffer, offset, bytesToRead, 0);
            return bytesToRead;
        }

        protected virtual bool EstablishSession(TlsSession sessionToResume)
        {
            this.m_tlsSession          = null;
            this.m_sessionParameters   = null;
            this.m_sessionMasterSecret = null;

            if (null == sessionToResume || !sessionToResume.IsResumable)
                return false;

            var sessionParameters = sessionToResume.ExportSessionParameters();
            if (null == sessionParameters)
                return false;

            if (!sessionParameters.IsExtendedMasterSecret)
            {
                var peer = this.Peer;
                if (!peer.AllowLegacyResumption() || peer.RequiresExtendedMasterSecret())
                    return false;

                /*
                 * NOTE: For session resumption without extended_master_secret, renegotiation MUST be
                 * disabled (see RFC 7627 5.4). We currently do not implement renegotiation and it is
                 * unlikely we ever would since it was removed in TLS 1.3.
                 */
            }

            var sessionMasterSecret = TlsUtilities.GetSessionMasterSecret(this.Context.Crypto,
                sessionParameters.MasterSecret);
            if (null == sessionMasterSecret)
                return false;

            this.m_tlsSession          = sessionToResume;
            this.m_sessionParameters   = sessionParameters;
            this.m_sessionMasterSecret = sessionMasterSecret;

            return true;
        }

        protected virtual void InvalidateSession()
        {
            if (this.m_sessionMasterSecret != null)
            {
                this.m_sessionMasterSecret.Destroy();
                this.m_sessionMasterSecret = null;
            }

            if (this.m_sessionParameters != null)
            {
                this.m_sessionParameters.Clear();
                this.m_sessionParameters = null;
            }

            if (this.m_tlsSession != null)
            {
                this.m_tlsSession.Invalidate();
                this.m_tlsSession = null;
            }
        }

        /// <exception cref="IOException" />
        protected virtual void ProcessFinishedMessage(MemoryStream buf)
        {
            var context            = this.Context;
            var securityParameters = context.SecurityParameters;
            var isServerContext    = context.IsServer;

            var verify_data = TlsUtilities.ReadFully(securityParameters.VerifyDataLength, buf);

            AssertEmpty(buf);

            var expected_verify_data = TlsUtilities.CalculateVerifyData(context, this.m_handshakeHash, !isServerContext);

            /*
             * Compare both checksums.
             */
            if (!Arrays.ConstantTimeAreEqual(expected_verify_data, verify_data))
                /*
                     * Wrong checksum in the finished message.
                     */
                throw new TlsFatalAlert(AlertDescription.decrypt_error);

            securityParameters.m_peerVerifyData = expected_verify_data;

            if (!this.m_resumedSession || securityParameters.IsExtendedMasterSecret)
                if (null == securityParameters.LocalVerifyData)
                    securityParameters.m_tlsUnique = expected_verify_data;
        }

        /// <exception cref="IOException" />
        protected virtual void Process13FinishedMessage(MemoryStream buf)
        {
            var context            = this.Context;
            var securityParameters = context.SecurityParameters;
            var isServerContext    = context.IsServer;

            var verify_data = TlsUtilities.ReadFully(securityParameters.VerifyDataLength, buf);

            AssertEmpty(buf);

            var expected_verify_data = TlsUtilities.CalculateVerifyData(context, this.m_handshakeHash, !isServerContext);

            /*
             * Compare both checksums.
             */
            if (!Arrays.ConstantTimeAreEqual(expected_verify_data, verify_data))
                /*
                     * Wrong checksum in the finished message.
                     */
                    throw new TlsFatalAlert(AlertDescription.decrypt_error);

                securityParameters.m_peerVerifyData = expected_verify_data;
                securityParameters.m_tlsUnique      = null;
                }

        /// <exception cref="IOException" />
        protected virtual void RaiseAlertFatal(short alertDescription, string message, Exception cause)
                {
                    this.Peer.NotifyAlertRaised(AlertLevel.fatal, alertDescription, message, cause);

                    byte[] alert = { (byte)AlertLevel.fatal, (byte)alertDescription };

                    try
                    {
                        this.m_recordStream.WriteRecord(ContentType.alert, alert, 0, 2);
                    }
                    catch (Exception)
                    {
                        // We are already processing an exception, so just ignore this
                    }
                }

        /// <exception cref="IOException" />
        protected virtual void RaiseAlertWarning(short alertDescription, string message)
                {
                    this.Peer.NotifyAlertRaised(AlertLevel.warning, alertDescription, message, null);

                    byte[] alert = { (byte)AlertLevel.warning, (byte)alertDescription };

                    this.SafeWriteRecord(ContentType.alert, alert, 0, 2);
                }


        /// <exception cref="IOException" />
        protected virtual void Receive13KeyUpdate(MemoryStream buf)
                {
                    // TODO[tls13] This is interesting enough to notify the TlsPeer for possible logging/vetting

                    if (!(this.m_appDataReady && this.m_keyUpdateEnabled))
                        throw new TlsFatalAlert(AlertDescription.unexpected_message);

                    var requestUpdate = TlsUtilities.ReadUint8(buf);

                    AssertEmpty(buf);

                    if (!KeyUpdateRequest.IsValid(requestUpdate))
                        throw new TlsFatalAlert(AlertDescription.illegal_parameter);

                    var updateRequested = KeyUpdateRequest.update_requested == requestUpdate;

                    TlsUtilities.Update13TrafficSecretPeer(this.Context);
                    this.m_recordStream.NotifyKeyUpdateReceived();

                    //this.m_keyUpdatePendingReceive &= updateRequested;
                    this.m_keyUpdatePendingSend |= updateRequested;
                }

        /// <exception cref="IOException" />
        protected virtual void SendCertificateMessage(Certificate certificate, Stream endPointHash)
                {
                    var context            = this.Context;
                    var securityParameters = context.SecurityParameters;
                    if (null != securityParameters.LocalCertificate)
                        throw new TlsFatalAlert(AlertDescription.internal_error);

                    if (null == certificate) certificate = Certificate.EmptyChain;

                    if (certificate.IsEmpty && !context.IsServer && securityParameters.NegotiatedVersion.IsSsl)
                    {
                        var message = "SSLv3 client didn't provide credentials";
                        this.RaiseAlertWarning(AlertDescription.no_certificate, message);
                    }
                    else
                    {
                        var message = new HandshakeMessageOutput(HandshakeType.certificate);
                        certificate.Encode(context, message, endPointHash);
                        message.Send(this);
                    }

                    securityParameters.m_localCertificate = certificate;
                }

        /// <exception cref="IOException" />
        protected virtual void Send13CertificateMessage(Certificate certificate)
                {
                    if (null == certificate)
                        throw new TlsFatalAlert(AlertDescription.internal_error);

                    var context            = this.Context;
                    var securityParameters = context.SecurityParameters;
                    if (null != securityParameters.LocalCertificate)
                        throw new TlsFatalAlert(AlertDescription.internal_error);

                    var message = new HandshakeMessageOutput(HandshakeType.certificate);
                    certificate.Encode(context, message, null);
                    message.Send(this);

                    securityParameters.m_localCertificate = certificate;
                }

        /// <exception cref="IOException" />
        protected virtual void Send13CertificateVerifyMessage(DigitallySigned certificateVerify)
                {
                    var message = new HandshakeMessageOutput(HandshakeType.certificate_verify);
                    certificateVerify.Encode(message);
                    message.Send(this);
                }

        /// <exception cref="IOException" />
        protected virtual void SendChangeCipherSpec()
                {
                    this.SendChangeCipherSpecMessage();
                    this.m_recordStream.EnablePendingCipherWrite();
                }

        /// <exception cref="IOException" />
        protected virtual void SendChangeCipherSpecMessage()
                {
                    byte[] message = { 1 };
                    this.SafeWriteRecord(ContentType.change_cipher_spec, message, 0, message.Length);
                }

        /// <exception cref="IOException" />
        protected virtual void SendFinishedMessage()
                {
                    var context            = this.Context;
                    var securityParameters = context.SecurityParameters;
                    var isServerContext    = context.IsServer;

                    var verify_data = TlsUtilities.CalculateVerifyData(context, this.m_handshakeHash, isServerContext);

                    securityParameters.m_localVerifyData = verify_data;

                    if (!this.m_resumedSession || securityParameters.IsExtendedMasterSecret)
                        if (null == securityParameters.PeerVerifyData)
                            securityParameters.m_tlsUnique = verify_data;

                    HandshakeMessageOutput.Send(this, HandshakeType.finished, verify_data);
                }

        /// <exception cref="IOException" />
        protected virtual void Send13FinishedMessage()
                {
                    var context            = this.Context;
                    var securityParameters = context.SecurityParameters;
                    var isServerContext    = context.IsServer;

                    var verify_data = TlsUtilities.CalculateVerifyData(context, this.m_handshakeHash, isServerContext);

                    securityParameters.m_localVerifyData = verify_data;
                    securityParameters.m_tlsUnique       = null;

                    HandshakeMessageOutput.Send(this, HandshakeType.finished, verify_data);
                }

        /// <exception cref="IOException" />
        protected virtual void Send13KeyUpdate(bool updateRequested)
                {
                    // TODO[tls13] This is interesting enough to notify the TlsPeer for possible logging/vetting

                    if (!(this.m_appDataReady && this.m_keyUpdateEnabled))
                        throw new TlsFatalAlert(AlertDescription.internal_error);

                    var requestUpdate = updateRequested
                        ? KeyUpdateRequest.update_requested
                        : KeyUpdateRequest.update_not_requested;

                    HandshakeMessageOutput.Send(this, HandshakeType.key_update, TlsUtilities.EncodeUint8(requestUpdate));

                    TlsUtilities.Update13TrafficSecretLocal(this.Context);
                    this.m_recordStream.NotifyKeyUpdateSent();

                    //this.m_keyUpdatePendingReceive |= updateRequested;
                    this.m_keyUpdatePendingSend &= updateRequested;
                }

        /// <exception cref="IOException" />
        protected virtual void SendSupplementalDataMessage(IList supplementalData)
                {
                    var message = new HandshakeMessageOutput(HandshakeType.supplemental_data);
                    WriteSupplementalData(message, supplementalData);
                    message.Send(this);
                }

                public virtual void Close() { this.HandleClose(true); }

                public virtual void Flush() { }

                internal bool IsApplicationDataReady => this.m_appDataReady;

                public virtual bool IsClosed => this.m_closed;

                public virtual bool IsConnected
                {
                    get
                    {
                        if (this.m_closed)
                            return false;

                        var context = this.ContextAdmin;

                        return null != context && context.IsConnected;
                    }
                }

                public virtual bool IsHandshaking
                {
                    get
                    {
                        if (this.m_closed)
                            return false;

                        var context = this.ContextAdmin;

                        return null != context && context.IsHandshaking;
                    }
                }

                /// <exception cref="IOException" />
                protected virtual short ProcessMaxFragmentLengthExtension(IDictionary clientExtensions,
                    IDictionary serverExtensions, short alertDescription)
                {
                    var maxFragmentLength = TlsExtensionsUtilities.GetMaxFragmentLengthExtension(serverExtensions);
                    if (maxFragmentLength >= 0)
                        if (!MaxFragmentLength.IsValid(maxFragmentLength)
                            || !this.m_resumedSession &&
                            maxFragmentLength != TlsExtensionsUtilities.GetMaxFragmentLengthExtension(clientExtensions))
                            throw new TlsFatalAlert(alertDescription);
                    return maxFragmentLength;
                }

                /// <exception cref="IOException" />
                protected virtual void RefuseRenegotiation()
                {
                    /*
                     * RFC 5746 4.5 SSLv3 clients [..] SHOULD use a fatal handshake_failure alert.
                     */
                    if (TlsUtilities.IsSsl(this.Context))
                        throw new TlsFatalAlert(AlertDescription.handshake_failure);

                    this.RaiseAlertWarning(AlertDescription.no_renegotiation, "Renegotiation not supported");
                }

                /// <summary>Make sure the <see cref="Stream" /> 'buf' is now empty. Fail otherwise.</summary>
                /// <param name="buf">The <see cref="Stream" /> to check.</param>
                /// <exception cref="IOException" />
                internal static void AssertEmpty(MemoryStream buf)
                {
                    if (buf.Position < buf.Length)
                        throw new TlsFatalAlert(AlertDescription.decode_error);
                }

                internal static byte[] CreateRandomBlock(bool useGmtUnixTime, TlsContext context)
                {
                    var result = context.NonceGenerator.GenerateNonce(32);

                    if (useGmtUnixTime) TlsUtilities.WriteGmtUnixTime(result, 0);

                    return result;
                }

                /// <exception cref="IOException" />
                internal static byte[] CreateRenegotiationInfo(byte[] renegotiated_connection) { return TlsUtilities.EncodeOpaque8(renegotiated_connection); }

                /// <exception cref="IOException" />
                internal static void EstablishMasterSecret(TlsContext context, TlsKeyExchange keyExchange)
                {
                    var preMasterSecret = keyExchange.GeneratePreMasterSecret();
                    if (preMasterSecret == null)
                        throw new TlsFatalAlert(AlertDescription.internal_error);

                    try
                    {
                        context.SecurityParameters.m_masterSecret = TlsUtilities.CalculateMasterSecret(context,
                            preMasterSecret);
                    }
                    finally
                    {
                        /*
                         * RFC 2246 8.1. The pre_master_secret should be deleted from memory once the
                         * master_secret has been computed.
                         */
                        preMasterSecret.Destroy();
                    }
                }

                /// <exception cref="IOException" />
                internal static IDictionary ReadExtensions(MemoryStream input)
                {
                    if (input.Position >= input.Length)
                        return null;

                    var extBytes = TlsUtilities.ReadOpaque16(input);

                    AssertEmpty(input);

                    return ReadExtensionsData(extBytes);
                }

                /// <exception cref="IOException" />
                internal static IDictionary ReadExtensionsData(byte[] extBytes)
                {
                    // Int32 -> byte[]
                    var extensions = Platform.CreateHashtable();

                    if (extBytes.Length > 0)
                    {
                        var buf = new MemoryStream(extBytes, false);

                        do
                        {
                            var extension_type = TlsUtilities.ReadUint16(buf);
                            var extension_data = TlsUtilities.ReadOpaque16(buf);

                            /*
                             * RFC 3546 2.3 There MUST NOT be more than one extension of the same type.
                             */
                            var key = extension_type;
                            if (extensions.Contains(key))
                                throw new TlsFatalAlert(AlertDescription.illegal_parameter,
                                    "Repeated extension: " + ExtensionType.GetText(extension_type));

                            extensions.Add(key, extension_data);
                        } while (buf.Position < buf.Length);
                    }

                    return extensions;
                }

                /// <exception cref="IOException" />
                internal static IDictionary ReadExtensionsData13(int handshakeType, byte[] extBytes)
                {
                    // Int32 -> byte[]
                    var extensions = Platform.CreateHashtable();

                    if (extBytes.Length > 0)
                    {
                        var buf = new MemoryStream(extBytes, false);

                        do
                        {
                            var extension_type = TlsUtilities.ReadUint16(buf);

                            if (!TlsUtilities.IsPermittedExtensionType13(handshakeType, extension_type))
                                throw new TlsFatalAlert(AlertDescription.illegal_parameter,
                                    "Invalid extension: " + ExtensionType.GetText(extension_type));

                            var extension_data = TlsUtilities.ReadOpaque16(buf);

                            /*
                             * RFC 3546 2.3 There MUST NOT be more than one extension of the same type.
                             */
                            var key = extension_type;
                            if (extensions.Contains(key))
                                throw new TlsFatalAlert(AlertDescription.illegal_parameter,
                                    "Repeated extension: " + ExtensionType.GetText(extension_type));

                            extensions.Add(key, extension_data);
                        } while (buf.Position < buf.Length);
                    }

                    return extensions;
                }

                /// <exception cref="IOException" />
                internal static IDictionary ReadExtensionsDataClientHello(byte[] extBytes)
                {
                    /*
                     * TODO[tls13] We are currently allowing any extensions to appear in ClientHello. It is
                     * somewhat complicated to restrict what can appear based on the specific set of versions
                     * the client is offering, and anyway could be fragile since clients may take a
                     * "kitchen sink" approach to adding extensions independently of the offered versions.
                     */

                    // Int32 -> byte[]
                    var extensions = Platform.CreateHashtable();

                    if (extBytes.Length > 0)
                    {
                        var buf = new MemoryStream(extBytes, false);

                        int extension_type;
                        var pre_shared_key_found = false;

                        do
                        {
                            extension_type = TlsUtilities.ReadUint16(buf);
                            var extension_data = TlsUtilities.ReadOpaque16(buf);

                            /*
                             * RFC 3546 2.3 There MUST NOT be more than one extension of the same type.
                             */
                            var key = extension_type;
                            if (extensions.Contains(key))
                                throw new TlsFatalAlert(AlertDescription.illegal_parameter,
                                    "Repeated extension: " + ExtensionType.GetText(extension_type));

                            extensions.Add(key, extension_data);

                            pre_shared_key_found |= ExtensionType.pre_shared_key == extension_type;
                        } while (buf.Position < buf.Length);

                        if (pre_shared_key_found && ExtensionType.pre_shared_key != extension_type)
                            throw new TlsFatalAlert(AlertDescription.illegal_parameter,
                                "'pre_shared_key' MUST be last in ClientHello");
                    }

                    return extensions;
                }

                /// <exception cref="IOException" />
                internal static IList ReadSupplementalDataMessage(MemoryStream input)
                {
                    var supp_data = TlsUtilities.ReadOpaque24(input, 1);

                    AssertEmpty(input);

                    var buf = new MemoryStream(supp_data, false);

                    var supplementalData = Platform.CreateArrayList();

                    while (buf.Position < buf.Length)
                    {
                        var supp_data_type = TlsUtilities.ReadUint16(buf);
                        var data           = TlsUtilities.ReadOpaque16(buf);

                        supplementalData.Add(new SupplementalDataEntry(supp_data_type, data));
                    }

                    return supplementalData;
                }

                /// <exception cref="IOException" />
                internal static void WriteExtensions(Stream output, IDictionary extensions) { WriteExtensions(output, extensions, 0); }

                /// <exception cref="IOException" />
                internal static void WriteExtensions(Stream output, IDictionary extensions, int bindersSize)
                {
                    if (null == extensions || extensions.Count < 1)
                        return;

                    var extBytes = WriteExtensionsData(extensions, bindersSize);

                    var lengthWithBinders = extBytes.Length + bindersSize;
                    TlsUtilities.CheckUint16(lengthWithBinders);
                    TlsUtilities.WriteUint16(lengthWithBinders, output);
                    output.Write(extBytes, 0, extBytes.Length);
                }

                /// <exception cref="IOException" />
                internal static byte[] WriteExtensionsData(IDictionary extensions) { return WriteExtensionsData(extensions, 0); }

                /// <exception cref="IOException" />
                internal static byte[] WriteExtensionsData(IDictionary extensions, int bindersSize)
                {
                    var buf = new MemoryStream();
                    WriteExtensionsData(extensions, buf, bindersSize);
                    return buf.ToArray();
                }

                /// <exception cref="IOException" />
                internal static void WriteExtensionsData(IDictionary extensions, MemoryStream buf) { WriteExtensionsData(extensions, buf, 0); }

                /// <exception cref="IOException" />
                internal static void WriteExtensionsData(IDictionary extensions, MemoryStream buf, int bindersSize)
                {
                    /*
                     * NOTE: There are reports of servers that don't accept a zero-length extension as the last
                     * one, so we write out any zero-length ones first as a best-effort workaround.
                     */
                    WriteSelectedExtensions(buf, extensions, true);
                    WriteSelectedExtensions(buf, extensions, false);
                    WritePreSharedKeyExtension(buf, extensions, bindersSize);
                }

                /// <exception cref="IOException" />
                internal static void WritePreSharedKeyExtension(MemoryStream buf, IDictionary extensions, int bindersSize)
                {
                    var extension_data = (byte[])extensions[ExtensionType.pre_shared_key];
                    if (null != extension_data)
                    {
                        TlsUtilities.CheckUint16(ExtensionType.pre_shared_key);
                        TlsUtilities.WriteUint16(ExtensionType.pre_shared_key, buf);

                        var lengthWithBinders = extension_data.Length + bindersSize;
                        TlsUtilities.CheckUint16(lengthWithBinders);
                        TlsUtilities.WriteUint16(lengthWithBinders, buf);
                        buf.Write(extension_data, 0, extension_data.Length);
                    }
                }

                /// <exception cref="IOException" />
                internal static void WriteSelectedExtensions(Stream output, IDictionary extensions, bool selectEmpty)
                {
                    foreach (int key in extensions.Keys)
                    {
                        var extension_type = key;

                        // NOTE: Must be last; handled by 'WritePreSharedKeyExtension'
                        if (ExtensionType.pre_shared_key == extension_type)
                            continue;

                        var extension_data = (byte[])extensions[key];

                        if (selectEmpty == (extension_data.Length == 0))
                        {
                            TlsUtilities.CheckUint16(extension_type);
                            TlsUtilities.WriteUint16(extension_type, output);
                            TlsUtilities.WriteOpaque16(extension_data, output);
                        }
                    }
                }

                /// <exception cref="IOException" />
                internal static void WriteSupplementalData(Stream output, IList supplementalData)
                {
                    var buf = new MemoryStream();

                    foreach (SupplementalDataEntry entry in supplementalData)
                    {
                        var supp_data_type = entry.DataType;
                        TlsUtilities.CheckUint16(supp_data_type);
                        TlsUtilities.WriteUint16(supp_data_type, buf);
                        TlsUtilities.WriteOpaque16(entry.Data, buf);
                    }

                    var supp_data = buf.ToArray();

                    TlsUtilities.WriteOpaque24(supp_data, output);
                }
                }
                }
#pragma warning restore
#endif