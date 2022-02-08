#if !BESTHTTP_DISABLE_ALTERNATE_SSL && (!UNITY_WEBGL || UNITY_EDITOR)
#pragma warning disable
namespace BestHTTP.SecureProtocol.Org.BouncyCastle.Tls
{
    using System;
    using System.IO;

    public sealed class KeyShareEntry
    {
        private static bool CheckKeyExchangeLength(int length) { return 0 < length && length < 1 << 16; }

        /// <param name="namedGroup">
        ///     <see cref="NamedGroup" />
        /// </param>
        /// <param name="keyExchange"></param>
        public KeyShareEntry(int namedGroup, byte[] keyExchange)
        {
            if (!TlsUtilities.IsValidUint16(namedGroup))
                throw new ArgumentException("should be a uint16", "namedGroup");
            if (null == keyExchange)
                throw new ArgumentNullException("keyExchange");
            if (!CheckKeyExchangeLength(keyExchange.Length))
                throw new ArgumentException("must have length from 1 to (2^16 - 1)", "keyExchange");

            this.NamedGroup  = namedGroup;
            this.KeyExchange = keyExchange;
        }

        /// <returns>
        ///     <see cref="NamedGroup" />
        /// </returns>
        public int NamedGroup { get; }

        public byte[] KeyExchange { get; }

        /// <summary>Encode this <see cref="KeyShareEntry" /> to a <see cref="Stream" />.</summary>
        /// <param name="output">the <see cref="Stream" /> to encode to.</param>
        /// <exception cref="IOException" />
        public void Encode(Stream output)
        {
            TlsUtilities.WriteUint16(this.NamedGroup, output);
            TlsUtilities.WriteOpaque16(this.KeyExchange, output);
        }

        /// <summary>Parse a <see cref="KeyShareEntry" /> from a <see cref="Stream" />.</summary>
        /// <param name="input">the <see cref="Stream" /> to parse from.</param>
        /// <returns>a <see cref="KeyShareEntry" /> object.</returns>
        /// <exception cref="IOException" />
        public static KeyShareEntry Parse(Stream input)
        {
            var namedGroup  = TlsUtilities.ReadUint16(input);
            var keyExchange = TlsUtilities.ReadOpaque16(input, 1);
            return new KeyShareEntry(namedGroup, keyExchange);
        }
    }
}
#pragma warning restore
#endif