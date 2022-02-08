#if !BESTHTTP_DISABLE_ALTERNATE_SSL && (!UNITY_WEBGL || UNITY_EDITOR)
#pragma warning disable
namespace BestHTTP.SecureProtocol.Org.BouncyCastle.Cmp
{
    using BestHTTP.SecureProtocol.Org.BouncyCastle.Asn1;
    using BestHTTP.SecureProtocol.Org.BouncyCastle.Asn1.Cmp;

    public class GeneralPkiMessage
    {
        private readonly PkiMessage pkiMessage;

        private static PkiMessage ParseBytes(byte[] encoding) { return PkiMessage.GetInstance(Asn1Object.FromByteArray(encoding)); }

        /// <summary>
        ///     Wrap a PKIMessage ASN.1 structure.
        /// </summary>
        /// <param name="pkiMessage">PKI message.</param>
        public GeneralPkiMessage(PkiMessage pkiMessage) { this.pkiMessage = pkiMessage; }

        /// <summary>
        ///     Create a PKIMessage from the passed in bytes.
        /// </summary>
        /// <param name="encoding">BER/DER encoding of the PKIMessage</param>
        public GeneralPkiMessage(byte[] encoding)
            : this(ParseBytes(encoding))
        {
        }

        public PkiHeader Header => this.pkiMessage.Header;

        public PkiBody Body => this.pkiMessage.Body;

        /// <summary>
        ///     Return true if this message has protection bits on it. A return value of true
        ///     indicates the message can be used to construct a ProtectedPKIMessage.
        /// </summary>
        public bool HasProtection => this.pkiMessage.Protection != null;

        public PkiMessage ToAsn1Structure() { return this.pkiMessage; }
    }
}
#pragma warning restore
#endif