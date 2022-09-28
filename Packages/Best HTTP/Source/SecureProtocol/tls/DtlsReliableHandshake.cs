#if !BESTHTTP_DISABLE_ALTERNATE_SSL && (!UNITY_WEBGL || UNITY_EDITOR)
#pragma warning disable
namespace BestHTTP.SecureProtocol.Org.BouncyCastle.Tls
{
    using System;
    using System.Collections;
    using System.IO;
    using BestHTTP.SecureProtocol.Org.BouncyCastle.Utilities;
    using BestHTTP.SecureProtocol.Org.BouncyCastle.Utilities.Date;

    internal class DtlsReliableHandshake
    {
        private const int MAX_RECEIVE_AHEAD     = 16;
        private const int MESSAGE_HEADER_LENGTH = 12;

        internal const int INITIAL_RESEND_MILLIS = 1000;
        private const  int MAX_RESEND_MILLIS     = 60000;

        /// <exception cref="IOException" />
        internal static DtlsRequest ReadClientRequest(byte[] data, int dataOff, int dataLen, Stream dtlsOutput)
        {
            // TODO Support the possibility of a fragmented ClientHello datagram

            var message = DtlsRecordLayer.ReceiveClientHelloRecord(data, dataOff, dataLen);
            if (null == message || message.Length < MESSAGE_HEADER_LENGTH)
                return null;

            var recordSeq = TlsUtilities.ReadUint48(data, dataOff + 5);

            var msgType = TlsUtilities.ReadUint8(message, 0);
            if (HandshakeType.client_hello != msgType)
                return null;

            var length = TlsUtilities.ReadUint24(message, 1);
            if (message.Length != MESSAGE_HEADER_LENGTH + length)
                return null;

            // TODO Consider stricter HelloVerifyRequest-related checks
            //int messageSeq = TlsUtilities.ReadUint16(message, 4);
            //if (messageSeq > 1)
            //    return null;

            var fragmentOffset = TlsUtilities.ReadUint24(message, 6);
            if (0 != fragmentOffset)
                return null;

            var fragmentLength = TlsUtilities.ReadUint24(message, 9);
            if (length != fragmentLength)
                return null;

            var clientHello = ClientHello.Parse(
                new MemoryStream(message, MESSAGE_HEADER_LENGTH, length, false), dtlsOutput);

            return new DtlsRequest(recordSeq, message, clientHello);
        }

        /// <exception cref="IOException" />
        internal static void SendHelloVerifyRequest(DatagramSender sender, long recordSeq, byte[] cookie)
        {
            TlsUtilities.CheckUint8(cookie.Length);

            var length = 3 + cookie.Length;

            var message = new byte[MESSAGE_HEADER_LENGTH + length];
            TlsUtilities.WriteUint8(HandshakeType.hello_verify_request, message, 0);
            TlsUtilities.WriteUint24(length, message, 1);
            //TlsUtilities.WriteUint16(0, message, 4);
            //TlsUtilities.WriteUint24(0, message, 6);
            TlsUtilities.WriteUint24(length, message, 9);

            // HelloVerifyRequest fields
            TlsUtilities.WriteVersion(ProtocolVersion.DTLSv10, message, MESSAGE_HEADER_LENGTH + 0);
            TlsUtilities.WriteOpaque8(cookie, message, MESSAGE_HEADER_LENGTH + 2);

            DtlsRecordLayer.SendHelloVerifyRequestRecord(sender, recordSeq, message);
        }

        /*
         * No 'final' modifiers so that it works in earlier JDKs
         */
        private readonly DtlsRecordLayer m_recordLayer;
        private readonly Timeout         m_handshakeTimeout;

        private IDictionary m_currentInboundFlight = Platform.CreateHashtable();
        private IDictionary m_previousInboundFlight;
        private IList       m_outboundFlight = Platform.CreateArrayList();

        private int     m_resendMillis = -1;
        private Timeout m_resendTimeout;

        private int m_next_send_seq, m_next_receive_seq;

