#if !BESTHTTP_DISABLE_ALTERNATE_SSL && (!UNITY_WEBGL || UNITY_EDITOR)
#pragma warning disable
namespace BestHTTP.SecureProtocol.Org.BouncyCastle.Tls
{
    using System;
    using BestHTTP.SecureProtocol.Org.BouncyCastle.Utilities;

    public sealed class ProtocolVersion
    {
        public static readonly ProtocolVersion SSLv3   = new(0x0300, "SSL 3.0");
        public static readonly ProtocolVersion TLSv10  = new(0x0301, "TLS 1.0");
        public static readonly ProtocolVersion TLSv11  = new(0x0302, "TLS 1.1");
        public static readonly ProtocolVersion TLSv12  = new(0x0303, "TLS 1.2");
        public static readonly ProtocolVersion TLSv13  = new(0x0304, "TLS 1.3");
        public static readonly ProtocolVersion DTLSv10 = new(0xFEFF, "DTLS 1.0");
        public static readonly ProtocolVersion DTLSv12 = new(0xFEFD, "DTLS 1.2");

        internal static readonly ProtocolVersion CLIENT_EARLIEST_SUPPORTED_DTLS = DTLSv10;
        internal static readonly ProtocolVersion CLIENT_EARLIEST_SUPPORTED_TLS  = SSLv3;
        internal static readonly ProtocolVersion CLIENT_LATEST_SUPPORTED_DTLS   = DTLSv12;
        internal static readonly ProtocolVersion CLIENT_LATEST_SUPPORTED_TLS    = TLSv13;

        internal static readonly ProtocolVersion SERVER_EARLIEST_SUPPORTED_DTLS = DTLSv10;
        internal static readonly ProtocolVersion SERVER_EARLIEST_SUPPORTED_TLS  = SSLv3;
        internal static readonly ProtocolVersion SERVER_LATEST_SUPPORTED_DTLS   = DTLSv12;
        internal static readonly ProtocolVersion SERVER_LATEST_SUPPORTED_TLS    = TLSv13;

        public static bool Contains(ProtocolVersion[] versions, ProtocolVersion version)
        {
            if (versions != null && version != null)
                for (var i = 0; i < versions.Length; ++i)
                    if (version.Equals(versions[i]))
                        return true;
            return false;
        }

        public static ProtocolVersion GetEarliestDtls(ProtocolVersion[] versions)
        {
            ProtocolVersion earliest = null;
            if (null != versions)
                for (var i = 0; i < versions.Length; ++i)
                {
                    var next = versions[i];
                    if (null != next && next.IsDtls)
                        if (null == earliest || next.MinorVersion > earliest.MinorVersion)
                            earliest = next;
                }

            return earliest;
        }

        public static ProtocolVersion GetEarliestTls(ProtocolVersion[] versions)
        {
            ProtocolVersion earliest = null;
            if (null != versions)
                for (var i = 0; i < versions.Length; ++i)
                {
                    var next = versions[i];
                    if (null != next && next.IsTls)
                        if (null == earliest || next.MinorVersion < earliest.MinorVersion)
                            earliest = next;
                }

            return earliest;
        }

        public static ProtocolVersion GetLatestDtls(ProtocolVersion[] versions)
        {
            ProtocolVersion latest = null;
            if (null != versions)
                for (var i = 0; i < versions.Length; ++i)
                {
                    var next = versions[i];
                    if (null != next && next.IsDtls)
                        if (null == latest || next.MinorVersion < latest.MinorVersion)
                            latest = next;
                }

            return latest;
        }

        public static ProtocolVersion GetLatestTls(ProtocolVersion[] versions)
        {
            ProtocolVersion latest = null;
            if (null != versions)
                for (var i = 0; i < versions.Length; ++i)
                {
                    var next = versions[i];
                    if (null != next && next.IsTls)
                        if (null == latest || next.MinorVersion > latest.MinorVersion)
                            latest = next;
                }

            return latest;
        }

        internal static bool IsSupportedDtlsVersionClient(ProtocolVersion version)
        {
            return null != version
                   && version.IsEqualOrLaterVersionOf(CLIENT_EARLIEST_SUPPORTED_DTLS)
                   && version.IsEqualOrEarlierVersionOf(CLIENT_LATEST_SUPPORTED_DTLS);
        }

        internal static bool IsSupportedDtlsVersionServer(ProtocolVersion version)
        {
            return null != version
                   && version.IsEqualOrLaterVersionOf(SERVER_EARLIEST_SUPPORTED_DTLS)
                   && version.IsEqualOrEarlierVersionOf(SERVER_LATEST_SUPPORTED_DTLS);
        }

        internal static bool IsSupportedTlsVersionClient(ProtocolVersion version)
        {
            if (null == version)
                return false;

            var fullVersion = version.FullVersion;

            return fullVersion >= CLIENT_EARLIEST_SUPPORTED_TLS.FullVersion
                   && fullVersion <= CLIENT_LATEST_SUPPORTED_TLS.FullVersion;
        }

        internal static bool IsSupportedTlsVersionServer(ProtocolVersion version)
        {
            if (null == version)
                return false;

            var fullVersion = version.FullVersion;

            return fullVersion >= SERVER_EARLIEST_SUPPORTED_TLS.FullVersion
                   && fullVersion <= SERVER_LATEST_SUPPORTED_TLS.FullVersion;
        }

        private ProtocolVersion(int v, string name)
        {
            this.FullVersion = v & 0xFFFF;
            this.Name        = name;
        }

