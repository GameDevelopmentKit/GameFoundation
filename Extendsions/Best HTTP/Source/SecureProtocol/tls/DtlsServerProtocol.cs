#if !BESTHTTP_DISABLE_ALTERNATE_SSL && (!UNITY_WEBGL || UNITY_EDITOR)
#pragma warning disable
namespace BestHTTP.SecureProtocol.Org.BouncyCastle.Tls
{
    using System;
    using System.Collections;
    using System.IO;
    using BestHTTP.SecureProtocol.Org.BouncyCastle.Tls.Crypto;
    using BestHTTP.SecureProtocol.Org.BouncyCastle.Utilities;
    using BestHTTP.SecureProtocol.Org.BouncyCastle.Utilities.IO;

    public class DtlsServerProtocol
        : DtlsProtocol
    {
        protected bool m_verifyRequests = true;

        public virtual bool VerifyRequests { get => this.m_verifyRequests; set => this.m_verifyRequests = value; }

        /// <exception cref="IOException" />
        public virtual DtlsTransport Accept(TlsServer server, DatagramTransport transport) { return this.Accept(server, transport, null); }

        /// <exception cref="IOException" />
        public virtual DtlsTransport Accept(TlsServer server, DatagramTransport transport, DtlsRequest request)
        {
            if (server == null)
                throw new ArgumentNullException("server");
            if (transport == null)
                throw new ArgumentNullException("transport");

            var state = new ServerHandshakeState();
            state.server        = server;
            state.serverContext = new TlsServerContextImpl(server.Crypto);
            server.Init(state.serverContext);
            state.serverContext.HandshakeBeginning(server);

            var securityParameters = state.serverContext.SecurityParameters;
            securityParameters.m_extendedPadding = server.ShouldUseExtendedPadding();

            var recordLayer = new DtlsRecordLayer(state.serverContext, state.server, transport);
            server.NotifyCloseHandle(recordLayer);

            try
            {
                return this.ServerHandshake(state, recordLayer, request);
            }
            catch (TlsFatalAlert fatalAlert)
            {
                this.AbortServerHandshake(state, recordLayer, fatalAlert.AlertDescription);
                throw fatalAlert;
            }
            catch (IOException e)
            {
                this.AbortServerHandshake(state, recordLayer, AlertDescription.internal_error);
                throw e;
            }
            catch (Exception e)
            {
                this.AbortServerHandshake(state, recordLayer, AlertDescription.internal_error);
                throw new TlsFatalAlert(AlertDescription.internal_error, e);
            }
            finally
            {
                securityParameters.Clear();
            }
        }

        internal virtual void AbortServerHandshake(ServerHandshakeState state, DtlsRecordLayer recordLayer,
            short alertDescription)
        {
            recordLayer.Fail(alertDescription);
            this.InvalidateSession(state);
        }

        /// <exception cref="IOException" />
        internal virtual DtlsTransport ServerHandshake(ServerHandshakeState state, DtlsRecordLayer recordLayer,
            DtlsRequest request)
        {
            var securityParameters = state.serverContext.SecurityParameters;

            var handshake = new DtlsReliableHandshake(state.serverContext, recordLayer,
                state.server.GetHandshakeTimeoutMillis(), request);

            DtlsReliableHandshake.Message clientMessage = null;

            if (null == request)
            {
                clientMessage = handshake.ReceiveMessage();

                // NOTE: DtlsRecordLayer requires any DTLS version, we don't otherwise constrain this
                //ProtocolVersion recordLayerVersion = recordLayer.ReadVersion;

                if (clientMessage.Type == HandshakeType.client_hello)
                    this.ProcessClientHello(state, clientMessage.Body);
                else
                    throw new TlsFatalAlert(AlertDescription.unexpected_message);
            }
            else
            {
                this.ProcessClientHello(state, request.ClientHello);
            }

            /*
             * NOTE: Currently no server support for session resumption
             * 
             * If adding support, ensure securityParameters.tlsUnique is set to the localVerifyData, but
             * ONLY when extended_master_secret has been negotiated (otherwise NULL).
             */
            {
                // TODO[resumption]

                state.tlsSession          = TlsUtilities.ImportSession(TlsUtilities.EmptyBytes, null);
                state.sessionParameters   = null;
                state.sessionMasterSecret = null;
            }

            securityParameters.m_sessionID = state.tlsSession.SessionID;

            state.server.NotifySession(state.tlsSession);

            {
                var serverHelloBody = this.GenerateServerHello(state, recordLayer);

                // TODO[dtls13] Ideally, move this into GenerateServerHello once legacy_record_version clarified
                {
                    var recordLayerVersion = state.serverContext.ServerVersion;
                    recordLayer.ReadVersion = recordLayerVersion;
                    recordLayer.SetWriteVersion(recordLayerVersion);
                }

                handshake.SendMessage(HandshakeType.server_hello, serverHelloBody);
            }

            handshake.HandshakeHash.NotifyPrfDetermined();

            var serverSupplementalData = state.server.GetServerSupplementalData();
            if (serverSupplementalData != null)
            {
                var supplementalDataBody = GenerateSupplementalData(serverSupplementalData);
                handshake.SendMessage(HandshakeType.supplemental_data, supplementalDataBody);
            }

            state.keyExchange       = TlsUtilities.InitKeyExchangeServer(state.serverContext, state.server);
            state.serverCredentials = TlsUtilities.EstablishServerCredentials(state.server);

            // Server certificate
            {
                Certificate serverCertificate = null;

                var endPointHash = new MemoryStream();
                if (state.serverCredentials == null)
                {
                    state.keyExchange.SkipServerCredentials();
                }
                else
                {
                    state.keyExchange.ProcessServerCredentials(state.serverCredentials);

                    serverCertificate = state.serverCredentials.Certificate;

                    SendCertificateMessage(state.serverContext, handshake, serverCertificate, endPointHash);
                }

                securityParameters.m_tlsServerEndPoint = endPointHash.ToArray();

                // TODO[RFC 3546] Check whether empty certificates is possible, allowed, or excludes CertificateStatus
                if (serverCertificate == null || serverCertificate.IsEmpty) securityParameters.m_statusRequestVersion = 0;
            }

            if (securityParameters.StatusRequestVersion > 0)
            {
                var certificateStatus = state.server.GetCertificateStatus();
                if (certificateStatus != null)
                {
                    var certificateStatusBody = this.GenerateCertificateStatus(state, certificateStatus);
                    handshake.SendMessage(HandshakeType.certificate_status, certificateStatusBody);
                }
            }

            var serverKeyExchange = state.keyExchange.GenerateServerKeyExchange();
            if (serverKeyExchange != null) handshake.SendMessage(HandshakeType.server_key_exchange, serverKeyExchange);

            if (state.serverCredentials != null)
            {
                state.certificateRequest = state.server.GetCertificateRequest();

                if (null == state.certificateRequest)
                {
                    /*
                     * For static agreement key exchanges, CertificateRequest is required since
                     * the client Certificate message is mandatory but can only be sent if the
                     * server requests it.
                     */
                    if (!state.keyExchange.RequiresCertificateVerify)
                        throw new TlsFatalAlert(AlertDescription.internal_error);
                }
                else
                {
                    if (TlsUtilities.IsTlsV12(state.serverContext)
                        != (state.certificateRequest.SupportedSignatureAlgorithms != null))
                        throw new TlsFatalAlert(AlertDescription.internal_error);

                    state.certificateRequest = TlsUtilities.ValidateCertificateRequest(state.certificateRequest, state.keyExchange);

                    TlsUtilities.EstablishServerSigAlgs(securityParameters, state.certificateRequest);

                    TlsUtilities.TrackHashAlgorithms(handshake.HandshakeHash, securityParameters.ServerSigAlgs);

                    var certificateRequestBody = this.GenerateCertificateRequest(state, state.certificateRequest);
                    handshake.SendMessage(HandshakeType.certificate_request, certificateRequestBody);
                }
            }

            handshake.SendMessage(HandshakeType.server_hello_done, TlsUtilities.EmptyBytes);

            var forceBuffering = false;
            TlsUtilities.SealHandshakeHash(state.serverContext, handshake.HandshakeHash, forceBuffering);

            clientMessage = handshake.ReceiveMessage();

            if (clientMessage.Type == HandshakeType.supplemental_data)
            {
                this.ProcessClientSupplementalData(state, clientMessage.Body);
                clientMessage = handshake.ReceiveMessage();
            }
            else
            {
                state.server.ProcessClientSupplementalData(null);
            }

            if (state.certificateRequest == null)
            {
                state.keyExchange.SkipClientCredentials();
            }
            else
            {
                if (clientMessage.Type == HandshakeType.certificate)
                {
                    this.ProcessClientCertificate(state, clientMessage.Body);
                    clientMessage = handshake.ReceiveMessage();
                }
                else
                {
                    if (TlsUtilities.IsTlsV12(state.serverContext))
                        /*
                             * RFC 5246 If no suitable certificate is available, the client MUST send a
                             * certificate message containing no certificates.
                             * 
                             * NOTE: In previous RFCs, this was SHOULD instead of MUST.
                             */
                        throw new TlsFatalAlert(AlertDescription.unexpected_message);

                    this.NotifyClientCertificate(state, Certificate.EmptyChain);
                }
            }

            if (clientMessage.Type == HandshakeType.client_key_exchange)
                this.ProcessClientKeyExchange(state, clientMessage.Body);
            else
                throw new TlsFatalAlert(AlertDescription.unexpected_message);

            securityParameters.m_sessionHash = TlsUtilities.GetCurrentPrfHash(handshake.HandshakeHash);

            TlsProtocol.EstablishMasterSecret(state.serverContext, state.keyExchange);
            recordLayer.InitPendingEpoch(TlsUtilities.InitCipher(state.serverContext));

            /*
             * RFC 5246 7.4.8 This message is only sent following a client certificate that has signing
             * capability (i.e., all certificates except those containing fixed Diffie-Hellman
             * parameters).
             */
            {
                var certificateVerifyHash = handshake.PrepareToFinish();

                if (this.ExpectCertificateVerifyMessage(state))
                {
                    var certificateVerifyBody = handshake.ReceiveMessageBody(HandshakeType.certificate_verify);
                    this.ProcessCertificateVerify(state, certificateVerifyBody, certificateVerifyHash);
                }
            }

            // NOTE: Calculated exclusive of the actual Finished message from the client
            securityParameters.m_peerVerifyData = TlsUtilities.CalculateVerifyData(state.serverContext,
                handshake.HandshakeHash, false);
            this.ProcessFinished(handshake.ReceiveMessageBody(HandshakeType.finished), securityParameters.PeerVerifyData);

            if (state.expectSessionTicket)
            {
                /*
                 * TODO[new_session_ticket] Check the server-side rules regarding the session ID, since the client
                 * is going to ignore any session ID it received once it sees the new_session_ticket message.
                 */

                var newSessionTicket     = state.server.GetNewSessionTicket();
                var newSessionTicketBody = this.GenerateNewSessionTicket(state, newSessionTicket);
                handshake.SendMessage(HandshakeType.new_session_ticket, newSessionTicketBody);
            }

            // NOTE: Calculated exclusive of the Finished message itself
            securityParameters.m_localVerifyData = TlsUtilities.CalculateVerifyData(state.serverContext,
                handshake.HandshakeHash, true);
            handshake.SendMessage(HandshakeType.finished, securityParameters.LocalVerifyData);

            handshake.Finish();

            state.sessionMasterSecret = securityParameters.MasterSecret;

            state.sessionParameters = new SessionParameters.Builder()
                .SetCipherSuite(securityParameters.CipherSuite)
                .SetExtendedMasterSecret(securityParameters.IsExtendedMasterSecret)
                .SetLocalCertificate(securityParameters.LocalCertificate)
                .SetMasterSecret(state.serverContext.Crypto.AdoptSecret(state.sessionMasterSecret))
                .SetNegotiatedVersion(securityParameters.NegotiatedVersion)
                .SetPeerCertificate(securityParameters.PeerCertificate)
                .SetPskIdentity(securityParameters.PskIdentity)
                .SetSrpIdentity(securityParameters.SrpIdentity)
                // TODO Consider filtering extensions that aren't relevant to resumed sessions
                .SetServerExtensions(state.serverExtensions)
                .Build();

            state.tlsSession = TlsUtilities.ImportSession(state.tlsSession.SessionID, state.sessionParameters);

            securityParameters.m_tlsUnique = securityParameters.PeerVerifyData;

            state.serverContext.HandshakeComplete(state.server, state.tlsSession);

            recordLayer.InitHeartbeat(state.heartbeat, HeartbeatMode.peer_allowed_to_send == state.heartbeatPolicy);

            return new DtlsTransport(recordLayer);
        }

        /// <exception cref="IOException" />
        protected virtual byte[] GenerateCertificateRequest(ServerHandshakeState state,
            CertificateRequest certificateRequest)
        {
            var buf = new MemoryStream();
            certificateRequest.Encode(state.serverContext, buf);
            return buf.ToArray();
        }

        /// <exception cref="IOException" />
        protected virtual byte[] GenerateCertificateStatus(ServerHandshakeState state,
            CertificateStatus certificateStatus)
        {
            var buf = new MemoryStream();
            // TODO[tls13] Ensure this cannot happen for (D)TLS1.3+
            certificateStatus.Encode(buf);
            return buf.ToArray();
        }

        /// <exception cref="IOException" />
        protected virtual byte[] GenerateNewSessionTicket(ServerHandshakeState state,
            NewSessionTicket newSessionTicket)
        {
            var buf = new MemoryStream();
            newSessionTicket.Encode(buf);
            return buf.ToArray();
        }

        /// <exception cref="IOException" />
        internal virtual byte[] GenerateServerHello(ServerHandshakeState state, DtlsRecordLayer recordLayer)
        {
            var context            = state.serverContext;
            var securityParameters = context.SecurityParameters;

            var server_version = state.server.GetServerVersion();
            {
                if (!ProtocolVersion.Contains(context.ClientSupportedVersions, server_version))
                    throw new TlsFatalAlert(AlertDescription.internal_error);

                // TODO[dtls13] Read draft/RFC for guidance on the legacy_record_version field
                //ProtocolVersion legacy_record_version = server_version.IsLaterVersionOf(ProtocolVersion.DTLSv12)
                //    ? ProtocolVersion.DTLSv12
                //    : server_version;

                //recordLayer.SetWriteVersion(legacy_record_version);
                securityParameters.m_negotiatedVersion = server_version;

                TlsUtilities.NegotiatedVersionDtlsServer(context);
            }

            {
                var useGmtUnixTime = ProtocolVersion.DTLSv12.IsEqualOrLaterVersionOf(server_version)
                                     && state.server.ShouldUseGmtUnixTime();

                securityParameters.m_serverRandom = TlsProtocol.CreateRandomBlock(useGmtUnixTime, context);

                if (!server_version.Equals(ProtocolVersion.GetLatestDtls(state.server.GetProtocolVersions())))
                    TlsUtilities.WriteDowngradeMarker(server_version, securityParameters.ServerRandom);
            }

            {
                var cipherSuite = ValidateSelectedCipherSuite(state.server.GetSelectedCipherSuite(),
                    AlertDescription.internal_error);

                if (!TlsUtilities.IsValidCipherSuiteSelection(state.offeredCipherSuites, cipherSuite) ||
                    !TlsUtilities.IsValidVersionForCipherSuite(cipherSuite, securityParameters.NegotiatedVersion))
                    throw new TlsFatalAlert(AlertDescription.internal_error);

                TlsUtilities.NegotiatedCipherSuite(securityParameters, cipherSuite);
            }

            state.serverExtensions = TlsExtensionsUtilities.EnsureExtensionsInitialised(
                state.server.GetServerExtensions());

            state.server.GetServerExtensionsForConnection(state.serverExtensions);

            var legacy_version = server_version;
            if (server_version.IsLaterVersionOf(ProtocolVersion.DTLSv12))
            {
                legacy_version = ProtocolVersion.DTLSv12;

                TlsExtensionsUtilities.AddSupportedVersionsExtensionServer(state.serverExtensions, server_version);
            }

            /*
             * RFC 5746 3.6. Server Behavior: Initial Handshake 
             */
            if (securityParameters.IsSecureRenegotiation)
            {
                var renegExtData = TlsUtilities.GetExtensionData(state.serverExtensions,
                    ExtensionType.renegotiation_info);
                var noRenegExt = null == renegExtData;

                if (noRenegExt)
                    /*
                         * Note that sending a "renegotiation_info" extension in response to a ClientHello
                         * containing only the SCSV is an explicit exception to the prohibition in RFC 5246,
                         * Section 7.4.1.4, on the server sending unsolicited extensions and is only allowed
                         * because the client is signaling its willingness to receive the extension via the
                         * TLS_EMPTY_RENEGOTIATION_INFO_SCSV SCSV.
                         */
                    /*
                         * If the secure_renegotiation flag is set to TRUE, the server MUST include an empty
                         * "renegotiation_info" extension in the ServerHello message.
                         */
                        state.serverExtensions[ExtensionType.renegotiation_info] = TlsProtocol.CreateRenegotiationInfo(
                            TlsUtilities.EmptyBytes);
                    }

                    /*
                     * RFC 7627 4. Clients and servers SHOULD NOT accept handshakes that do not use the extended
                     * master secret [..]. (and see 5.2, 5.3)
                     * 
                     * RFC 8446 Appendix D. Because TLS 1.3 always hashes in the transcript up to the server
                     * Finished, implementations which support both TLS 1.3 and earlier versions SHOULD indicate
                     * the use of the Extended Master Secret extension in their APIs whenever TLS 1.3 is used.
                     */
                    if (TlsUtilities.IsTlsV13(server_version))
                    {
                        securityParameters.m_extendedMasterSecret = true;
                    }
                    else
                    {
                        securityParameters.m_extendedMasterSecret = state.offeredExtendedMasterSecret
                                                                    && state.server.ShouldUseExtendedMasterSecret();

                        if (securityParameters.IsExtendedMasterSecret)
                            TlsExtensionsUtilities.AddExtendedMasterSecretExtension(state.serverExtensions);
                        else if (state.server.RequiresExtendedMasterSecret())
                            throw new TlsFatalAlert(AlertDescription.handshake_failure);
                        else if (state.resumedSession && !state.server.AllowLegacyResumption()) throw new TlsFatalAlert(AlertDescription.internal_error);
                    }

                    // Heartbeats
                    if (null != state.heartbeat || HeartbeatMode.peer_allowed_to_send == state.heartbeatPolicy)
                        TlsExtensionsUtilities.AddHeartbeatExtension(state.serverExtensions,
                            new HeartbeatExtension(state.heartbeatPolicy));


                    /*
                     * RFC 7301 3.1. When session resumption or session tickets [...] are used, the previous
                     * contents of this extension are irrelevant, and only the values in the new handshake
                     * messages are considered.
                     */
                    securityParameters.m_applicationProtocol = TlsExtensionsUtilities.GetAlpnExtensionServer(
                        state.serverExtensions);
                    securityParameters.m_applicationProtocolSet = true;

                    /*
                     * TODO RFC 3546 2.3 If [...] the older session is resumed, then the server MUST ignore
                     * extensions appearing in the client hello, and send a server hello containing no
                     * extensions.
                     */
                    if (state.serverExtensions.Count > 0)
                    {
                        securityParameters.m_encryptThenMac = TlsExtensionsUtilities.HasEncryptThenMacExtension(
                            state.serverExtensions);

                        securityParameters.m_maxFragmentLength = EvaluateMaxFragmentLengthExtension(state.resumedSession,
                            state.clientExtensions, state.serverExtensions, AlertDescription.internal_error);

                        securityParameters.m_truncatedHmac = TlsExtensionsUtilities.HasTruncatedHmacExtension(state.serverExtensions);

                        /*
                         * TODO It's surprising that there's no provision to allow a 'fresh' CertificateStatus to be sent in
                         * a session resumption handshake.
                         */
                        if (!state.resumedSession)
                        {
                            // TODO[tls13] See RFC 8446 4.4.2.1
                            if (TlsUtilities.HasExpectedEmptyExtensionData(state.serverExtensions,
                                    ExtensionType.status_request_v2, AlertDescription.internal_error))
                                securityParameters.m_statusRequestVersion = 2;
                            else if (TlsUtilities.HasExpectedEmptyExtensionData(state.serverExtensions,
                                         ExtensionType.status_request, AlertDescription.internal_error))
                                securityParameters.m_statusRequestVersion = 1;
                        }

                        state.expectSessionTicket = !state.resumedSession
                                                    && TlsUtilities.HasExpectedEmptyExtensionData(state.serverExtensions, ExtensionType.session_ticket,
                                                        AlertDescription.internal_error);
                    }

                    ApplyMaxFragmentLengthExtension(recordLayer, securityParameters.MaxFragmentLength);


                    var serverHello = new ServerHello(legacy_version, securityParameters.ServerRandom,
                        state.tlsSession.SessionID, securityParameters.CipherSuite, state.serverExtensions);

                    var buf = new MemoryStream();
                    serverHello.Encode(state.serverContext, buf);
                    return buf.ToArray();
                    }

                    protected virtual void InvalidateSession(ServerHandshakeState state)
                    {
                        if (state.sessionMasterSecret != null)
                        {
                            state.sessionMasterSecret.Destroy();
                            state.sessionMasterSecret = null;
                        }

                        if (state.sessionParameters != null)
                        {
                            state.sessionParameters.Clear();
                            state.sessionParameters = null;
                        }

                        if (state.tlsSession != null)
                        {
                            state.tlsSession.Invalidate();
                            state.tlsSession = null;
                        }
                    }

                    /// <exception cref="IOException" />
                    protected virtual void NotifyClientCertificate(ServerHandshakeState state, Certificate clientCertificate)
                    {
                        if (null == state.certificateRequest)
                            throw new TlsFatalAlert(AlertDescription.internal_error);

                        TlsUtilities.ProcessClientCertificate(state.serverContext, clientCertificate, state.keyExchange,
                            state.server);
                    }

                    /// <exception cref="IOException" />
                    protected virtual void ProcessClientCertificate(ServerHandshakeState state, byte[] body)
                    {
                        var buf = new MemoryStream(body, false);

                        var options = new Certificate.ParseOptions()
                            .SetMaxChainLength(state.server.GetMaxCertificateChainLength());

                        var clientCertificate = Certificate.Parse(options, state.serverContext, buf, null);

                        TlsProtocol.AssertEmpty(buf);

                        this.NotifyClientCertificate(state, clientCertificate);
                    }

                    /// <exception cref="IOException" />
                    protected virtual void ProcessCertificateVerify(ServerHandshakeState state, byte[] body,
                        TlsHandshakeHash handshakeHash)
                    {
                        if (state.certificateRequest == null)
                            throw new InvalidOperationException();

                        var buf = new MemoryStream(body, false);

                        var context           = state.serverContext;
                        var certificateVerify = DigitallySigned.Parse(context, buf);

                        TlsProtocol.AssertEmpty(buf);

                        TlsUtilities.VerifyCertificateVerifyClient(context, state.certificateRequest, certificateVerify, handshakeHash);
                    }

                    /// <exception cref="IOException" />
                    protected virtual void ProcessClientHello(ServerHandshakeState state, byte[] body)
                    {
                        var buf         = new MemoryStream(body, false);
                        var clientHello = ClientHello.Parse(buf, new NullOutputStream());
                        this.ProcessClientHello(state, clientHello);
                    }

                    /// <exception cref="IOException" />
                    protected virtual void ProcessClientHello(ServerHandshakeState state, ClientHello clientHello)
                    {
                        // TODO Read RFCs for guidance on the expected record layer version number
                        var legacy_version = clientHello.Version;
                        state.offeredCipherSuites = clientHello.CipherSuites;

                        /*
                         * TODO RFC 3546 2.3 If [...] the older session is resumed, then the server MUST ignore
                         * extensions appearing in the client hello, and send a server hello containing no
                         * extensions.
                         */
                        state.clientExtensions = clientHello.Extensions;


                        var context            = state.serverContext;
                        var securityParameters = context.SecurityParameters;

                        if (!legacy_version.IsDtls)
                            throw new TlsFatalAlert(AlertDescription.illegal_parameter);

                        context.SetRsaPreMasterSecretVersion(legacy_version);

                        context.SetClientSupportedVersions(
                            TlsExtensionsUtilities.GetSupportedVersionsExtensionClient(state.clientExtensions));

                        var client_version = legacy_version;
                        if (null == context.ClientSupportedVersions)
                        {
                            if (client_version.IsLaterVersionOf(ProtocolVersion.DTLSv12)) client_version = ProtocolVersion.DTLSv12;

                            context.SetClientSupportedVersions(client_version.DownTo(ProtocolVersion.DTLSv10));
                        }
                        else
                        {
                            client_version = ProtocolVersion.GetLatestDtls(context.ClientSupportedVersions);
                        }

                        if (!ProtocolVersion.SERVER_EARLIEST_SUPPORTED_DTLS.IsEqualOrEarlierVersionOf(client_version))
                            throw new TlsFatalAlert(AlertDescription.protocol_version);

                        context.SetClientVersion(client_version);

                        state.server.NotifyClientVersion(context.ClientVersion);

                        securityParameters.m_clientRandom = clientHello.Random;

                        state.server.NotifyFallback(Arrays.Contains(state.offeredCipherSuites, CipherSuite.TLS_FALLBACK_SCSV));

                        state.server.NotifyOfferedCipherSuites(state.offeredCipherSuites);

                        /*
                         * TODO[resumption] Check RFC 7627 5.4. for required behaviour 
                         */

                        /*
                         * RFC 5746 3.6. Server Behavior: Initial Handshake
                         */
                        {
                            /*
                             * RFC 5746 3.4. The client MUST include either an empty "renegotiation_info" extension,
                             * or the TLS_EMPTY_RENEGOTIATION_INFO_SCSV signaling cipher suite value in the
                             * ClientHello. Including both is NOT RECOMMENDED.
                             */

                            /*
                             * When a ClientHello is received, the server MUST check if it includes the
                             * TLS_EMPTY_RENEGOTIATION_INFO_SCSV SCSV. If it does, set the secure_renegotiation flag
                             * to TRUE.
                             */
                            if (Arrays.Contains(state.offeredCipherSuites, CipherSuite.TLS_EMPTY_RENEGOTIATION_INFO_SCSV)) securityParameters.m_secureRenegotiation = true;

                            /*
                             * The server MUST check if the "renegotiation_info" extension is included in the
                             * ClientHello.
                             */
                            var renegExtData = TlsUtilities.GetExtensionData(state.clientExtensions,
                                ExtensionType.renegotiation_info);
                            if (renegExtData != null)
                            {
                                /*
                                 * If the extension is present, set secure_renegotiation flag to TRUE. The
                                 * server MUST then verify that the length of the "renegotiated_connection"
                                 * field is zero, and if it is not, MUST abort the handshake.
                                 */
                                securityParameters.m_secureRenegotiation = true;

                                if (!Arrays.ConstantTimeAreEqual(renegExtData,
                                        TlsProtocol.CreateRenegotiationInfo(TlsUtilities.EmptyBytes)))
                                    throw new TlsFatalAlert(AlertDescription.handshake_failure);
                            }
                        }

                        state.server.NotifySecureRenegotiation(securityParameters.IsSecureRenegotiation);

                        state.offeredExtendedMasterSecret = TlsExtensionsUtilities.HasExtendedMasterSecretExtension(
                            state.clientExtensions);

                        if (state.clientExtensions != null)
                        {
                            // NOTE: Validates the padding extension data, if present
                            TlsExtensionsUtilities.GetPaddingExtension(state.clientExtensions);

                            securityParameters.m_clientServerNames = TlsExtensionsUtilities.GetServerNameExtensionClient(
                                state.clientExtensions);

                            /*
                             * RFC 5246 7.4.1.4.1. Note: this extension is not meaningful for TLS versions prior
                             * to 1.2. Clients MUST NOT offer it if they are offering prior versions.
                             */
                            if (TlsUtilities.IsSignatureAlgorithmsExtensionAllowed(client_version)) TlsUtilities.EstablishClientSigAlgs(securityParameters, state.clientExtensions);

                            securityParameters.m_clientSupportedGroups = TlsExtensionsUtilities.GetSupportedGroupsExtension(
                                state.clientExtensions);

                            // Heartbeats
                            {
                                var heartbeatExtension = TlsExtensionsUtilities.GetHeartbeatExtension(
                                    state.clientExtensions);
                                if (null != heartbeatExtension)
                                {
                                    if (HeartbeatMode.peer_allowed_to_send == heartbeatExtension.Mode) state.heartbeat = state.server.GetHeartbeat();

                                    state.heartbeatPolicy = state.server.GetHeartbeatPolicy();
                                }
                            }

                            state.server.ProcessClientExtensions(state.clientExtensions);
                        }
                    }

                    /// <exception cref="IOException" />
                    protected virtual void ProcessClientKeyExchange(ServerHandshakeState state, byte[] body)
                    {
                        var buf = new MemoryStream(body, false);
                        state.keyExchange.ProcessClientKeyExchange(buf);
                        TlsProtocol.AssertEmpty(buf);
                    }

                    /// <exception cref="IOException" />
                    protected virtual void ProcessClientSupplementalData(ServerHandshakeState state, byte[] body)
                    {
                        var buf                    = new MemoryStream(body, false);
                        var clientSupplementalData = TlsProtocol.ReadSupplementalDataMessage(buf);
                        state.server.ProcessClientSupplementalData(clientSupplementalData);
                    }

                    protected virtual bool ExpectCertificateVerifyMessage(ServerHandshakeState state)
                    {
                        if (null == state.certificateRequest)
                            return false;

                        var clientCertificate = state.serverContext.SecurityParameters.PeerCertificate;

                        return null != clientCertificate && !clientCertificate.IsEmpty
                                                         && (null == state.keyExchange || state.keyExchange.RequiresCertificateVerify);
                    }

                    protected internal class ServerHandshakeState
                    {
                        internal TlsServer                 server;
                        internal TlsServerContextImpl      serverContext;
                        internal TlsSession                tlsSession;
                        internal SessionParameters         sessionParameters;
                        internal TlsSecret                 sessionMasterSecret;
                        internal SessionParameters.Builder sessionParametersBuilder = null;
                        internal int[]                     offeredCipherSuites;
                        internal IDictionary               clientExtensions;
                        internal IDictionary               serverExtensions;
                        internal bool                      offeredExtendedMasterSecret;
                        internal bool                      resumedSession = false;
                        internal bool                      expectSessionTicket;
                        internal TlsKeyExchange            keyExchange;
                        internal TlsCredentials            serverCredentials;
                        internal CertificateRequest        certificateRequest;
                        internal TlsHeartbeat              heartbeat;
                        internal short                     heartbeatPolicy = HeartbeatMode.peer_not_allowed_to_send;
                    }
                    }
                    }
#pragma warning restore
#endif