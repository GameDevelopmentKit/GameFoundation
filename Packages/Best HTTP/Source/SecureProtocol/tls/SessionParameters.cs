#if !BESTHTTP_DISABLE_ALTERNATE_SSL && (!UNITY_WEBGL || UNITY_EDITOR)
#pragma warning disable
namespace BestHTTP.SecureProtocol.Org.BouncyCastle.Tls
{
    using System;
    using System.Collections;
    using System.IO;
    using BestHTTP.SecureProtocol.Org.BouncyCastle.Tls.Crypto;
    using BestHTTP.SecureProtocol.Org.BouncyCastle.Utilities;

    public sealed class SessionParameters
    {
        public sealed class Builder
        {
            private int             m_cipherSuite = -1;
            private Certificate     m_localCertificate;
            private TlsSecret       m_masterSecret;
            private ProtocolVersion m_negotiatedVersion;
            private Certificate     m_peerCertificate;
            private byte[]          m_pskIdentity;
            private byte[]          m_srpIdentity;
            private byte[]          m_encodedServerExtensions;
            private bool            m_extendedMasterSecret;

            public SessionParameters Build()
            {
                this.Validate(this.m_cipherSuite >= 0, "cipherSuite");
                this.Validate(this.m_masterSecret != null, "masterSecret");
                return new SessionParameters(this.m_cipherSuite, this.m_localCertificate, this.m_masterSecret, this.m_negotiatedVersion, this.m_peerCertificate, this.m_pskIdentity, this.m_srpIdentity,
                    this.m_encodedServerExtensions, this.m_extendedMasterSecret);
            }

            public Builder SetCipherSuite(int cipherSuite)
            {
                this.m_cipherSuite = cipherSuite;
                return this;
            }

            public Builder SetExtendedMasterSecret(bool extendedMasterSecret)
            {
                this.m_extendedMasterSecret = extendedMasterSecret;
                return this;
            }

            public Builder SetLocalCertificate(Certificate localCertificate)
            {
                this.m_localCertificate = localCertificate;
                return this;
            }

            public Builder SetMasterSecret(TlsSecret masterSecret)
            {
                this.m_masterSecret = masterSecret;
                return this;
            }

            public Builder SetNegotiatedVersion(ProtocolVersion negotiatedVersion)
            {
                this.m_negotiatedVersion = negotiatedVersion;
                return this;
            }

            public Builder SetPeerCertificate(Certificate peerCertificate)
            {
                this.m_peerCertificate = peerCertificate;
                return this;
            }

            public Builder SetPskIdentity(byte[] pskIdentity)
            {
                this.m_pskIdentity = pskIdentity;
                return this;
            }

            public Builder SetSrpIdentity(byte[] srpIdentity)
            {
                this.m_srpIdentity = srpIdentity;
                return this;
            }

            /// <exception cref="IOException" />
            public Builder SetServerExtensions(IDictionary serverExtensions)
            {
                if (serverExtensions == null || serverExtensions.Count < 1)
                {
                    this.m_encodedServerExtensions = null;
                }
                else
                {
                    var buf = new MemoryStream();
                    TlsProtocol.WriteExtensions(buf, serverExtensions);
                    this.m_encodedServerExtensions = buf.ToArray();
                }

                return this;
            }

            private void Validate(bool condition, string parameter)
            {
                if (!condition)
                    throw new InvalidOperationException("Required session parameter '" + parameter + "' not configured");
            }
        }

        private readonly byte[] m_encodedServerExtensions;

        private SessionParameters(int cipherSuite, Certificate localCertificate, TlsSecret masterSecret,
            ProtocolVersion negotiatedVersion, Certificate peerCertificate, byte[] pskIdentity, byte[] srpIdentity,
            byte[] encodedServerExtensions, bool extendedMasterSecret)
        {
            this.CipherSuite               = cipherSuite;
            this.LocalCertificate          = localCertificate;
            this.MasterSecret              = masterSecret;
            this.NegotiatedVersion         = negotiatedVersion;
            this.PeerCertificate           = peerCertificate;
            this.PskIdentity               = Arrays.Clone(pskIdentity);
            this.SrpIdentity               = Arrays.Clone(srpIdentity);
            this.m_encodedServerExtensions = encodedServerExtensions;
            this.IsExtendedMasterSecret    = extendedMasterSecret;
        }

        public int CipherSuite { get; }

        public void Clear()
        {
            if (this.MasterSecret != null) this.MasterSecret.Destroy();
        }

        public SessionParameters Copy()
        {
            return new SessionParameters(this.CipherSuite, this.LocalCertificate, this.MasterSecret, this.NegotiatedVersion, this.PeerCertificate, this.PskIdentity, this.SrpIdentity,
                this.m_encodedServerExtensions, this.IsExtendedMasterSecret);
        }

        public bool IsExtendedMasterSecret { get; }

        public Certificate LocalCertificate { get; }

        public TlsSecret MasterSecret { get; }

        public ProtocolVersion NegotiatedVersion { get; }

        public Certificate PeerCertificate { get; }

        public byte[] PskIdentity { get; }

        /// <exception cref="IOException" />
        public IDictionary ReadServerExtensions()
        {
            if (this.m_encodedServerExtensions == null)
                return null;

            return TlsProtocol.ReadExtensions(new MemoryStream(this.m_encodedServerExtensions, false));
        }

        public byte[] SrpIdentity { get; }
    }
}
#pragma warning restore
#endif