        public ProtocolVersion[] DownTo(ProtocolVersion min)
        {
            if (!this.IsEqualOrLaterVersionOf(min))
                throw new ArgumentException("must be an equal or earlier version of this one", "min");

            var result = Platform.CreateArrayList();
            result.Add(this);

            var current = this;
            while (!current.Equals(min))
            {
                current = current.GetPreviousVersion();
                result.Add(current);
            }

            var versions                                       = new ProtocolVersion[result.Count];
            for (var i = 0; i < result.Count; ++i) versions[i] = (ProtocolVersion)result[i];
            return versions;
        }

        public int FullVersion { get; }

        public int MajorVersion => this.FullVersion >> 8;

        public int MinorVersion => this.FullVersion & 0xFF;

        public string Name { get; }

        public bool IsDtls => this.MajorVersion == 0xFE;

        public bool IsSsl => this == SSLv3;

        public bool IsTls => this.MajorVersion == 0x03;

        public ProtocolVersion GetEquivalentTlsVersion()
        {
            switch (this.MajorVersion)
            {
                case 0x03:
                    return this;
                case 0xFE:
                    switch (this.MinorVersion)
                    {
                        case 0xFF: return TLSv11;
                        case 0xFD: return TLSv12;
                        default:   return null;
                    }
                default:
                    return null;
            }
        }

        public ProtocolVersion GetNextVersion()
        {
            int major = this.MajorVersion, minor = this.MinorVersion;
            switch (major)
            {
                case 0x03:
                    switch (minor)
                    {
                        case 0xFF: return null;
                        default:   return Get(major, minor + 1);
                    }
                case 0xFE:
                    switch (minor)
                    {
                        case 0x00: return null;
                        case 0xFF: return DTLSv12;
                        default:   return Get(major, minor - 1);
                    }
                default:
                    return null;
            }
        }

        public ProtocolVersion GetPreviousVersion()
        {
            int major = this.MajorVersion, minor = this.MinorVersion;
            switch (major)
            {
                case 0x03:
                    switch (minor)
                    {
                        case 0x00: return null;
                        default:   return Get(major, minor - 1);
                    }
                case 0xFE:
                    switch (minor)
                    {
                        case 0xFF: return null;
                        case 0xFD: return DTLSv10;
                        default:   return Get(major, minor + 1);
                    }
                default:
                    return null;
            }
        }

        public bool IsEarlierVersionOf(ProtocolVersion version)
        {
            if (null == version || this.MajorVersion != version.MajorVersion)
                return false;

            var diffMinorVersion = this.MinorVersion - version.MinorVersion;
            return this.IsDtls ? diffMinorVersion > 0 : diffMinorVersion < 0;
        }

        public bool IsEqualOrEarlierVersionOf(ProtocolVersion version)
        {
            if (null == version || this.MajorVersion != version.MajorVersion)
                return false;

            var diffMinorVersion = this.MinorVersion - version.MinorVersion;
            return this.IsDtls ? diffMinorVersion >= 0 : diffMinorVersion <= 0;
        }

        public bool IsEqualOrLaterVersionOf(ProtocolVersion version)
        {
            if (null == version || this.MajorVersion != version.MajorVersion)
                return false;

            var diffMinorVersion = this.MinorVersion - version.MinorVersion;
            return this.IsDtls ? diffMinorVersion <= 0 : diffMinorVersion >= 0;
        }

        public bool IsLaterVersionOf(ProtocolVersion version)
        {
            if (null == version || this.MajorVersion != version.MajorVersion)
                return false;

            var diffMinorVersion = this.MinorVersion - version.MinorVersion;
            return this.IsDtls ? diffMinorVersion < 0 : diffMinorVersion > 0;
        }

        public override bool Equals(object other) { return this == other || other is ProtocolVersion && this.Equals((ProtocolVersion)other); }

        public bool Equals(ProtocolVersion other) { return other != null && this.FullVersion == other.FullVersion; }

        public override int GetHashCode() { return this.FullVersion; }

        public static ProtocolVersion Get(int major, int minor)
        {
            switch (major)
            {
                case 0x03:
                {
                    switch (minor)
                    {
                        case 0x00:
                            return SSLv3;
                        case 0x01:
                            return TLSv10;
                        case 0x02:
                            return TLSv11;
                        case 0x03:
                            return TLSv12;
                        case 0x04:
                            return TLSv13;
                    }

                    return GetUnknownVersion(major, minor, "TLS");
                }
                case 0xFE:
                {
                    switch (minor)
                    {
                        case 0xFF:
                            return DTLSv10;
                        case 0xFE:
                            throw new ArgumentException("{0xFE, 0xFE} is a reserved protocol version");
                        case 0xFD:
                            return DTLSv12;
                    }

                    return GetUnknownVersion(major, minor, "DTLS");
                }
                default:
                {
                    return GetUnknownVersion(major, minor, "UNKNOWN");
                }
            }
        }

        public ProtocolVersion[] Only() { return new[] { this }; }

        public override string ToString() { return this.Name; }

        private static void CheckUint8(int versionOctet)
        {
            if (!TlsUtilities.IsValidUint8(versionOctet))
                throw new ArgumentException("not a valid octet", "versionOctet");
        }

        private static ProtocolVersion GetUnknownVersion(int major, int minor, string prefix)
        {
            CheckUint8(major);
            CheckUint8(minor);

            var v   = (major << 8) | minor;
            var hex = Platform.ToUpperInvariant(Convert.ToString(0x10000 | v, 16).Substring(1));
            return new ProtocolVersion(v, prefix + " 0x" + hex);
        }
    }
}
#pragma warning restore
#endif