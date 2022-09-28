#if !BESTHTTP_DISABLE_ALTERNATE_SSL && (!UNITY_WEBGL || UNITY_EDITOR)
#pragma warning disable
namespace BestHTTP.SecureProtocol.Org.BouncyCastle.Tls
{
    using System;
    using System.Collections;
    using System.IO;
    using BestHTTP.SecureProtocol.Org.BouncyCastle.Tls.Crypto;
    using BestHTTP.SecureProtocol.Org.BouncyCastle.Utilities;

    public class TlsServerProtocol
        : TlsProtocol
    {
        protected TlsServer            m_tlsServer;
        internal  TlsServerContextImpl m_tlsServerContext;

        protected int[]              m_offeredCipherSuites;
        protected TlsKeyExchange     m_keyExchange;
        protected CertificateRequest m_certificateRequest;

        /// <summary>Constructor for non-blocking mode.</summary>
        /// <remarks>
        ///     When data is received, use <see cref="TlsProtocol.OfferInput(byte[])" /> to provide the received ciphertext,
        ///     then use <see cref="TlsProtocol.ReadInput(byte[],int,int)" /> to read the corresponding cleartext.<br /><br />
        ///     Similarly, when data needs to be sent, use <see cref="TlsProtocol.WriteApplicationData(byte[],int,int)" />
        ///     to provide the cleartext, then use <see cref="TlsProtocol.ReadOutput(byte[],int,int)" /> to get the
        ///     corresponding ciphertext.
        /// </remarks>
        public TlsServerProtocol() { }

        /// <summary>Constructor for blocking mode.</summary>
        /// <param name="stream">The <see cref="Stream" /> of data to/from the server.</param>
        public TlsServerProtocol(Stream stream)
            : base(stream)
        {
        }

        /// <summary>Constructor for blocking mode.</summary>
        /// <param name="input">The <see cref="Stream" /> of data from the server.</param>
        /// <param name="output">The <see cref="Stream" /> of data to the server.</param>
        public TlsServerProtocol(Stream input, Stream output)
            : base(input, output)
        {
        }

        /// <summary>Receives a TLS handshake in the role of server.</summary>
        /// <remarks>
        ///     In blocking mode, this will not return until the handshake is complete. In non-blocking mode, use
        ///     <see cref="TlsPeer.NotifyHandshakeComplete" /> to receive a callback when the handshake is complete.
        /// </remarks>
        /// <param name="tlsServer">The <see cref="TlsServer" /> to use for the handshake.</param>
        /// <exception cref="IOException">If in blocking mode and handshake was not successful.</exception>
        public void Accept(TlsServer tlsServer)
        {
            if (tlsServer == null)
                throw new ArgumentNullException("tlsServer");
            if (this.m_tlsServer != null)
                throw new InvalidOperationException("'Accept' can only be called once");

            this.m_tlsServer        = tlsServer;
            this.m_tlsServerContext = new TlsServerContextImpl(tlsServer.Crypto);

            tlsServer.Init(this.m_tlsServerContext);
            tlsServer.NotifyCloseHandle(this);

            this.BeginHandshake();

            if (this.m_blocking) this.BlockForHandshake();
        }

        protected override void CleanupHandshake()
        {
            base.CleanupHandshake();

            this.m_offeredCipherSuites = null;
            this.m_keyExchange         = null;
            this.m_certificateRequest  = null;
        }

        protected virtual bool ExpectCertificateVerifyMessage()
        {
            if (null == this.m_certificateRequest)
                return false;

            var clientCertificate = this.m_tlsServerContext.SecurityParameters.PeerCertificate;

            return null != clientCertificate && !clientCertificate.IsEmpty
                                             && (null == this.m_keyExchange || this.m_keyExchange.RequiresCertificateVerify);
        }

        /// <exception cref="IOException" />
        protected virtual ServerHello Generate13HelloRetryRequest(ClientHello clientHello)
        {
            // TODO[tls13] In future there might be other reasons for a HelloRetryRequest.
            if (this.m_retryGroup < 0)
                throw new TlsFatalAlert(AlertDescription.internal_error);

            var securityParameters = this.m_tlsServerContext.SecurityParameters;
            var serverVersion      = securityParameters.NegotiatedVersion;

            var serverHelloExtensions = Platform.CreateHashtable();
            TlsExtensionsUtilities.AddSupportedVersionsExtensionServer(serverHelloExtensions, serverVersion);
            if (this.m_retryGroup >= 0) TlsExtensionsUtilities.AddKeyShareHelloRetryRequest(serverHelloExtensions, this.m_retryGroup);
            if (null != this.m_retryCookie) TlsExtensionsUtilities.AddCookieExtension(serverHelloExtensions, this.m_retryCookie);

            TlsUtilities.CheckExtensionData13(serverHelloExtensions, HandshakeType.hello_retry_request,
                AlertDescription.internal_error);

            return new ServerHello(clientHello.SessionID, securityParameters.CipherSuite, serverHelloExtensions);
        }

        /// <exception cref="IOException" />
        protected virtual ServerHello Generate13ServerHello(ClientHello clientHello,
            HandshakeMessageInput clientHelloMessage, bool afterHelloRetryRequest)
        {
            var securityParameters = this.m_tlsServerContext.SecurityParameters;


            var legacy_session_id = clientHello.SessionID;

            var clientHelloExtensions = clientHello.Extensions;
            if (null == clientHelloExtensions)
                throw new TlsFatalAlert(AlertDescription.missing_extension);


            var serverVersion = securityParameters.NegotiatedVersion;
            var crypto        = this.m_tlsServerContext.Crypto;

            // NOTE: Will only select for psk_dhe_ke
            var selectedPsk = TlsUtilities.SelectPreSharedKey(this.m_tlsServerContext, this.m_tlsServer,
                clientHelloExtensions, clientHelloMessage, this.m_handshakeHash, afterHelloRetryRequest);

            var           clientShares = TlsExtensionsUtilities.GetKeyShareClientHello(clientHelloExtensions);
            KeyShareEntry clientShare  = null;

            if (afterHelloRetryRequest)
            {
                if (this.m_retryGroup < 0)
                    throw new TlsFatalAlert(AlertDescription.internal_error);

                if (null == selectedPsk)
                {
                    /*
                     * RFC 8446 4.2.3. If a server is authenticating via a certificate and the client has
                     * not sent a "signature_algorithms" extension, then the server MUST abort the handshake
                     * with a "missing_extension" alert.
                     */
                    if (null == securityParameters.ClientSigAlgs)
                        throw new TlsFatalAlert(AlertDescription.missing_extension);
                }
                else
                {
                    // TODO[tls13] Maybe filter the offered PSKs by PRF algorithm before server selection instead
                    if (selectedPsk.m_psk.PrfAlgorithm != securityParameters.PrfAlgorithm)
                        throw new TlsFatalAlert(AlertDescription.illegal_parameter);
                }

                /*
                 * TODO[tls13] Confirm fields in the ClientHello haven't changed
                 * 
                 * RFC 8446 4.1.2 [..] when the server has responded to its ClientHello with a
                 * HelloRetryRequest [..] the client MUST send the same ClientHello without
                 * modification, except as follows: [key_share, early_data, cookie, pre_shared_key,
                 * padding].
                 */

                var cookie = TlsExtensionsUtilities.GetCookieExtension(clientHelloExtensions);
                if (!Arrays.AreEqual(this.m_retryCookie, cookie))
                    throw new TlsFatalAlert(AlertDescription.illegal_parameter);

                this.m_retryCookie = null;

                clientShare = TlsUtilities.SelectKeyShare(clientShares, this.m_retryGroup);
                if (null == clientShare)
                    throw new TlsFatalAlert(AlertDescription.illegal_parameter);
            }
            else
            {
                this.m_clientExtensions = clientHelloExtensions;

                securityParameters.m_secureRenegotiation = false;

                // NOTE: Validates the padding extension data, if present
                TlsExtensionsUtilities.GetPaddingExtension(clientHelloExtensions);

                securityParameters.m_clientServerNames = TlsExtensionsUtilities
                    .GetServerNameExtensionClient(clientHelloExtensions);

                TlsUtilities.EstablishClientSigAlgs(securityParameters, clientHelloExtensions);

                /*
                 * RFC 8446 4.2.3. If a server is authenticating via a certificate and the client has
                 * not sent a "signature_algorithms" extension, then the server MUST abort the handshake
                 * with a "missing_extension" alert.
                 */
                if (null == selectedPsk && null == securityParameters.ClientSigAlgs)
                    throw new TlsFatalAlert(AlertDescription.missing_extension);

                this.m_tlsServer.ProcessClientExtensions(clientHelloExtensions);

                /*
                 * NOTE: Currently no server support for session resumption
                 * 
                 * If adding support, ensure securityParameters.tlsUnique is set to the localVerifyData, but
                 * ONLY when extended_master_secret has been negotiated (otherwise NULL).
                 */
                {
                    // TODO[tls13] Resumption/PSK

                    this.m_tlsSession          = TlsUtilities.ImportSession(TlsUtilities.EmptyBytes, null);
                    this.m_sessionParameters   = null;
                    this.m_sessionMasterSecret = null;
                }

                securityParameters.m_sessionID = this.m_tlsSession.SessionID;

                this.m_tlsServer.NotifySession(this.m_tlsSession);

                TlsUtilities.NegotiatedVersionTlsServer(this.m_tlsServerContext);

                {
                    securityParameters.m_serverRandom = CreateRandomBlock(false, this.m_tlsServerContext);

                    if (!serverVersion.Equals(ProtocolVersion.GetLatestTls(this.m_tlsServer.GetProtocolVersions()))) TlsUtilities.WriteDowngradeMarker(serverVersion, securityParameters.ServerRandom);
                }

                {
                    // TODO[tls13] Constrain selection when PSK selected
                    var cipherSuite = this.m_tlsServer.GetSelectedCipherSuite();

                    if (!TlsUtilities.IsValidCipherSuiteSelection(this.m_offeredCipherSuites, cipherSuite) ||
                        !TlsUtilities.IsValidVersionForCipherSuite(cipherSuite, serverVersion))
                        throw new TlsFatalAlert(AlertDescription.internal_error);

                    TlsUtilities.NegotiatedCipherSuite(securityParameters, cipherSuite);
                }

                var clientSupportedGroups = securityParameters.ClientSupportedGroups;
                var serverSupportedGroups = securityParameters.ServerSupportedGroups;

                clientShare = TlsUtilities.SelectKeyShare(crypto, serverVersion, clientShares, clientSupportedGroups,
                    serverSupportedGroups);

                if (null == clientShare)
                {
                    this.m_retryGroup = TlsUtilities.SelectKeyShareGroup(crypto, serverVersion, clientSupportedGroups,
                        serverSupportedGroups);
                    if (this.m_retryGroup < 0)
                        throw new TlsFatalAlert(AlertDescription.handshake_failure);

                    this.m_retryCookie = this.m_tlsServerContext.NonceGenerator.GenerateNonce(16);

                    return this.Generate13HelloRetryRequest(clientHello);
                }

                if (clientShare.NamedGroup != serverSupportedGroups[0])
                {
                    /*
                     * TODO[tls13] RFC 8446 4.2.7. As of TLS 1.3, servers are permitted to send the
                     * "supported_groups" extension to the client. Clients MUST NOT act upon any
                     * information found in "supported_groups" prior to successful completion of the
                     * handshake but MAY use the information learned from a successfully completed
                     * handshake to change what groups they use in their "key_share" extension in
                     * subsequent connections. If the server has a group it prefers to the ones in the
                     * "key_share" extension but is still willing to accept the ClientHello, it SHOULD
                     * send "supported_groups" to update the client's view of its preferences; this
                     * extension SHOULD contain all groups the server supports, regardless of whether
                     * they are currently supported by the client.
                     */
                }
            }


            var serverHelloExtensions     = Platform.CreateHashtable();
            var serverEncryptedExtensions = TlsExtensionsUtilities.EnsureExtensionsInitialised(this.m_tlsServer.GetServerExtensions());

            this.m_tlsServer.GetServerExtensionsForConnection(serverEncryptedExtensions);

            var serverLegacyVersion = ProtocolVersion.TLSv12;
            TlsExtensionsUtilities.AddSupportedVersionsExtensionServer(serverHelloExtensions, serverVersion);

            /*
             * RFC 8446 Appendix D. Because TLS 1.3 always hashes in the transcript up to the server
             * Finished, implementations which support both TLS 1.3 and earlier versions SHOULD indicate
             * the use of the Extended Master Secret extension in their APIs whenever TLS 1.3 is used.
             */
            securityParameters.m_extendedMasterSecret = true;

            /*
             * RFC 7301 3.1. When session resumption or session tickets [...] are used, the previous
             * contents of this extension are irrelevant, and only the values in the new handshake
             * messages are considered.
             */
            securityParameters.m_applicationProtocol = TlsExtensionsUtilities.GetAlpnExtensionServer(
                serverEncryptedExtensions);
            securityParameters.m_applicationProtocolSet = true;

            if (serverEncryptedExtensions.Count > 0)
                securityParameters.m_maxFragmentLength = this.ProcessMaxFragmentLengthExtension(clientHelloExtensions,
                    serverEncryptedExtensions, AlertDescription.internal_error);

            securityParameters.m_encryptThenMac = false;
            securityParameters.m_truncatedHmac  = false;

            /*
             * TODO[tls13] RFC 8446 4.4.2.1. OCSP Status and SCT Extensions.
             * 
             * OCSP information is carried in an extension for a CertificateEntry.
             */
            securityParameters.m_statusRequestVersion = clientHelloExtensions.Contains(ExtensionType.status_request)
                ? 1
                : 0;

            this.m_expectSessionTicket = false;

            TlsSecret pskEarlySecret = null;
            if (null != selectedPsk)
            {
                pskEarlySecret = selectedPsk.m_earlySecret;

                this.m_selectedPsk13 = true;

                TlsExtensionsUtilities.AddPreSharedKeyServerHello(serverHelloExtensions, selectedPsk.m_index);
            }

            TlsSecret sharedSecret;
            {
                var namedGroup = clientShare.NamedGroup;

                TlsAgreement agreement;
                if (NamedGroup.RefersToASpecificCurve(namedGroup))
                    agreement = crypto.CreateECDomain(new TlsECConfig(namedGroup)).CreateECDH();
                else if (NamedGroup.RefersToASpecificFiniteField(namedGroup))
                    agreement = crypto.CreateDHDomain(new TlsDHConfig(namedGroup, true)).CreateDH();
                else
                    throw new TlsFatalAlert(AlertDescription.internal_error);

                var key_exchange = agreement.GenerateEphemeral();
                var serverShare  = new KeyShareEntry(namedGroup, key_exchange);
                TlsExtensionsUtilities.AddKeyShareServerHello(serverHelloExtensions, serverShare);

                agreement.ReceivePeerValue(clientShare.KeyExchange);
                sharedSecret = agreement.CalculateSecret();
            }

            TlsUtilities.Establish13PhaseSecrets(this.m_tlsServerContext, pskEarlySecret, sharedSecret);

            this.m_serverExtensions = serverEncryptedExtensions;

            this.ApplyMaxFragmentLengthExtension(securityParameters.MaxFragmentLength);

            TlsUtilities.CheckExtensionData13(serverHelloExtensions, HandshakeType.server_hello,
                AlertDescription.internal_error);

            return new ServerHello(serverLegacyVersion, securityParameters.ServerRandom, legacy_session_id,
                securityParameters.CipherSuite, serverHelloExtensions);
        }

        /// <exception cref="IOException" />
        protected virtual ServerHello GenerateServerHello(ClientHello clientHello,
            HandshakeMessageInput clientHelloMessage)
        {
            var clientLegacyVersion = clientHello.Version;
            if (!clientLegacyVersion.IsTls)
                throw new TlsFatalAlert(AlertDescription.illegal_parameter);

            this.m_offeredCipherSuites = clientHello.CipherSuites;


            var securityParameters = this.m_tlsServerContext.SecurityParameters;

            this.m_tlsServerContext.SetClientSupportedVersions(
                TlsExtensionsUtilities.GetSupportedVersionsExtensionClient(clientHello.Extensions));

            var clientVersion = clientLegacyVersion;
            if (null == this.m_tlsServerContext.ClientSupportedVersions)
            {
                if (clientVersion.IsLaterVersionOf(ProtocolVersion.TLSv12)) clientVersion = ProtocolVersion.TLSv12;

                this.m_tlsServerContext.SetClientSupportedVersions(clientVersion.DownTo(ProtocolVersion.SSLv3));
            }
            else
            {
                clientVersion = ProtocolVersion.GetLatestTls(this.m_tlsServerContext.ClientSupportedVersions);
            }

            // Set the legacy_record_version to use for early alerts 
            this.m_recordStream.SetWriteVersion(clientVersion);

            if (!ProtocolVersion.SERVER_EARLIEST_SUPPORTED_TLS.IsEqualOrEarlierVersionOf(clientVersion))
                throw new TlsFatalAlert(AlertDescription.protocol_version);

            // NOT renegotiating
            {
                this.m_tlsServerContext.SetClientVersion(clientVersion);
            }

            this.m_tlsServer.NotifyClientVersion(this.m_tlsServerContext.ClientVersion);

            securityParameters.m_clientRandom = clientHello.Random;

            this.m_tlsServer.NotifyFallback(Arrays.Contains(this.m_offeredCipherSuites, CipherSuite.TLS_FALLBACK_SCSV));

            this.m_tlsServer.NotifyOfferedCipherSuites(this.m_offeredCipherSuites);

            // TODO[tls13] Negotiate cipher suite first?

            ProtocolVersion serverVersion;

            // NOT renegotiating
            {
                serverVersion = this.m_tlsServer.GetServerVersion();
                if (!ProtocolVersion.Contains(this.m_tlsServerContext.ClientSupportedVersions, serverVersion))
                    throw new TlsFatalAlert(AlertDescription.internal_error);

                securityParameters.m_negotiatedVersion = serverVersion;
            }

            securityParameters.m_clientSupportedGroups = TlsExtensionsUtilities.GetSupportedGroupsExtension(
                clientHello.Extensions);
            securityParameters.m_serverSupportedGroups = this.m_tlsServer.GetSupportedGroups();

            if (ProtocolVersion.TLSv13.IsEqualOrEarlierVersionOf(serverVersion))
            {
                // See RFC 8446 D.4.
                this.m_recordStream.SetIgnoreChangeCipherSpec(true);

                this.m_recordStream.SetWriteVersion(ProtocolVersion.TLSv12);

                return this.Generate13ServerHello(clientHello, clientHelloMessage, false);
            }

            this.m_recordStream.SetWriteVersion(serverVersion);

            this.m_clientExtensions = clientHello.Extensions;

            var clientRenegExtData = TlsUtilities.GetExtensionData(this.m_clientExtensions, ExtensionType.renegotiation_info);

            // NOT renegotiating
            {
                /*
                 * RFC 5746 3.6. Server Behavior: Initial Handshake (both full and session-resumption)
                 */

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
                if (Arrays.Contains(this.m_offeredCipherSuites, CipherSuite.TLS_EMPTY_RENEGOTIATION_INFO_SCSV)) securityParameters.m_secureRenegotiation = true;

                /*
                 * The server MUST check if the "renegotiation_info" extension is included in the
                 * ClientHello.
                 */
                if (clientRenegExtData != null)
                {
                    /*
                     * If the extension is present, set secure_renegotiation flag to TRUE. The
                     * server MUST then verify that the length of the "renegotiated_connection"
                     * field is zero, and if it is not, MUST abort the handshake.
                     */
                    securityParameters.m_secureRenegotiation = true;

                    if (!Arrays.ConstantTimeAreEqual(clientRenegExtData,
                            CreateRenegotiationInfo(TlsUtilities.EmptyBytes)))
                        throw new TlsFatalAlert(AlertDescription.handshake_failure);
                }
            }

            this.m_tlsServer.NotifySecureRenegotiation(securityParameters.IsSecureRenegotiation);

            var offeredExtendedMasterSecret = TlsExtensionsUtilities.HasExtendedMasterSecretExtension(this.m_clientExtensions);

            if (this.m_clientExtensions != null)
            {
                // NOTE: Validates the padding extension data, if present
                TlsExtensionsUtilities.GetPaddingExtension(this.m_clientExtensions);

                securityParameters.m_clientServerNames = TlsExtensionsUtilities.GetServerNameExtensionClient(this.m_clientExtensions);

                /*
                 * RFC 5246 7.4.1.4.1. Note: this extension is not meaningful for TLS versions prior
                 * to 1.2. Clients MUST NOT offer it if they are offering prior versions.
                 */
                if (TlsUtilities.IsSignatureAlgorithmsExtensionAllowed(clientVersion)) TlsUtilities.EstablishClientSigAlgs(securityParameters, this.m_clientExtensions);

                securityParameters.m_clientSupportedGroups = TlsExtensionsUtilities.GetSupportedGroupsExtension(this.m_clientExtensions);

                this.m_tlsServer.ProcessClientExtensions(this.m_clientExtensions);
            }

            this.m_resumedSession = this.EstablishSession(this.m_tlsServer.GetSessionToResume(clientHello.SessionID));

            if (!this.m_resumedSession)
            {
                var newSessionID                       = this.m_tlsServer.GetNewSessionID();
                if (null == newSessionID) newSessionID = TlsUtilities.EmptyBytes;

                this.m_tlsSession          = TlsUtilities.ImportSession(newSessionID, null);
                this.m_sessionParameters   = null;
                this.m_sessionMasterSecret = null;
            }

            securityParameters.m_sessionID = this.m_tlsSession.SessionID;

            this.m_tlsServer.NotifySession(this.m_tlsSession);

            TlsUtilities.NegotiatedVersionTlsServer(this.m_tlsServerContext);

            {
                var useGmtUnixTime = this.m_tlsServer.ShouldUseGmtUnixTime();

                securityParameters.m_serverRandom = CreateRandomBlock(useGmtUnixTime, this.m_tlsServerContext);

                if (!serverVersion.Equals(ProtocolVersion.GetLatestTls(this.m_tlsServer.GetProtocolVersions()))) TlsUtilities.WriteDowngradeMarker(serverVersion, securityParameters.ServerRandom);
            }

            {
                var cipherSuite = this.m_resumedSession
                    ? this.m_sessionParameters.CipherSuite
                    : this.m_tlsServer.GetSelectedCipherSuite();

                if (!TlsUtilities.IsValidCipherSuiteSelection(this.m_offeredCipherSuites, cipherSuite) ||
                    !TlsUtilities.IsValidVersionForCipherSuite(cipherSuite, serverVersion))
                    throw new TlsFatalAlert(AlertDescription.internal_error);

                TlsUtilities.NegotiatedCipherSuite(securityParameters, cipherSuite);
            }

            this.m_tlsServerContext.SetRsaPreMasterSecretVersion(clientLegacyVersion);

            {
                var sessionServerExtensions = this.m_resumedSession
                    ? this.m_sessionParameters.ReadServerExtensions()
                    : this.m_tlsServer.GetServerExtensions();

                this.m_serverExtensions = TlsExtensionsUtilities.EnsureExtensionsInitialised(sessionServerExtensions);
            }

            this.m_tlsServer.GetServerExtensionsForConnection(this.m_serverExtensions);

            // NOT renegotiating
            {
                /*
                 * RFC 5746 3.6. Server Behavior: Initial Handshake (both full and session-resumption)
                 */
                if (securityParameters.IsSecureRenegotiation)
                {
                    var serverRenegExtData = TlsUtilities.GetExtensionData(this.m_serverExtensions,
                        ExtensionType.renegotiation_info);
                    var noRenegExt = null == serverRenegExtData;

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
                        this.m_serverExtensions[ExtensionType.renegotiation_info] = CreateRenegotiationInfo(
                            TlsUtilities.EmptyBytes);
                }
            }

            /*
             * RFC 7627 4. Clients and servers SHOULD NOT accept handshakes that do not use the extended
             * master secret [..]. (and see 5.2, 5.3)
             */
            if (this.m_resumedSession)
            {
                if (!this.m_sessionParameters.IsExtendedMasterSecret)
                    /*
                         * TODO[resumption] ProvTlsServer currently only resumes EMS sessions. Revisit this
                         * in relation to 'tlsServer.allowLegacyResumption()'.
                         */
                        throw new TlsFatalAlert(AlertDescription.internal_error);

                    if (!offeredExtendedMasterSecret)
                        throw new TlsFatalAlert(AlertDescription.handshake_failure);

                    securityParameters.m_extendedMasterSecret = true;

                    TlsExtensionsUtilities.AddExtendedMasterSecretExtension(this.m_serverExtensions);
                    }
                    else
                    {
                        securityParameters.m_extendedMasterSecret = offeredExtendedMasterSecret && !serverVersion.IsSsl
                                                                                                && this.m_tlsServer.ShouldUseExtendedMasterSecret();

                        if (securityParameters.IsExtendedMasterSecret)
                            TlsExtensionsUtilities.AddExtendedMasterSecretExtension(this.m_serverExtensions);
                        else if (this.m_tlsServer.RequiresExtendedMasterSecret()) throw new TlsFatalAlert(AlertDescription.handshake_failure);
                    }

                    securityParameters.m_applicationProtocol    = TlsExtensionsUtilities.GetAlpnExtensionServer(this.m_serverExtensions);
                    securityParameters.m_applicationProtocolSet = true;

                    if (this.m_serverExtensions.Count > 0)
                    {
                        securityParameters.m_encryptThenMac = TlsExtensionsUtilities.HasEncryptThenMacExtension(this.m_serverExtensions);

                        securityParameters.m_maxFragmentLength = this.ProcessMaxFragmentLengthExtension(this.m_clientExtensions, this.m_serverExtensions, AlertDescription.internal_error);

                        securityParameters.m_truncatedHmac = TlsExtensionsUtilities.HasTruncatedHmacExtension(this.m_serverExtensions);

                        if (!this.m_resumedSession)
                        {
                            if (TlsUtilities.HasExpectedEmptyExtensionData(this.m_serverExtensions, ExtensionType.status_request_v2,
                                    AlertDescription.internal_error))
                                securityParameters.m_statusRequestVersion = 2;
                            else if (TlsUtilities.HasExpectedEmptyExtensionData(this.m_serverExtensions, ExtensionType.status_request,
                                         AlertDescription.internal_error))
                                securityParameters.m_statusRequestVersion = 1;

                            this.m_expectSessionTicket = TlsUtilities.HasExpectedEmptyExtensionData(this.m_serverExtensions,
                                ExtensionType.session_ticket, AlertDescription.internal_error);
                        }
                    }

                    this.ApplyMaxFragmentLengthExtension(securityParameters.MaxFragmentLength);

                    return new ServerHello(serverVersion, securityParameters.ServerRandom, this.m_tlsSession.SessionID,
                        securityParameters.CipherSuite, this.m_serverExtensions);
                    }

                    protected override TlsContext Context => this.m_tlsServerContext;

                    internal override AbstractTlsContext ContextAdmin => this.m_tlsServerContext;

                    protected override TlsPeer Peer => this.m_tlsServer;

                    /// <exception cref="IOException" />
                    protected virtual void Handle13HandshakeMessage(short type, HandshakeMessageInput buf)
                    {
                        if (!this.IsTlsV13ConnectionState())
                            throw new TlsFatalAlert(AlertDescription.internal_error);

                        if (this.m_resumedSession)
                            /*
                                 * TODO[tls13] Abbreviated handshakes (PSK resumption)
                                 * 
                                 * NOTE: No CertificateRequest, Certificate, CertificateVerify messages, but client
                                 * might now send EndOfEarlyData after receiving server Finished message.
                                 */
                                throw new TlsFatalAlert(AlertDescription.internal_error);

                            switch (type)
                            {
                                case HandshakeType.certificate:
                                {
                                    switch (this.m_connectionState)
                                    {
                                        case CS_SERVER_FINISHED:
                                        {
                                            this.Receive13ClientCertificate(buf);
                                            this.m_connectionState = CS_CLIENT_CERTIFICATE;
                                            break;
                                        }
                                        default:
                                            throw new TlsFatalAlert(AlertDescription.unexpected_message);
                                    }

                                    break;
                                }
                                case HandshakeType.certificate_verify:
                                {
                                    switch (this.m_connectionState)
                                    {
                                        case CS_CLIENT_CERTIFICATE:
                                        {
                                            this.Receive13ClientCertificateVerify(buf);
                                            buf.UpdateHash(this.m_handshakeHash);
                                            this.m_connectionState = CS_CLIENT_CERTIFICATE_VERIFY;
                                            break;
                                        }
                                        default:
                                            throw new TlsFatalAlert(AlertDescription.unexpected_message);
                                    }

                                    break;
                                }
                                case HandshakeType.client_hello:
                                {
                                    switch (this.m_connectionState)
                                    {
                                        case CS_START:
                                        {
                                            // NOTE: Legacy handler should be dispatching initial ClientHello.
                                            throw new TlsFatalAlert(AlertDescription.internal_error);
                                        }
                                        case CS_SERVER_HELLO_RETRY_REQUEST:
                                        {
                                            var clientHelloRetry = this.ReceiveClientHelloMessage(buf);
                                            this.m_connectionState = CS_CLIENT_HELLO_RETRY;

                                            var serverHello = this.Generate13ServerHello(clientHelloRetry, buf, true);
                                            this.SendServerHelloMessage(serverHello);
                                            this.m_connectionState = CS_SERVER_HELLO;

                                            this.Send13ServerHelloCoda(serverHello, true);
                                            break;
                                        }
                                        default:
                                            throw new TlsFatalAlert(AlertDescription.unexpected_message);
                                    }

                                    break;
                                }
                                case HandshakeType.finished:
                                {
                                    switch (this.m_connectionState)
                                    {
                                        case CS_SERVER_FINISHED:
                                        case CS_CLIENT_CERTIFICATE:
                                        case CS_CLIENT_CERTIFICATE_VERIFY:
                                        {
                                            if (this.m_connectionState == CS_SERVER_FINISHED) this.Skip13ClientCertificate();
                                            if (this.m_connectionState != CS_CLIENT_CERTIFICATE_VERIFY) this.Skip13ClientCertificateVerify();

                                            this.Receive13ClientFinished(buf);
                                            this.m_connectionState = CS_CLIENT_FINISHED;

                                            // See RFC 8446 D.4.
                                            this.m_recordStream.SetIgnoreChangeCipherSpec(false);

                                            // NOTE: Completes the switch to application-data phase (server entered after CS_SERVER_FINISHED).
                                            this.m_recordStream.EnablePendingCipherRead(false);

                                            this.CompleteHandshake();
                                            break;
                                        }
                                        default:
                                            throw new TlsFatalAlert(AlertDescription.unexpected_message);
                                    }

                                    break;
                                }
                                case HandshakeType.key_update:
                                {
                                    this.Receive13KeyUpdate(buf);
                                    break;
                                }

                                case HandshakeType.certificate_request:
                                case HandshakeType.certificate_status:
                                case HandshakeType.certificate_url:
                                case HandshakeType.client_key_exchange:
                                case HandshakeType.encrypted_extensions:
                                case HandshakeType.end_of_early_data:
                                case HandshakeType.hello_request:
                                case HandshakeType.hello_verify_request:
                                case HandshakeType.message_hash:
                                case HandshakeType.new_session_ticket:
                                case HandshakeType.server_hello:
                                case HandshakeType.server_hello_done:
                                case HandshakeType.server_key_exchange:
                                case HandshakeType.supplemental_data:
                                default:
                                    throw new TlsFatalAlert(AlertDescription.unexpected_message);
                            }
                            }

                            protected override void HandleHandshakeMessage(short type, HandshakeMessageInput buf)
                            {
                                var securityParameters = this.m_tlsServerContext.SecurityParameters;

                                if (this.m_connectionState > CS_CLIENT_HELLO
                                    && TlsUtilities.IsTlsV13(securityParameters.NegotiatedVersion))
                                {
                                    this.Handle13HandshakeMessage(type, buf);
                                    return;
                                }

                                if (!this.IsLegacyConnectionState())
                                    throw new TlsFatalAlert(AlertDescription.internal_error);

                                if (this.m_resumedSession)
                                {
                                    if (type != HandshakeType.finished || this.m_connectionState != CS_SERVER_FINISHED)
                                        throw new TlsFatalAlert(AlertDescription.unexpected_message);

                                    this.ProcessFinishedMessage(buf);
                                    this.m_connectionState = CS_CLIENT_FINISHED;

                                    this.CompleteHandshake();
                                    return;
                                }

                                switch (type)
                                {
                                    case HandshakeType.client_hello:
                                    {
                                        if (this.IsApplicationDataReady)
                                        {
                                            this.RefuseRenegotiation();
                                            break;
                                        }

                                        switch (this.m_connectionState)
                                        {
                                            case CS_END:
                                            {
                                                throw new TlsFatalAlert(AlertDescription.internal_error);
                                            }
                                            case CS_START:
                                            {
                                                var clientHello = this.ReceiveClientHelloMessage(buf);
                                                this.m_connectionState = CS_CLIENT_HELLO;

                                                var serverHello = this.GenerateServerHello(clientHello, buf);
                                                this.m_handshakeHash.NotifyPrfDetermined();

                                                if (TlsUtilities.IsTlsV13(securityParameters.NegotiatedVersion))
                                                {
                                                    this.m_handshakeHash.SealHashAlgorithms();

                                                    if (serverHello.IsHelloRetryRequest())
                                                    {
                                                        TlsUtilities.AdjustTranscriptForRetry(this.m_handshakeHash);
                                                        this.SendServerHelloMessage(serverHello);
                                                        this.m_connectionState = CS_SERVER_HELLO_RETRY_REQUEST;

                                                        // See RFC 8446 D.4.
                                                        this.SendChangeCipherSpecMessage();
                                                    }
                                                    else
                                                    {
                                                        this.SendServerHelloMessage(serverHello);
                                                        this.m_connectionState = CS_SERVER_HELLO;

                                                        // See RFC 8446 D.4.
                                                        this.SendChangeCipherSpecMessage();

                                                        this.Send13ServerHelloCoda(serverHello, false);
                                                    }

                                                    break;
                                                }

                                                // For TLS 1.3+, this was already done by GenerateServerHello
                                                buf.UpdateHash(this.m_handshakeHash);

                                                this.SendServerHelloMessage(serverHello);
                                                this.m_connectionState = CS_SERVER_HELLO;

                                                if (this.m_resumedSession)
                                                {
                                                    securityParameters.m_masterSecret = this.m_sessionMasterSecret;
                                                    this.m_recordStream.SetPendingCipher(TlsUtilities.InitCipher(this.m_tlsServerContext));

                                                    this.SendChangeCipherSpec();
                                                    this.SendFinishedMessage();
                                                    this.m_connectionState = CS_SERVER_FINISHED;
                                                    break;
                                                }

                                                var serverSupplementalData = this.m_tlsServer.GetServerSupplementalData();
                                                if (serverSupplementalData != null)
                                                {
                                                    this.SendSupplementalDataMessage(serverSupplementalData);
                                                    this.m_connectionState = CS_SERVER_SUPPLEMENTAL_DATA;
                                                }

                                                this.m_keyExchange = TlsUtilities.InitKeyExchangeServer(this.m_tlsServerContext, this.m_tlsServer);

                                                var serverCredentials = TlsUtilities.EstablishServerCredentials(this.m_tlsServer);

                                                // Server certificate
                                                {
                                                    Certificate serverCertificate = null;

                                                    var endPointHash = new MemoryStream();
                                                    if (null == serverCredentials)
                                                    {
                                                        this.m_keyExchange.SkipServerCredentials();
                                                    }
                                                    else
                                                    {
                                                        this.m_keyExchange.ProcessServerCredentials(serverCredentials);

                                                        serverCertificate = serverCredentials.Certificate;
                                                        this.SendCertificateMessage(serverCertificate, endPointHash);
                                                        this.m_connectionState = CS_SERVER_CERTIFICATE;
                                                    }

                                                    securityParameters.m_tlsServerEndPoint = endPointHash.ToArray();

                                                    // TODO[RFC 3546] Check whether empty certificates is possible, allowed, or excludes
                                                    // CertificateStatus
                                                    if (null == serverCertificate || serverCertificate.IsEmpty) securityParameters.m_statusRequestVersion = 0;
                                                }

                                                if (securityParameters.StatusRequestVersion > 0)
                                                {
                                                    var certificateStatus = this.m_tlsServer.GetCertificateStatus();
                                                    if (certificateStatus != null)
                                                    {
                                                        this.SendCertificateStatusMessage(certificateStatus);
                                                        this.m_connectionState = CS_SERVER_CERTIFICATE_STATUS;
                                                    }
                                                }

                                                var serverKeyExchange = this.m_keyExchange.GenerateServerKeyExchange();
                                                if (serverKeyExchange != null)
                                                {
                                                    this.SendServerKeyExchangeMessage(serverKeyExchange);
                                                    this.m_connectionState = CS_SERVER_KEY_EXCHANGE;
                                                }

                                                if (null != serverCredentials)
                                                {
                                                    this.m_certificateRequest = this.m_tlsServer.GetCertificateRequest();

                                                    if (null == this.m_certificateRequest)
                                                    {
                                                        /*
                                                         * For static agreement key exchanges, CertificateRequest is required since
                                                         * the client Certificate message is mandatory but can only be sent if the
                                                         * server requests it.
                                                         */
                                                        if (!this.m_keyExchange.RequiresCertificateVerify)
                                                            throw new TlsFatalAlert(AlertDescription.internal_error);
                                                    }
                                                    else
                                                    {
                                                        if (TlsUtilities.IsTlsV12(this.m_tlsServerContext)
                                                            != (this.m_certificateRequest.SupportedSignatureAlgorithms != null))
                                                            throw new TlsFatalAlert(AlertDescription.internal_error);

                                                        this.m_certificateRequest = TlsUtilities.ValidateCertificateRequest(this.m_certificateRequest, this.m_keyExchange);

                                                        TlsUtilities.EstablishServerSigAlgs(securityParameters, this.m_certificateRequest);

                                                        TlsUtilities.TrackHashAlgorithms(this.m_handshakeHash, securityParameters.ServerSigAlgs);

                                                        this.SendCertificateRequestMessage(this.m_certificateRequest);
                                                        this.m_connectionState = CS_SERVER_CERTIFICATE_REQUEST;
                                                    }
                                                }

                                                this.SendServerHelloDoneMessage();
                                                this.m_connectionState = CS_SERVER_HELLO_DONE;

                                                var forceBuffering = false;
                                                TlsUtilities.SealHandshakeHash(this.m_tlsServerContext, this.m_handshakeHash, forceBuffering);

                                                break;
                                            }
                                            default:
                                                throw new TlsFatalAlert(AlertDescription.unexpected_message);
                                        }

                                        break;
                                    }
                                    case HandshakeType.supplemental_data:
                                    {
                                        switch (this.m_connectionState)
                                        {
                                            case CS_SERVER_HELLO_DONE:
                                            {
                                                this.m_tlsServer.ProcessClientSupplementalData(ReadSupplementalDataMessage(buf));
                                                this.m_connectionState = CS_CLIENT_SUPPLEMENTAL_DATA;
                                                break;
                                            }
                                            default:
                                                throw new TlsFatalAlert(AlertDescription.unexpected_message);
                                        }

                                        break;
                                    }
                                    case HandshakeType.certificate:
                                    {
                                        switch (this.m_connectionState)
                                        {
                                            case CS_SERVER_HELLO_DONE:
                                            case CS_CLIENT_SUPPLEMENTAL_DATA:
                                            {
                                                if (this.m_connectionState != CS_CLIENT_SUPPLEMENTAL_DATA) this.m_tlsServer.ProcessClientSupplementalData(null);

                                                this.ReceiveCertificateMessage(buf);
                                                this.m_connectionState = CS_CLIENT_CERTIFICATE;
                                                break;
                                            }
                                            default:
                                                throw new TlsFatalAlert(AlertDescription.unexpected_message);
                                        }

                                        break;
                                    }
                                    case HandshakeType.client_key_exchange:
                                    {
                                        switch (this.m_connectionState)
                                        {
                                            case CS_SERVER_HELLO_DONE:
                                            case CS_CLIENT_SUPPLEMENTAL_DATA:
                                            case CS_CLIENT_CERTIFICATE:
                                            {
                                                if (this.m_connectionState == CS_SERVER_HELLO_DONE) this.m_tlsServer.ProcessClientSupplementalData(null);
                                                if (this.m_connectionState != CS_CLIENT_CERTIFICATE)
                                                {
                                                    if (null == this.m_certificateRequest)
                                                        this.m_keyExchange.SkipClientCredentials();
                                                    else if (TlsUtilities.IsTlsV12(this.m_tlsServerContext))
                                                        /*
                                                             * RFC 5246 If no suitable certificate is available, the client MUST send a
                                                             * certificate message containing no certificates.
                                                             * 
                                                             * NOTE: In previous RFCs, this was SHOULD instead of MUST.
                                                             */
                                                            throw new TlsFatalAlert(AlertDescription.unexpected_message);
                                                        else if (TlsUtilities.IsSsl(this.m_tlsServerContext))
                                                            /*
                                                                 * SSL 3.0 If the server has sent a certificate request Message, the client must
                                                                 * send either the certificate message or a no_certificate alert.
                                                                 */
                                                                throw new TlsFatalAlert(AlertDescription.unexpected_message);
                                                            else
                                                                this.NotifyClientCertificate(Certificate.EmptyChain);
                                                            }

                                                            this.ReceiveClientKeyExchangeMessage(buf);
                                                            this.m_connectionState = CS_CLIENT_KEY_EXCHANGE;
                                                            break;
                                                            }
                                                            default:
                                                                throw new TlsFatalAlert(AlertDescription.unexpected_message);
                                                            }

                                                            break;
                                                            }
                                                            case HandshakeType.certificate_verify:
                                                            {
                                                                switch (this.m_connectionState)
                                                                {
                                                                    case CS_CLIENT_KEY_EXCHANGE:
                                                                    {
                                                                        /*
                                                                         * RFC 5246 7.4.8 This message is only sent following a client certificate that has
                                                                         * signing capability (i.e., all certificates except those containing fixed
                                                                         * Diffie-Hellman parameters).
                                                                         */
                                                                        if (!this.ExpectCertificateVerifyMessage())
                                                                            throw new TlsFatalAlert(AlertDescription.unexpected_message);

                                                                        this.ReceiveCertificateVerifyMessage(buf);
                                                                        buf.UpdateHash(this.m_handshakeHash);
                                                                        this.m_connectionState = CS_CLIENT_CERTIFICATE_VERIFY;
                                                                        break;
                                                                    }
                                                                    default:
                                                                        throw new TlsFatalAlert(AlertDescription.unexpected_message);
                                                                }

                                                                break;
                                                            }
                                                            case HandshakeType.finished:
                                                            {
                                                                switch (this.m_connectionState)
                                                                {
                                                                    case CS_CLIENT_KEY_EXCHANGE:
                                                                    case CS_CLIENT_CERTIFICATE_VERIFY:
                                                                    {
                                                                        if (this.m_connectionState != CS_CLIENT_CERTIFICATE_VERIFY)
                                                                            if (this.ExpectCertificateVerifyMessage())
                                                                                throw new TlsFatalAlert(AlertDescription.unexpected_message);

                                                                        this.ProcessFinishedMessage(buf);
                                                                        buf.UpdateHash(this.m_handshakeHash);
                                                                        this.m_connectionState = CS_CLIENT_FINISHED;

                                                                        if (this.m_expectSessionTicket)
                                                                        {
                                                                            /*
                                                                             * TODO[new_session_ticket] Check the server-side rules regarding the session ID, since
                                                                             * the client is going to ignore any session ID it received once it sees the
                                                                             * new_session_ticket message.
                                                                             */

                                                                            this.SendNewSessionTicketMessage(this.m_tlsServer.GetNewSessionTicket());
                                                                            this.m_connectionState = CS_SERVER_SESSION_TICKET;
                                                                        }

                                                                        this.SendChangeCipherSpec();
                                                                        this.SendFinishedMessage();
                                                                        this.m_connectionState = CS_SERVER_FINISHED;

                                                                        this.CompleteHandshake();
                                                                        break;
                                                                    }
                                                                    default:
                                                                        throw new TlsFatalAlert(AlertDescription.unexpected_message);
                                                                }

                                                                break;
                                                            }

                                                            case HandshakeType.certificate_request:
                                                            case HandshakeType.certificate_status:
                                                            case HandshakeType.certificate_url:
                                                            case HandshakeType.encrypted_extensions:
                                                            case HandshakeType.end_of_early_data:
                                                            case HandshakeType.hello_request:
                                                            case HandshakeType.hello_verify_request:
                                                            case HandshakeType.key_update:
                                                            case HandshakeType.message_hash:
                                                            case HandshakeType.new_session_ticket:
                                                            case HandshakeType.server_hello:
                                                            case HandshakeType.server_hello_done:
                                                            case HandshakeType.server_key_exchange:
                                                            default:
                                                                throw new TlsFatalAlert(AlertDescription.unexpected_message);
                                                            }
                                                            }

                                                            protected override void HandleAlertWarningMessage(short alertDescription)
                                                            {
                                                                /*
                                                                 * SSL 3.0 If the server has sent a certificate request Message, the client must send
                                                                 * either the certificate message or a no_certificate alert.
                                                                 */
                                                                if (AlertDescription.no_certificate == alertDescription && null != this.m_certificateRequest
                                                                                                                        && TlsUtilities.IsSsl(this.m_tlsServerContext))
                                                                    switch (this.m_connectionState)
                                                                    {
                                                                        case CS_SERVER_HELLO_DONE:
                                                                        case CS_CLIENT_SUPPLEMENTAL_DATA:
                                                                        {
                                                                            if (this.m_connectionState != CS_CLIENT_SUPPLEMENTAL_DATA) this.m_tlsServer.ProcessClientSupplementalData(null);

                                                                            this.NotifyClientCertificate(Certificate.EmptyChain);
                                                                            this.m_connectionState = CS_CLIENT_CERTIFICATE;
                                                                            return;
                                                                        }
                                                                    }

                                                                base.HandleAlertWarningMessage(alertDescription);
                                                            }

                                                            /// <exception cref="IOException" />
                                                            protected virtual void NotifyClientCertificate(Certificate clientCertificate)
                                                            {
                                                                if (null == this.m_certificateRequest)
                                                                    throw new TlsFatalAlert(AlertDescription.internal_error);

                                                                TlsUtilities.ProcessClientCertificate(this.m_tlsServerContext, clientCertificate, this.m_keyExchange, this.m_tlsServer);
                                                            }

                                                            /// <exception cref="IOException" />
                                                            protected virtual void Receive13ClientCertificate(MemoryStream buf)
                                                            {
                                                                // TODO[tls13] This currently just duplicates 'receiveCertificateMessage'

                                                                if (null == this.m_certificateRequest)
                                                                    throw new TlsFatalAlert(AlertDescription.unexpected_message);

                                                                var options = new Certificate.ParseOptions()
                                                                    .SetMaxChainLength(this.m_tlsServer.GetMaxCertificateChainLength());

                                                                var clientCertificate = Certificate.Parse(options, this.m_tlsServerContext, buf, null);

                                                                AssertEmpty(buf);

                                                                this.NotifyClientCertificate(clientCertificate);
                                                            }

                                                            /// <exception cref="IOException" />
                                                            protected void Receive13ClientCertificateVerify(MemoryStream buf)
                                                            {
                                                                var clientCertificate = this.m_tlsServerContext.SecurityParameters.PeerCertificate;
                                                                if (null == clientCertificate || clientCertificate.IsEmpty)
                                                                    throw new TlsFatalAlert(AlertDescription.internal_error);

                                                                // TODO[tls13] Actual structure is 'CertificateVerify' in RFC 8446, consider adding for clarity
                                                                var certificateVerify = DigitallySigned.Parse(this.m_tlsServerContext, buf);

                                                                AssertEmpty(buf);

                                                                TlsUtilities.Verify13CertificateVerifyClient(this.m_tlsServerContext, this.m_certificateRequest, certificateVerify,
                                                                    this.m_handshakeHash);
                                                            }

                                                            /// <exception cref="IOException" />
                                                            protected virtual void Receive13ClientFinished(MemoryStream buf) { this.Process13FinishedMessage(buf); }

                                                            /// <exception cref="IOException" />
                                                            protected virtual void ReceiveCertificateMessage(MemoryStream buf)
                                                            {
                                                                if (null == this.m_certificateRequest)
                                                                    throw new TlsFatalAlert(AlertDescription.unexpected_message);

                                                                var options = new Certificate.ParseOptions()
                                                                    .SetMaxChainLength(this.m_tlsServer.GetMaxCertificateChainLength());

                                                                var clientCertificate = Certificate.Parse(options, this.m_tlsServerContext, buf, null);

                                                                AssertEmpty(buf);

                                                                this.NotifyClientCertificate(clientCertificate);
                                                            }

                                                            /// <exception cref="IOException" />
                                                            protected virtual void ReceiveCertificateVerifyMessage(MemoryStream buf)
                                                            {
                                                                var certificateVerify = DigitallySigned.Parse(this.m_tlsServerContext, buf);

                                                                AssertEmpty(buf);

                                                                TlsUtilities.VerifyCertificateVerifyClient(this.m_tlsServerContext, this.m_certificateRequest, certificateVerify,
                                                                    this.m_handshakeHash);

                                                                this.m_handshakeHash = this.m_handshakeHash.StopTracking();
                                                            }

                                                            /// <exception cref="IOException" />
                                                            protected virtual ClientHello ReceiveClientHelloMessage(MemoryStream buf) { return ClientHello.Parse(buf, null); }

                                                            /// <exception cref="IOException" />
                                                            protected virtual void ReceiveClientKeyExchangeMessage(MemoryStream buf)
                                                            {
                                                                this.m_keyExchange.ProcessClientKeyExchange(buf);

                                                                AssertEmpty(buf);

                                                                var isSsl = TlsUtilities.IsSsl(this.m_tlsServerContext);
                                                                if (isSsl)
                                                                    // NOTE: For SSLv3 (only), master_secret needed to calculate session hash
                                                                    EstablishMasterSecret(this.m_tlsServerContext, this.m_keyExchange);

                                                                this.m_tlsServerContext.SecurityParameters.m_sessionHash = TlsUtilities.GetCurrentPrfHash(this.m_handshakeHash);

                                                                if (!isSsl)
                                                                    // NOTE: For (D)TLS, session hash potentially needed for extended_master_secret
                                                                    EstablishMasterSecret(this.m_tlsServerContext, this.m_keyExchange);

                                                                this.m_recordStream.SetPendingCipher(TlsUtilities.InitCipher(this.m_tlsServerContext));

                                                                if (!this.ExpectCertificateVerifyMessage()) this.m_handshakeHash = this.m_handshakeHash.StopTracking();
                                                            }

                                                            /// <exception cref="IOException" />
                                                            protected virtual void Send13EncryptedExtensionsMessage(IDictionary serverExtensions)
                                                            {
                                                                // TODO[tls13] Avoid extra copy; use placeholder to write opaque-16 data directly to message buffer

                                                                var extBytes = WriteExtensionsData(serverExtensions);

                                                                var message = new HandshakeMessageOutput(HandshakeType.encrypted_extensions);
                                                                TlsUtilities.WriteOpaque16(extBytes, message);
                                                                message.Send(this);
                                                            }

                                                            /// <exception cref="IOException" />
                                                            protected virtual void Send13ServerHelloCoda(ServerHello serverHello, bool afterHelloRetryRequest)
                                                            {
                                                                var securityParameters = this.m_tlsServerContext.SecurityParameters;

                                                                var serverHelloTranscriptHash = TlsUtilities.GetCurrentPrfHash(this.m_handshakeHash);

                                                                TlsUtilities.Establish13PhaseHandshake(this.m_tlsServerContext, serverHelloTranscriptHash, this.m_recordStream);

                                                                this.m_recordStream.EnablePendingCipherWrite();
                                                                this.m_recordStream.EnablePendingCipherRead(true);

                                                                this.Send13EncryptedExtensionsMessage(this.m_serverExtensions);
                                                                this.m_connectionState = CS_SERVER_ENCRYPTED_EXTENSIONS;

                                                                if (this.m_selectedPsk13)
                                                                {
                                                                    /*
                                                                     * For PSK-only key exchange, there's no CertificateRequest, Certificate, CertificateVerify.
                                                                     */
                                                                }
                                                                else
                                                                {
                                                                    // CertificateRequest
                                                                    {
                                                                        this.m_certificateRequest = this.m_tlsServer.GetCertificateRequest();
                                                                        if (null != this.m_certificateRequest)
                                                                        {
                                                                            if (!this.m_certificateRequest.HasCertificateRequestContext(TlsUtilities.EmptyBytes))
                                                                                throw new TlsFatalAlert(AlertDescription.internal_error);

                                                                            TlsUtilities.EstablishServerSigAlgs(securityParameters, this.m_certificateRequest);

                                                                            this.SendCertificateRequestMessage(this.m_certificateRequest);
                                                                            this.m_connectionState = CS_SERVER_CERTIFICATE_REQUEST;
                                                                        }
                                                                    }

                                                                    var serverCredentials = TlsUtilities.Establish13ServerCredentials(this.m_tlsServer);
                                                                    if (null == serverCredentials)
                                                                        throw new TlsFatalAlert(AlertDescription.internal_error);

                                                                    // Certificate
                                                                    {
                                                                        /*
                                                                         * TODO[tls13] Note that we are expecting the TlsServer implementation to take care of
                                                                         * e.g. adding optional "status_request" extension to each CertificateEntry.
                                                                         */
                                                                        /*
                                                                         * No CertificateStatus message is sent; TLS 1.3 uses per-CertificateEntry
                                                                         * "status_request" extension instead.
                                                                         */

                                                                        var serverCertificate = serverCredentials.Certificate;
                                                                        this.Send13CertificateMessage(serverCertificate);
                                                                        securityParameters.m_tlsServerEndPoint = null;
                                                                        this.m_connectionState                 = CS_SERVER_CERTIFICATE;
                                                                    }

                                                                    // CertificateVerify
                                                                    {
                                                                        var certificateVerify = TlsUtilities.Generate13CertificateVerify(this.m_tlsServerContext,
                                                                            serverCredentials, this.m_handshakeHash);
                                                                        this.Send13CertificateVerifyMessage(certificateVerify);
                                                                        this.m_connectionState = CS_CLIENT_CERTIFICATE_VERIFY;
                                                                    }
                                                                }

                                                                // Finished
                                                                {
                                                                    this.Send13FinishedMessage();
                                                                    this.m_connectionState = CS_SERVER_FINISHED;
                                                                }

                                                                var serverFinishedTranscriptHash = TlsUtilities.GetCurrentPrfHash(this.m_handshakeHash);

                                                                TlsUtilities.Establish13PhaseApplication(this.m_tlsServerContext, serverFinishedTranscriptHash, this.m_recordStream);

                                                                this.m_recordStream.EnablePendingCipherWrite();
                                                            }

                                                            /// <exception cref="IOException" />
                                                            protected virtual void SendCertificateRequestMessage(CertificateRequest certificateRequest)
                                                            {
                                                                var message = new HandshakeMessageOutput(HandshakeType.certificate_request);
                                                                certificateRequest.Encode(this.m_tlsServerContext, message);
                                                                message.Send(this);
                                                            }

                                                            /// <exception cref="IOException" />
                                                            protected virtual void SendCertificateStatusMessage(CertificateStatus certificateStatus)
                                                            {
                                                                var message = new HandshakeMessageOutput(HandshakeType.certificate_status);
                                                                // TODO[tls13] Ensure this cannot happen for (D)TLS1.3+
                                                                certificateStatus.Encode(message);
                                                                message.Send(this);
                                                            }

                                                            /// <exception cref="IOException" />
                                                            protected virtual void SendHelloRequestMessage()
                                                            {
                                                                HandshakeMessageOutput.Send(this, HandshakeType.hello_request, TlsUtilities.EmptyBytes);
                                                            }

                                                            /// <exception cref="IOException" />
                                                            protected virtual void SendNewSessionTicketMessage(NewSessionTicket newSessionTicket)
                                                            {
                                                                if (newSessionTicket == null)
                                                                    throw new TlsFatalAlert(AlertDescription.internal_error);

                                                                var message = new HandshakeMessageOutput(HandshakeType.new_session_ticket);
                                                                newSessionTicket.Encode(message);
                                                                message.Send(this);
                                                            }

                                                            /// <exception cref="IOException" />
                                                            protected virtual void SendServerHelloDoneMessage()
                                                            {
                                                                HandshakeMessageOutput.Send(this, HandshakeType.server_hello_done, TlsUtilities.EmptyBytes);
                                                            }

                                                            /// <exception cref="IOException" />
                                                            protected virtual void SendServerHelloMessage(ServerHello serverHello)
                                                            {
                                                                var message = new HandshakeMessageOutput(HandshakeType.server_hello);
                                                                serverHello.Encode(this.m_tlsServerContext, message);
                                                                message.Send(this);
                                                            }

                                                            /// <exception cref="IOException" />
                                                            protected virtual void SendServerKeyExchangeMessage(byte[] serverKeyExchange)
                                                            {
                                                                HandshakeMessageOutput.Send(this, HandshakeType.server_key_exchange, serverKeyExchange);
                                                            }

                                                            /// <exception cref="IOException" />
                                                            protected virtual void Skip13ClientCertificate()
                                                            {
                                                                if (null != this.m_certificateRequest)
                                                                    throw new TlsFatalAlert(AlertDescription.unexpected_message);
                                                            }

                                                            /// <exception cref="IOException" />
                                                            protected virtual void Skip13ClientCertificateVerify()
                                                            {
                                                                if (this.ExpectCertificateVerifyMessage())
                                                                    throw new TlsFatalAlert(AlertDescription.unexpected_message);
                                                            }
                                                            }
                                                            }
#pragma warning restore
#endif