        internal DtlsReliableHandshake(TlsContext context, DtlsRecordLayer transport, int timeoutMillis,
            DtlsRequest request)
        {
            this.m_recordLayer      = transport;
            this.HandshakeHash      = new DeferredHash(context);
            this.m_handshakeTimeout = Timeout.ForWaitMillis(timeoutMillis);

            if (null != request)
            {
                this.m_resendMillis  = INITIAL_RESEND_MILLIS;
                this.m_resendTimeout = new Timeout(this.m_resendMillis);

                var recordSeq  = request.RecordSeq;
                var messageSeq = request.MessageSeq;
                var message    = request.Message;

                this.m_recordLayer.ResetAfterHelloVerifyRequestServer(recordSeq);

                // Simulate a previous flight consisting of the request ClientHello
                var reassembler = new DtlsReassembler(HandshakeType.client_hello,
                    message.Length - MESSAGE_HEADER_LENGTH);
                this.m_currentInboundFlight[messageSeq] = reassembler;

                // We sent HelloVerifyRequest with (message) sequence number 0
                this.m_next_send_seq    = 1;
                this.m_next_receive_seq = messageSeq + 1;

                this.HandshakeHash.Update(message, 0, message.Length);
            }
        }

        internal void ResetAfterHelloVerifyRequestClient()
        {
            this.m_currentInboundFlight  = Platform.CreateHashtable();
            this.m_previousInboundFlight = null;
            this.m_outboundFlight        = Platform.CreateArrayList();

            this.m_resendMillis  = -1;
            this.m_resendTimeout = null;

            // We're waiting for ServerHello, always with (message) sequence number 1
            this.m_next_receive_seq = 1;

            this.HandshakeHash.Reset();
        }

        internal TlsHandshakeHash HandshakeHash { get; private set; }

        internal TlsHandshakeHash PrepareToFinish()
        {
            var result = this.HandshakeHash;
            this.HandshakeHash = this.HandshakeHash.StopTracking();
            return result;
        }

        /// <exception cref="IOException" />
        internal void SendMessage(short msg_type, byte[] body)
        {
            TlsUtilities.CheckUint24(body.Length);

            if (null != this.m_resendTimeout)
            {
                this.CheckInboundFlight();

                this.m_resendMillis  = -1;
                this.m_resendTimeout = null;

                this.m_outboundFlight.Clear();
            }

            var message = new Message(this.m_next_send_seq++, msg_type, body);

            this.m_outboundFlight.Add(message);

            this.WriteMessage(message);
            this.UpdateHandshakeMessagesDigest(message);
        }

        /// <exception cref="IOException" />
        internal byte[] ReceiveMessageBody(short msg_type)
        {
            var message = this.ReceiveMessage();
            if (message.Type != msg_type)
                throw new TlsFatalAlert(AlertDescription.unexpected_message);

            return message.Body;
        }

        /// <exception cref="IOException" />
        internal Message ReceiveMessage()
        {
            var currentTimeMillis = DateTimeUtilities.CurrentUnixMs();

            if (null == this.m_resendTimeout)
            {
                this.m_resendMillis  = INITIAL_RESEND_MILLIS;
                this.m_resendTimeout = new Timeout(this.m_resendMillis, currentTimeMillis);

                this.PrepareInboundFlight(Platform.CreateHashtable());
            }

            byte[] buf = null;

            for (;;)
            {
                if (this.m_recordLayer.IsClosed)
                    throw new TlsFatalAlert(AlertDescription.user_canceled);

                var pending = this.GetPendingMessage();
                if (pending != null)
                    return pending;

                if (Timeout.HasExpired(this.m_handshakeTimeout, currentTimeMillis))
                    throw new TlsTimeoutException("Handshake timed out");

                var waitMillis = Timeout.GetWaitMillis(this.m_handshakeTimeout, currentTimeMillis);
                waitMillis = Timeout.ConstrainWaitMillis(waitMillis, this.m_resendTimeout, currentTimeMillis);

                // NOTE: Ensure a finite wait, of at least 1ms
                if (waitMillis < 1) waitMillis = 1;

                var receiveLimit                                  = this.m_recordLayer.GetReceiveLimit();
                if (buf == null || buf.Length < receiveLimit) buf = new byte[receiveLimit];

                var received = this.m_recordLayer.Receive(buf, 0, receiveLimit, waitMillis);
                if (received < 0)
                    this.ResendOutboundFlight();
                else
                    this.ProcessRecord(MAX_RECEIVE_AHEAD, this.m_recordLayer.ReadEpoch, buf, 0, received);

                currentTimeMillis = DateTimeUtilities.CurrentUnixMs();
            }
        }

