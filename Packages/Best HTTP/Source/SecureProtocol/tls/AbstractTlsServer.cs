#if !BESTHTTP_DISABLE_ALTERNATE_SSL && (!UNITY_WEBGL || UNITY_EDITOR)
#pragma warning disable
namespace BestHTTP.SecureProtocol.Org.BouncyCastle.Tls
{
    using System;
    using System.Collections;
    using System.IO;
    using BestHTTP.SecureProtocol.Org.BouncyCastle.Tls.Crypto;
    using BestHTTP.SecureProtocol.Org.BouncyCastle.Utilities;

    /// <summary>Base class for a TLS server.</summary>
    public abstract class AbstractTlsServer
        : AbstractTlsPeer, TlsServer
    {
        protected TlsServerContext  m_context;
        protected ProtocolVersion[] m_protocolVersions;
        protected int[]             m_cipherSuites;

        protected int[]       m_offeredCipherSuites;
        protected IDictionary m_clientExtensions;

        protected bool                     m_encryptThenMACOffered;
        protected short                    m_maxFragmentLengthOffered;
        protected bool                     m_truncatedHMacOffered;
        protected bool                     m_clientSentECPointFormats;
        protected CertificateStatusRequest m_certificateStatusRequest;
        protected IList                    m_statusRequestV2;
        protected IList                    m_trustedCAKeys;

        protected int          m_selectedCipherSuite;
        protected IList        m_clientProtocolNames;
        protected ProtocolName m_selectedProtocolName;

        protected readonly IDictionary m_serverExtensions = Platform.CreateHashtable();

        public AbstractTlsServer(TlsCrypto crypto)
            : base(crypto)
        {
        }

        protected virtual bool AllowCertificateStatus() { return true; }

        protected virtual bool AllowEncryptThenMac() { return true; }

        protected virtual bool AllowMultiCertStatus() { return false; }

        protected virtual bool AllowTruncatedHmac() { return false; }

        protected virtual bool AllowTrustedCAIndication() { return false; }

        protected virtual int GetMaximumNegotiableCurveBits()
        {
            var clientSupportedGroups = this.m_context.SecurityParameters.ClientSupportedGroups;
            if (clientSupportedGroups == null)
                /*
                     * RFC 4492 4. A client that proposes ECC cipher suites may choose not to include these
                     * extensions. In this case, the server is free to choose any one of the elliptic curves
                     * or point formats [...].
                     */
                return NamedGroup.GetMaximumCurveBits();

            var maxBits                                                    = 0;
            for (var i = 0; i < clientSupportedGroups.Length; ++i) maxBits = Math.Max(maxBits, NamedGroup.GetCurveBits(clientSupportedGroups[i]));
            return maxBits;
        }

        protected virtual int GetMaximumNegotiableFiniteFieldBits()
        {
            var clientSupportedGroups = this.m_context.SecurityParameters.ClientSupportedGroups;
            if (clientSupportedGroups == null) return NamedGroup.GetMaximumFiniteFieldBits();

            var maxBits                                                    = 0;
            for (var i = 0; i < clientSupportedGroups.Length; ++i) maxBits = Math.Max(maxBits, NamedGroup.GetFiniteFieldBits(clientSupportedGroups[i]));
            return maxBits;
        }

        protected virtual IList GetProtocolNames() { return null; }

        protected virtual bool IsSelectableCipherSuite(int cipherSuite, int availCurveBits, int availFiniteFieldBits,
            IList sigAlgs)
        {
            // TODO[tls13] The version check should be separated out (eventually select ciphersuite before version)
            return TlsUtilities.IsValidVersionForCipherSuite(cipherSuite, this.m_context.ServerVersion)
                   && availCurveBits >= TlsEccUtilities.GetMinimumCurveBits(cipherSuite)
                   && availFiniteFieldBits >= TlsDHUtilities.GetMinimumFiniteFieldBits(cipherSuite)
                   && TlsUtilities.IsValidCipherSuiteForSignatureAlgorithms(cipherSuite, sigAlgs);
        }

        protected virtual bool PreferLocalCipherSuites() { return false; }

        /// <exception cref="IOException" />
        protected virtual bool SelectCipherSuite(int cipherSuite)
        {
            this.m_selectedCipherSuite = cipherSuite;
            return true;
        }

        protected virtual int SelectDH(int minimumFiniteFieldBits)
        {
            var clientSupportedGroups = this.m_context.SecurityParameters.ClientSupportedGroups;
            if (clientSupportedGroups == null)
                return this.SelectDHDefault(minimumFiniteFieldBits);

            // Try to find a supported named group of the required size from the client's list.
            for (var i = 0; i < clientSupportedGroups.Length; ++i)
            {
                var namedGroup = clientSupportedGroups[i];
                if (NamedGroup.GetFiniteFieldBits(namedGroup) >= minimumFiniteFieldBits)
                    return namedGroup;
            }

            return -1;
        }

        protected virtual int SelectDHDefault(int minimumFiniteFieldBits)
        {
            return minimumFiniteFieldBits <= 2048 ? NamedGroup.ffdhe2048
                : minimumFiniteFieldBits <= 3072  ? NamedGroup.ffdhe3072
                : minimumFiniteFieldBits <= 4096  ? NamedGroup.ffdhe4096
                : minimumFiniteFieldBits <= 6144  ? NamedGroup.ffdhe6144
                : minimumFiniteFieldBits <= 8192  ? NamedGroup.ffdhe8192
                                                    : -1;
        }

        protected virtual int SelectECDH(int minimumCurveBits)
        {
            var clientSupportedGroups = this.m_context.SecurityParameters.ClientSupportedGroups;
            if (clientSupportedGroups == null)
                return this.SelectECDHDefault(minimumCurveBits);

            // Try to find a supported named group of the required size from the client's list.
            for (var i = 0; i < clientSupportedGroups.Length; ++i)
            {
                var namedGroup = clientSupportedGroups[i];
                if (NamedGroup.GetCurveBits(namedGroup) >= minimumCurveBits)
                    return namedGroup;
            }

            return -1;
        }

        protected virtual int SelectECDHDefault(int minimumCurveBits)
        {
            return minimumCurveBits <= 256 ? NamedGroup.secp256r1
                : minimumCurveBits <= 384  ? NamedGroup.secp384r1
                : minimumCurveBits <= 521  ? NamedGroup.secp521r1
                                             : -1;
        }

        protected virtual ProtocolName SelectProtocolName()
        {
            var serverProtocolNames = this.GetProtocolNames();
            if (null == serverProtocolNames || serverProtocolNames.Count < 1)
                return null;

            var result = this.SelectProtocolName(this.m_clientProtocolNames, serverProtocolNames);
            if (null == result)
                throw new TlsFatalAlert(AlertDescription.no_application_protocol);

            return result;
        }

        protected virtual ProtocolName SelectProtocolName(IList clientProtocolNames, IList serverProtocolNames)
        {
            foreach (ProtocolName serverProtocolName in serverProtocolNames)
                if (clientProtocolNames.Contains(serverProtocolName))
                    return serverProtocolName;
            return null;
        }

        protected virtual bool ShouldSelectProtocolNameEarly() { return true; }

        public virtual void Init(TlsServerContext context)
        {
            this.m_context = context;

            this.m_protocolVersions = this.GetSupportedVersions();
            this.m_cipherSuites     = this.GetSupportedCipherSuites();
        }

        public override ProtocolVersion[] GetProtocolVersions() { return this.m_protocolVersions; }

        public override int[] GetCipherSuites() { return this.m_cipherSuites; }

        public override void NotifyHandshakeBeginning()
        {
            base.NotifyHandshakeBeginning();

            this.m_offeredCipherSuites      = null;
            this.m_clientExtensions         = null;
            this.m_encryptThenMACOffered    = false;
            this.m_maxFragmentLengthOffered = 0;
            this.m_truncatedHMacOffered     = false;
            this.m_clientSentECPointFormats = false;
            this.m_certificateStatusRequest = null;
            this.m_selectedCipherSuite      = -1;
            this.m_selectedProtocolName     = null;
            this.m_serverExtensions.Clear();
        }

        public virtual TlsSession GetSessionToResume(byte[] sessionID) { return null; }

        public virtual byte[] GetNewSessionID() { return null; }

        public virtual TlsPskExternal GetExternalPsk(IList identities) { return null; }

        public virtual void NotifySession(TlsSession session) { }

        public virtual void NotifyClientVersion(ProtocolVersion clientVersion) { }

        public virtual void NotifyFallback(bool isFallback)
        {
            /*
             * RFC 7507 3. If TLS_FALLBACK_SCSV appears in ClientHello.cipher_suites and the highest
             * protocol version supported by the server is higher than the version indicated in
             * ClientHello.client_version, the server MUST respond with a fatal inappropriate_fallback
             * alert [..].
             */
            if (isFallback)
            {
                var serverVersions = this.GetProtocolVersions();
                var clientVersion  = this.m_context.ClientVersion;

                ProtocolVersion latestServerVersion;
                if (clientVersion.IsTls)
                    latestServerVersion = ProtocolVersion.GetLatestTls(serverVersions);
                else if (clientVersion.IsDtls)
                    latestServerVersion = ProtocolVersion.GetLatestDtls(serverVersions);
                else
                    throw new TlsFatalAlert(AlertDescription.internal_error);

                if (null != latestServerVersion && latestServerVersion.IsLaterVersionOf(clientVersion)) throw new TlsFatalAlert(AlertDescription.inappropriate_fallback);
            }
        }

        public virtual void NotifyOfferedCipherSuites(int[] offeredCipherSuites) { this.m_offeredCipherSuites = offeredCipherSuites; }

        public virtual void ProcessClientExtensions(IDictionary clientExtensions)
        {
            this.m_clientExtensions = clientExtensions;

            if (null != clientExtensions)
            {
                this.m_clientProtocolNames = TlsExtensionsUtilities.GetAlpnExtensionClient(clientExtensions);

                if (this.ShouldSelectProtocolNameEarly())
                    if (null != this.m_clientProtocolNames && this.m_clientProtocolNames.Count > 0)
                        this.m_selectedProtocolName = this.SelectProtocolName();

                // TODO[tls13] Don't need these if we have negotiated (D)TLS 1.3+
                {
                    this.m_encryptThenMACOffered = TlsExtensionsUtilities.HasEncryptThenMacExtension(clientExtensions);
                    this.m_truncatedHMacOffered  = TlsExtensionsUtilities.HasTruncatedHmacExtension(clientExtensions);
                    this.m_statusRequestV2       = TlsExtensionsUtilities.GetStatusRequestV2Extension(clientExtensions);
                    this.m_trustedCAKeys         = TlsExtensionsUtilities.GetTrustedCAKeysExtensionClient(clientExtensions);

                    // We only support uncompressed format, this is just to validate the extension, and note its presence.
                    this.m_clientSentECPointFormats =
                        null != TlsExtensionsUtilities.GetSupportedPointFormatsExtension(clientExtensions);
                }

                this.m_certificateStatusRequest = TlsExtensionsUtilities.GetStatusRequestExtension(clientExtensions);

                this.m_maxFragmentLengthOffered = TlsExtensionsUtilities.GetMaxFragmentLengthExtension(clientExtensions);
                if (this.m_maxFragmentLengthOffered >= 0 && !MaxFragmentLength.IsValid(this.m_maxFragmentLengthOffered))
                    throw new TlsFatalAlert(AlertDescription.illegal_parameter);
            }
        }

        public virtual ProtocolVersion GetServerVersion()
        {
            var serverVersions = this.GetProtocolVersions();
            var clientVersions = this.m_context.ClientSupportedVersions;

            foreach (var clientVersion in clientVersions)
                if (ProtocolVersion.Contains(serverVersions, clientVersion))
                    return clientVersion;

            throw new TlsFatalAlert(AlertDescription.protocol_version);
        }

        public virtual int[] GetSupportedGroups()
        {
            // TODO[tls13] The rest of this class assumes all named groups are supported
            return new[]
            {
                NamedGroup.x25519, NamedGroup.x448, NamedGroup.secp256r1, NamedGroup.secp384r1,
                NamedGroup.ffdhe2048, NamedGroup.ffdhe3072, NamedGroup.ffdhe4096
            };
        }

        public virtual int GetSelectedCipherSuite()
        {
            var securityParameters = this.m_context.SecurityParameters;
            var negotiatedVersion  = securityParameters.NegotiatedVersion;

            if (TlsUtilities.IsTlsV13(negotiatedVersion))
            {
                var commonCipherSuite13 = TlsUtilities.GetCommonCipherSuite13(negotiatedVersion, this.m_offeredCipherSuites, this.GetCipherSuites(), this.PreferLocalCipherSuites());

                if (commonCipherSuite13 >= 0 && this.SelectCipherSuite(commonCipherSuite13)) return commonCipherSuite13;
            }
            else
            {
                /*
                 * RFC 5246 7.4.3. In order to negotiate correctly, the server MUST check any candidate
                 * cipher suites against the "signature_algorithms" extension before selecting them. This is
                 * somewhat inelegant but is a compromise designed to minimize changes to the original
                 * cipher suite design.
                 */
                var sigAlgs = TlsUtilities.GetUsableSignatureAlgorithms(securityParameters.ClientSigAlgs);

                /*
                 * RFC 4429 5.1. A server that receives a ClientHello containing one or both of these
                 * extensions MUST use the client's enumerated capabilities to guide its selection of an
                 * appropriate cipher suite. One of the proposed ECC cipher suites must be negotiated only
                 * if the server can successfully complete the handshake while using the curves and point
                 * formats supported by the client [...].
                 */
                var availCurveBits       = this.GetMaximumNegotiableCurveBits();
                var availFiniteFieldBits = this.GetMaximumNegotiableFiniteFieldBits();

                var cipherSuites = TlsUtilities.GetCommonCipherSuites(this.m_offeredCipherSuites, this.GetCipherSuites(), this.PreferLocalCipherSuites());

                for (var i = 0; i < cipherSuites.Length; ++i)
                {
                    var cipherSuite = cipherSuites[i];
                    if (this.IsSelectableCipherSuite(cipherSuite, availCurveBits, availFiniteFieldBits, sigAlgs)
                        && this.SelectCipherSuite(cipherSuite))
                        return cipherSuite;
                }
            }

            throw new TlsFatalAlert(AlertDescription.handshake_failure, "No selectable cipher suite");
        }

        // IDictionary is (Int32 -> byte[])
        public virtual IDictionary GetServerExtensions()
        {
            var isTlsV13 = TlsUtilities.IsTlsV13(this.m_context);

            if (isTlsV13)
            {
                if (null != this.m_certificateStatusRequest && this.AllowCertificateStatus())
                {
                    /*
                     * TODO[tls13] RFC 8446 4.4.2.1. OCSP Status and SCT Extensions.
                     * 
                     * OCSP information is carried in an extension for a CertificateEntry.
                     */
                }
            }
            else
            {
                if (this.m_encryptThenMACOffered && this.AllowEncryptThenMac())
                    /*
                         * RFC 7366 3. If a server receives an encrypt-then-MAC request extension from a client
                         * and then selects a stream or Authenticated Encryption with Associated Data (AEAD)
                         * ciphersuite, it MUST NOT send an encrypt-then-MAC response extension back to the
                         * client.
                         */
                        if (TlsUtilities.IsBlockCipherSuite(this.m_selectedCipherSuite))
                            TlsExtensionsUtilities.AddEncryptThenMacExtension(this.m_serverExtensions);

                    if (this.m_truncatedHMacOffered && this.AllowTruncatedHmac()) TlsExtensionsUtilities.AddTruncatedHmacExtension(this.m_serverExtensions);

                    if (this.m_clientSentECPointFormats && TlsEccUtilities.IsEccCipherSuite(this.m_selectedCipherSuite))
                        /*
                             * RFC 4492 5.2. A server that selects an ECC cipher suite in response to a ClientHello
                             * message including a Supported Point Formats Extension appends this extension (along
                             * with others) to its ServerHello message, enumerating the point formats it can parse.
                             */
                            TlsExtensionsUtilities.AddSupportedPointFormatsExtension(this.m_serverExtensions,
                                new[] { ECPointFormat.uncompressed });

                        // TODO[tls13] See RFC 8446 4.4.2.1
                        if (null != this.m_statusRequestV2 && this.AllowMultiCertStatus())
                            /*
                                 * RFC 6961 2.2. If a server returns a "CertificateStatus" message in response to a
                                 * "status_request_v2" request, then the server MUST have included an extension of type
                                 * "status_request_v2" with empty "extension_data" in the extended server hello..
                                 */
                                TlsExtensionsUtilities.AddEmptyExtensionData(this.m_serverExtensions, ExtensionType.status_request_v2);
                            else if (null != this.m_certificateStatusRequest && this.AllowCertificateStatus())
                                /*
                                     * RFC 6066 8. If a server returns a "CertificateStatus" message, then the server MUST
                                     * have included an extension of type "status_request" with empty "extension_data" in
                                     * the extended server hello.
                                     */
                                    TlsExtensionsUtilities.AddEmptyExtensionData(this.m_serverExtensions, ExtensionType.status_request);

                                if (null != this.m_trustedCAKeys && this.AllowTrustedCAIndication()) TlsExtensionsUtilities.AddTrustedCAKeysExtensionServer(this.m_serverExtensions);
                                }

                                if (this.m_maxFragmentLengthOffered >= 0 && MaxFragmentLength.IsValid(this.m_maxFragmentLengthOffered))
                                    TlsExtensionsUtilities.AddMaxFragmentLengthExtension(this.m_serverExtensions, this.m_maxFragmentLengthOffered);

                                return this.m_serverExtensions;
                                }

                                public virtual void GetServerExtensionsForConnection(IDictionary serverExtensions)
                                {
                                    if (!this.ShouldSelectProtocolNameEarly())
                                        if (null != this.m_clientProtocolNames && this.m_clientProtocolNames.Count > 0)
                                            this.m_selectedProtocolName = this.SelectProtocolName();

                                    /*
                                     * RFC 7301 3.1. When session resumption or session tickets [...] are used, the previous
                                     * contents of this extension are irrelevant, and only the values in the new handshake
                                     * messages are considered.
                                     */
                                    if (null == this.m_selectedProtocolName)
                                        serverExtensions.Remove(ExtensionType.application_layer_protocol_negotiation);
                                    else
                                        TlsExtensionsUtilities.AddAlpnExtensionServer(serverExtensions, this.m_selectedProtocolName);
                                }

                                public virtual IList GetServerSupplementalData() { return null; }

                                public abstract TlsCredentials GetCredentials();

                                public virtual CertificateStatus GetCertificateStatus() { return null; }

                                public virtual CertificateRequest GetCertificateRequest() { return null; }

                                public virtual TlsPskIdentityManager GetPskIdentityManager() { return null; }

                                public virtual TlsSrpLoginParameters GetSrpLoginParameters() { return null; }

                                public virtual TlsDHConfig GetDHConfig()
                                {
                                    var minimumFiniteFieldBits = TlsDHUtilities.GetMinimumFiniteFieldBits(this.m_selectedCipherSuite);
                                    var namedGroup             = this.SelectDH(minimumFiniteFieldBits);
                                    return TlsDHUtilities.CreateNamedDHConfig(this.m_context, namedGroup);
                                }

                                public virtual TlsECConfig GetECDHConfig()
                                {
                                    var minimumCurveBits = TlsEccUtilities.GetMinimumCurveBits(this.m_selectedCipherSuite);
                                    var namedGroup       = this.SelectECDH(minimumCurveBits);
                                    return TlsEccUtilities.CreateNamedECConfig(this.m_context, namedGroup);
                                }

                                public virtual void ProcessClientSupplementalData(IList clientSupplementalData)
                                {
                                    if (clientSupplementalData != null)
                                        throw new TlsFatalAlert(AlertDescription.unexpected_message);
                                }

                                public virtual void NotifyClientCertificate(Certificate clientCertificate) { throw new TlsFatalAlert(AlertDescription.internal_error); }

                                public virtual NewSessionTicket GetNewSessionTicket()
                                {
                                    /*
                                     * RFC 5077 3.3. If the server determines that it does not want to include a ticket after it
                                     * has included the SessionTicket extension in the ServerHello, then it sends a zero-length
                                     * ticket in the NewSessionTicket handshake message.
                                     */
                                    return new NewSessionTicket(0L, TlsUtilities.EmptyBytes);
                                }
                                }
                                }
#pragma warning restore
#endif