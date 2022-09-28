#if !BESTHTTP_DISABLE_ALTERNATE_SSL && (!UNITY_WEBGL || UNITY_EDITOR)
#pragma warning disable
namespace BestHTTP.SecureProtocol.Org.BouncyCastle.Tls
{
    using System;

    /// <summary>RFC 5764 4.1.1</summary>
    public sealed class UseSrtpData
    {
        /// <param name="protectionProfiles">see <see cref="SrtpProtectionProfile" /> for valid constants.</param>
        /// <param name="mki">valid lengths from 0 to 255.</param>
        public UseSrtpData(int[] protectionProfiles, byte[] mki)
        {
            if (TlsUtilities.IsNullOrEmpty(protectionProfiles) || protectionProfiles.Length >= 1 << 15)
                throw new ArgumentException("must have length from 1 to (2^15 - 1)", "protectionProfiles");

            if (mki == null)
                mki = TlsUtilities.EmptyBytes;
            else if (mki.Length > 255) throw new ArgumentException("cannot be longer than 255 bytes", "mki");

            this.ProtectionProfiles = protectionProfiles;
            this.Mki                = mki;
        }

        /// <returns>see <see cref="SrtpProtectionProfile" /> for valid constants.</returns>
        public int[] ProtectionProfiles { get; }

        /// <returns>valid lengths from 0 to 255.</returns>
        public byte[] Mki { get; }
    }
}
#pragma warning restore
#endif