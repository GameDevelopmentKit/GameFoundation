#if !BESTHTTP_DISABLE_ALTERNATE_SSL && (!UNITY_WEBGL || UNITY_EDITOR)
#pragma warning disable
namespace BestHTTP.SecureProtocol.Org.BouncyCastle.Tls
{
    using System;
    using System.Collections;
    using System.IO;

    /// <summary>RFC 5764 DTLS Extension to Establish Keys for SRTP.</summary>
    public abstract class TlsSrtpUtilities
    {
        /// <exception cref="IOException" />
        public static void AddUseSrtpExtension(IDictionary extensions, UseSrtpData useSrtpData) { extensions[ExtensionType.use_srtp] = CreateUseSrtpExtension(useSrtpData); }

        /// <exception cref="IOException" />
        public static UseSrtpData GetUseSrtpExtension(IDictionary extensions)
        {
            var extensionData = TlsUtilities.GetExtensionData(extensions, ExtensionType.use_srtp);
            return extensionData == null ? null : ReadUseSrtpExtension(extensionData);
        }

        /// <exception cref="IOException" />
        public static byte[] CreateUseSrtpExtension(UseSrtpData useSrtpData)
        {
            if (useSrtpData == null)
                throw new ArgumentNullException("useSrtpData");

            var buf = new MemoryStream();

            // SRTPProtectionProfiles
            TlsUtilities.WriteUint16ArrayWithUint16Length(useSrtpData.ProtectionProfiles, buf);

            // srtp_mki
            TlsUtilities.WriteOpaque8(useSrtpData.Mki, buf);

            return buf.ToArray();
        }

        /// <exception cref="IOException" />
        public static UseSrtpData ReadUseSrtpExtension(byte[] extensionData)
        {
            if (extensionData == null)
                throw new ArgumentNullException("extensionData");

            var buf = new MemoryStream(extensionData, false);

            // SRTPProtectionProfiles
            var length = TlsUtilities.ReadUint16(buf);
            if (length < 2 || (length & 1) != 0)
                throw new TlsFatalAlert(AlertDescription.decode_error);

            var protectionProfiles = TlsUtilities.ReadUint16Array(length / 2, buf);

            // srtp_mki
            var mki = TlsUtilities.ReadOpaque8(buf);

            TlsProtocol.AssertEmpty(buf);

            return new UseSrtpData(protectionProfiles, mki);
        }
    }
}
#pragma warning restore
#endif