        internal void Finish()
        {
            DtlsHandshakeRetransmit retransmit = null;
            if (null != this.m_resendTimeout)
            {
                this.CheckInboundFlight();
            }
            else
            {
                this.PrepareInboundFlight(null);

                if (this.m_previousInboundFlight != null)
                    /*
                         * RFC 6347 4.2.4. In addition, for at least twice the default MSL defined for [TCP],
                         * when in the FINISHED state, the node that transmits the last flight (the server in an
                         * ordinary handshake or the client in a resumed handshake) MUST respond to a retransmit
                         * of the peer's last flight with a retransmit of the last flight.
                         */
                    retransmit = new Retransmit(this);
            }

            this.m_recordLayer.HandshakeSuccessful(retransmit);
        }

        internal static int BackOff(int timeoutMillis)
        {
            /*
             * TODO[DTLS] implementations SHOULD back off handshake packet size during the
             * retransmit backoff.
             */
            return Math.Min(timeoutMillis * 2, MAX_RESEND_MILLIS);
        }

        /**
         * Check that there are no "extra" messages left in the current inbound flight
         */
        private void CheckInboundFlight()
        {
            foreach (int key in this.m_currentInboundFlight.Keys)
                if (key >= this.m_next_receive_seq)
                {
                    // TODO Should this be considered an error?
                }
        }

        /// <exception cref="IOException" />
        private Message GetPendingMessage()
        {
            var next = (DtlsReassembler)this.m_currentInboundFlight[this.m_next_receive_seq];
            if (next != null)
            {
                var body = next.GetBodyIfComplete();
                if (body != null)
                {
                    this.m_previousInboundFlight = null;
                    return this.UpdateHandshakeMessagesDigest(new Message(this.m_next_receive_seq++, next.MsgType, body));
                }
            }

            return null;
        }

        private void PrepareInboundFlight(IDictionary nextFlight)
        {
            ResetAll(this.m_currentInboundFlight);
            this.m_previousInboundFlight = this.m_currentInboundFlight;
            this.m_currentInboundFlight  = nextFlight;
        }

        /// <exception cref="IOException" />
        private void ProcessRecord(int windowSize, int epoch, byte[] buf, int off, int len)
        {
            var checkPreviousFlight = false;

            while (len >= MESSAGE_HEADER_LENGTH)
            {
                var fragment_length = TlsUtilities.ReadUint24(buf, off + 9);
                var message_length  = fragment_length + MESSAGE_HEADER_LENGTH;
                if (len < message_length)
                    // NOTE: Truncated message - ignore it
                    break;

                var length          = TlsUtilities.ReadUint24(buf, off + 1);
                var fragment_offset = TlsUtilities.ReadUint24(buf, off + 6);
                if (fragment_offset + fragment_length > length)
                    // NOTE: Malformed fragment - ignore it and the rest of the record
                    break;

                /*
                 * NOTE: This very simple epoch check will only work until we want to support
                 * renegotiation (and we're not likely to do that anyway).
                 */
                var msg_type      = TlsUtilities.ReadUint8(buf, off + 0);
                var expectedEpoch = msg_type == HandshakeType.finished ? 1 : 0;
                if (epoch != expectedEpoch)
                    break;

                var message_seq = TlsUtilities.ReadUint16(buf, off + 4);
                if (message_seq >= this.m_next_receive_seq + windowSize)
                {
                    // NOTE: Too far ahead - ignore
                }
                else if (message_seq >= this.m_next_receive_seq)
                {
                    var reassembler = (DtlsReassembler)this.m_currentInboundFlight[message_seq];
                    if (reassembler == null)
                    {
                        reassembler                              = new DtlsReassembler(msg_type, length);
                        this.m_currentInboundFlight[message_seq] = reassembler;
                    }

                    reassembler.ContributeFragment(msg_type, length, buf, off + MESSAGE_HEADER_LENGTH, fragment_offset,
                        fragment_length);
                }
                else if (this.m_previousInboundFlight != null)
                {
                    /*
                     * NOTE: If we receive the previous flight of incoming messages in full again,
                     * retransmit our last flight
                     */

                    var reassembler = (DtlsReassembler)this.m_previousInboundFlight[message_seq];
                    if (reassembler != null)
                    {
                        reassembler.ContributeFragment(msg_type, length, buf, off + MESSAGE_HEADER_LENGTH,
                            fragment_offset, fragment_length);
                        checkPreviousFlight = true;
                    }
                }

                off += message_length;
                len -= message_length;
            }

            if (checkPreviousFlight && CheckAll(this.m_previousInboundFlight))
            {
                this.ResendOutboundFlight();
                ResetAll(this.m_previousInboundFlight);
            }
        }

