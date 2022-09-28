#if !BESTHTTP_DISABLE_ALTERNATE_SSL && (!UNITY_WEBGL || UNITY_EDITOR)
#pragma warning disable
namespace BestHTTP.SecureProtocol.Org.BouncyCastle.Bcpg
{
    using System;
    using BestHTTP.SecureProtocol.Org.BouncyCastle.Utilities;

    /// <summary>
    ///     Represents revocation reason OpenPGP signature sub packet.
    /// </summary>
    public class RevocationReason
        : SignatureSubpacket
    {
        public RevocationReason(bool isCritical, bool isLongLength, byte[] data)
            : base(SignatureSubpacketTag.RevocationReason, isCritical, isLongLength, data)
        {
        }

        public RevocationReason(
            bool isCritical,
            RevocationReasonTag reason,
            string description)
            : base(SignatureSubpacketTag.RevocationReason, isCritical, false, CreateData(reason, description))
        {
        }

        private static byte[] CreateData(
            RevocationReasonTag reason,
            string description)
        {
            var descriptionBytes = Strings.ToUtf8ByteArray(description);
            var data             = new byte[1 + descriptionBytes.Length];

            data[0] = (byte)reason;
            Array.Copy(descriptionBytes, 0, data, 1, descriptionBytes.Length);

            return data;
        }

        public virtual RevocationReasonTag GetRevocationReason() { return (RevocationReasonTag)this.GetData()[0]; }

        public virtual string GetRevocationDescription()
        {
            var data = this.GetData();
            if (data.Length == 1) return string.Empty;

            var description = new byte[data.Length - 1];
            Array.Copy(data, 1, description, 0, description.Length);

            return Strings.FromUtf8ByteArray(description);
        }
    }
}
#pragma warning restore
#endif