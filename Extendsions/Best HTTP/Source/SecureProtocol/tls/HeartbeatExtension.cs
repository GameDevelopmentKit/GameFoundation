#if !BESTHTTP_DISABLE_ALTERNATE_SSL && (!UNITY_WEBGL || UNITY_EDITOR)
#pragma warning disable
namespace BestHTTP.SecureProtocol.Org.BouncyCastle.Tls
{
    using System;
    using System.IO;

    public sealed class HeartbeatExtension
    {
        public HeartbeatExtension(short mode)
        {
            if (!HeartbeatMode.IsValid(mode))
                throw new ArgumentException("not a valid HeartbeatMode value", "mode");

            this.Mode = mode;
        }

        public short Mode { get; }

        /// <summary>Encode this <see cref="HeartbeatExtension" /> to a <see cref="Stream" />.</summary>
        /// <param name="output">the <see cref="Stream" /> to encode to.</param>
        /// <exception cref="IOException" />
        public void Encode(Stream output) { TlsUtilities.WriteUint8(this.Mode, output); }

        /// <summary>Parse a <see cref="HeartbeatExtension" /> from a <see cref="Stream" />.</summary>
        /// <param name="input">the <see cref="Stream" /> to parse from.</param>
        /// <returns>a <see cref="HeartbeatExtension" /> object.</returns>
        /// <exception cref="IOException" />
        public static HeartbeatExtension Parse(Stream input)
        {
            var mode = TlsUtilities.ReadUint8(input);
            if (!HeartbeatMode.IsValid(mode))
                throw new TlsFatalAlert(AlertDescription.illegal_parameter);

            return new HeartbeatExtension(mode);
        }
    }
}
#pragma warning restore
#endif