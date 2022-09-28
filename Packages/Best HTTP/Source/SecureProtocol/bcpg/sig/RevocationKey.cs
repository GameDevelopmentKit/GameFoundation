#if !BESTHTTP_DISABLE_ALTERNATE_SSL && (!UNITY_WEBGL || UNITY_EDITOR)
#pragma warning disable
namespace BestHTTP.SecureProtocol.Org.BouncyCastle.Bcpg
{
	using System;

	/// <summary>
    ///     Represents revocation key OpenPGP signature sub packet.
    /// </summary>
    public class RevocationKey
        : SignatureSubpacket
    {
        // 1 octet of class, 
        // 1 octet of public-key algorithm ID, 
        // 20 octets of fingerprint
        public RevocationKey(
            bool isCritical,
            bool isLongLength,
            byte[] data)
            : base(SignatureSubpacketTag.RevocationKey, isCritical, isLongLength, data)
        {
        }

        public RevocationKey(
            bool isCritical,
            RevocationKeyTag signatureClass,
            PublicKeyAlgorithmTag keyAlgorithm,
            byte[] fingerprint)
            : base(SignatureSubpacketTag.RevocationKey, isCritical, false,
                CreateData(signatureClass, keyAlgorithm, fingerprint))
        {
        }

        private static byte[] CreateData(
            RevocationKeyTag signatureClass,
            PublicKeyAlgorithmTag keyAlgorithm,
            byte[] fingerprint)
        {
            var data = new byte[2 + fingerprint.Length];
            data[0] = (byte)signatureClass;
            data[1] = (byte)keyAlgorithm;
            Array.Copy(fingerprint, 0, data, 2, fingerprint.Length);
            return data;
        }

        public virtual RevocationKeyTag SignatureClass => (RevocationKeyTag)this.GetData()[0];

        public virtual PublicKeyAlgorithmTag Algorithm => (PublicKeyAlgorithmTag)this.GetData()[1];

        public virtual byte[] GetFingerprint()
        {
            var data        = this.GetData();
            var fingerprint = new byte[data.Length - 2];
            Array.Copy(data, 2, fingerprint, 0, fingerprint.Length);
            return fingerprint;
        }
    }
}
#pragma warning restore
#endif