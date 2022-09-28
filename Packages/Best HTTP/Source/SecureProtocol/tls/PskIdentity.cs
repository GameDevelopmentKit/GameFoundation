#if !BESTHTTP_DISABLE_ALTERNATE_SSL && (!UNITY_WEBGL || UNITY_EDITOR)
#pragma warning disable
namespace BestHTTP.SecureProtocol.Org.BouncyCastle.Tls
{
    using System;
    using System.IO;
    using BestHTTP.SecureProtocol.Org.BouncyCastle.Utilities;

    public sealed class PskIdentity
    {
        public PskIdentity(byte[] identity, long obfuscatedTicketAge)
        {
            if (null == identity)
                throw new ArgumentNullException("identity");
            if (identity.Length < 1 || !TlsUtilities.IsValidUint16(identity.Length))
                throw new ArgumentException("should have length from 1 to 65535", "identity");
            if (!TlsUtilities.IsValidUint32(obfuscatedTicketAge))
                throw new ArgumentException("should be a uint32", "obfuscatedTicketAge");

            this.Identity            = identity;
            this.ObfuscatedTicketAge = obfuscatedTicketAge;
        }

        public int GetEncodedLength() { return 6 + this.Identity.Length; }

        public byte[] Identity { get; }

        public long ObfuscatedTicketAge { get; }

        public void Encode(Stream output)
        {
            TlsUtilities.WriteOpaque16(this.Identity, output);
            TlsUtilities.WriteUint32(this.ObfuscatedTicketAge, output);
        }

        public static PskIdentity Parse(Stream input)
        {
            var identity            = TlsUtilities.ReadOpaque16(input, 1);
            var obfuscatedTicketAge = TlsUtilities.ReadUint32(input);
            return new PskIdentity(identity, obfuscatedTicketAge);
        }

        public override bool Equals(object obj)
        {
            var that = obj as PskIdentity;
            if (null == that)
                return false;

            return this.ObfuscatedTicketAge == that.ObfuscatedTicketAge
                   && Arrays.ConstantTimeAreEqual(this.Identity, that.Identity);
        }

        public override int GetHashCode() { return Arrays.GetHashCode(this.Identity) ^ this.ObfuscatedTicketAge.GetHashCode(); }
    }
}
#pragma warning restore
#endif