        /// <exception cref="IOException" />
        private void ResendOutboundFlight()
        {
            this.m_recordLayer.ResetWriteEpoch();
            foreach (Message message in this.m_outboundFlight) this.WriteMessage(message);

            this.m_resendMillis  = BackOff(this.m_resendMillis);
            this.m_resendTimeout = new Timeout(this.m_resendMillis);
        }

        /// <exception cref="IOException" />
        private Message UpdateHandshakeMessagesDigest(Message message)
        {
            var msg_type = message.Type;
            switch (msg_type)
            {
                case HandshakeType.hello_request:
                case HandshakeType.hello_verify_request:
                case HandshakeType.key_update:
                    break;

                // TODO[dtls13] Not included in the transcript for (D)TLS 1.3+
                case HandshakeType.new_session_ticket:
                default:
                {
                    var body = message.Body;
                    var buf  = new byte[MESSAGE_HEADER_LENGTH];
                    TlsUtilities.WriteUint8(msg_type, buf, 0);
                    TlsUtilities.WriteUint24(body.Length, buf, 1);
                    TlsUtilities.WriteUint16(message.Seq, buf, 4);
                    TlsUtilities.WriteUint24(0, buf, 6);
                    TlsUtilities.WriteUint24(body.Length, buf, 9);
                    this.HandshakeHash.Update(buf, 0, buf.Length);
                    this.HandshakeHash.Update(body, 0, body.Length);
                    break;
                }
            }

            return message;
        }

        /// <exception cref="IOException" />
        private void WriteMessage(Message message)
        {
            var sendLimit     = this.m_recordLayer.GetSendLimit();
            var fragmentLimit = sendLimit - MESSAGE_HEADER_LENGTH;

            // TODO Support a higher minimum fragment size?
            if (fragmentLimit < 1)
                // TODO Should we be throwing an exception here?
                throw new TlsFatalAlert(AlertDescription.internal_error);

            var length = message.Body.Length;

            // NOTE: Must still send a fragment if body is empty
            var fragment_offset = 0;
            do
            {
                var fragment_length = Math.Min(length - fragment_offset, fragmentLimit);
                this.WriteHandshakeFragment(message, fragment_offset, fragment_length);
                fragment_offset += fragment_length;
            } while (fragment_offset < length);
        }

        /// <exception cref="IOException" />
        private void WriteHandshakeFragment(Message message, int fragment_offset, int fragment_length)
        {
            var fragment = new RecordLayerBuffer(MESSAGE_HEADER_LENGTH + fragment_length);
            TlsUtilities.WriteUint8(message.Type, fragment);
            TlsUtilities.WriteUint24(message.Body.Length, fragment);
            TlsUtilities.WriteUint16(message.Seq, fragment);
            TlsUtilities.WriteUint24(fragment_offset, fragment);
            TlsUtilities.WriteUint24(fragment_length, fragment);
            fragment.Write(message.Body, fragment_offset, fragment_length);

            fragment.SendToRecordLayer(this.m_recordLayer);
        }

        private static bool CheckAll(IDictionary inboundFlight)
        {
            foreach (DtlsReassembler r in inboundFlight.Values)
                if (r.GetBodyIfComplete() == null)
                    return false;
            return true;
        }

        private static void ResetAll(IDictionary inboundFlight)
        {
            foreach (DtlsReassembler r in inboundFlight.Values) r.Reset();
        }

        internal class Message
        {
            internal Message(int message_seq, short msg_type, byte[] body)
            {
                this.Seq  = message_seq;
                this.Type = msg_type;
                this.Body = body;
            }

            public int Seq { get; }

            public short Type { get; }

            public byte[] Body { get; }
        }

        internal class RecordLayerBuffer
            : MemoryStream
        {
            internal RecordLayerBuffer(int size)
                : base(size)
            {
            }

            internal void SendToRecordLayer(DtlsRecordLayer recordLayer)
            {
#if PORTABLE || NETFX_CORE
                byte[] buf = ToArray();
                int bufLen = buf.Length;
#else
                var buf    = this.GetBuffer();
                var bufLen = (int)this.Length;
#endif

                recordLayer.Send(buf, 0, bufLen);
                Platform.Dispose(this);
            }
        }

        internal class Retransmit
            : DtlsHandshakeRetransmit
        {
            private readonly DtlsReliableHandshake m_outer;

            internal Retransmit(DtlsReliableHandshake outer) { this.m_outer = outer; }

            public void ReceivedHandshakeRecord(int epoch, byte[] buf, int off, int len) { this.m_outer.ProcessRecord(0, epoch, buf, off, len); }
        }
    }
}
#pragma warning restore
#endif