#if !BESTHTTP_DISABLE_ALTERNATE_SSL && (!UNITY_WEBGL || UNITY_EDITOR)
#pragma warning disable
namespace BestHTTP.SecureProtocol.Org.BouncyCastle.Cmp
{
    using System;
    using BestHTTP.SecureProtocol.Org.BouncyCastle.Asn1;
    using BestHTTP.SecureProtocol.Org.BouncyCastle.Asn1.Cmp;
    using BestHTTP.SecureProtocol.Org.BouncyCastle.Asn1.X509;
    using BestHTTP.SecureProtocol.Org.BouncyCastle.Crmf;
    using BestHTTP.SecureProtocol.Org.BouncyCastle.Crypto;
    using BestHTTP.SecureProtocol.Org.BouncyCastle.Utilities;
    using BestHTTP.SecureProtocol.Org.BouncyCastle.X509;

    /// <summary>
    ///     Wrapper for a PKIMessage with protection attached to it.
    /// </summary>
    public class ProtectedPkiMessage
    {
        private readonly PkiMessage pkiMessage;

        /// <summary>
        ///     Wrap a general message.
        /// </summary>
        /// <exception cref="ArgumentException">If the general message does not have protection.</exception>
        /// <param name="pkiMessage">The General message</param>
        public ProtectedPkiMessage(GeneralPkiMessage pkiMessage)
        {
            if (!pkiMessage.HasProtection)
                throw new ArgumentException("pki message not protected");

            this.pkiMessage = pkiMessage.ToAsn1Structure();
        }

        /// <summary>
        ///     Wrap a PKI message.
        /// </summary>
        /// <exception cref="ArgumentException">If the PKI message does not have protection.</exception>
        /// <param name="pkiMessage">The PKI message</param>
        public ProtectedPkiMessage(PkiMessage pkiMessage)
        {
            if (null == pkiMessage.Header.ProtectionAlg)
                throw new ArgumentException("pki message not protected");

            this.pkiMessage = pkiMessage;
        }

        /// <summary>
        ///     Message header
        /// </summary>
        public PkiHeader Header => this.pkiMessage.Header;

        /// <summary>
        ///     Message Body
        /// </summary>
        public PkiBody Body => this.pkiMessage.Body;

        /// <summary>
        ///     Return the underlying ASN.1 structure contained in this object.
        /// </summary>
        /// <returns>PKI Message structure</returns>
        public PkiMessage ToAsn1Message() { return this.pkiMessage; }

        /// <summary>
        ///     Determine whether the message is protected by a password based MAC. Use verify(PKMACBuilder, char[])
        ///     to verify the message if this method returns true.
        /// </summary>
        /// <returns>true if protection MAC PBE based, false otherwise.</returns>
        public bool HasPasswordBasedMacProtected => this.Header.ProtectionAlg.Algorithm.Equals(CmpObjectIdentifiers.passwordBasedMac);

        /// <summary>
        ///     Return the extra certificates associated with this message.
        /// </summary>
        /// <returns>an array of extra certificates, zero length if none present.</returns>
        public X509Certificate[] GetCertificates()
        {
            var certs = this.pkiMessage.GetExtraCerts();
            if (null == certs)
                return new X509Certificate[0];

            var res                                       = new X509Certificate[certs.Length];
            for (var t = 0; t < certs.Length; t++) res[t] = new X509Certificate(X509CertificateStructure.GetInstance(certs[t].GetEncoded()));

            return res;
        }

        /// <summary>
        ///     Verify a message with a public key based signature attached.
        /// </summary>
        /// <param name="verifierFactory">a factory of signature verifiers.</param>
        /// <returns>true if the provider is able to create a verifier that validates the signature, false otherwise.</returns>
        public bool Verify(IVerifierFactory verifierFactory)
        {
            var streamCalculator = verifierFactory.CreateCalculator();

            var result = (IVerifier)this.Process(streamCalculator);

            return result.IsVerified(this.pkiMessage.Protection.GetBytes());
        }

        private object Process(IStreamCalculator streamCalculator)
        {
            var avec = new Asn1EncodableVector();
            avec.Add(this.pkiMessage.Header);
            avec.Add(this.pkiMessage.Body);
            var enc = new DerSequence(avec).GetDerEncoded();

            streamCalculator.Stream.Write(enc, 0, enc.Length);
            streamCalculator.Stream.Flush();
            Platform.Dispose(streamCalculator.Stream);

            return streamCalculator.GetResult();
        }

        /// <summary>
        ///     Verify a message with password based MAC protection.
        /// </summary>
        /// <param name="pkMacBuilder">MAC builder that can be used to construct the appropriate MacCalculator</param>
        /// <param name="password">the MAC password</param>
        /// <returns>true if the passed in password and MAC builder verify the message, false otherwise.</returns>
        /// <exception cref="InvalidOperationException">if algorithm not MAC based, or an exception is thrown verifying the MAC.</exception>
        public bool Verify(PKMacBuilder pkMacBuilder, char[] password)
        {
            if (!CmpObjectIdentifiers.passwordBasedMac.Equals(this.pkiMessage.Header.ProtectionAlg.Algorithm))
                throw new InvalidOperationException("protection algorithm is not mac based");

            var parameter = PbmParameter.GetInstance(this.pkiMessage.Header.ProtectionAlg.Parameters);

            pkMacBuilder.SetParameters(parameter);

            var result = (IBlockResult)this.Process(pkMacBuilder.Build(password).CreateCalculator());

            return Arrays.ConstantTimeAreEqual(result.Collect(), this.pkiMessage.Protection.GetBytes());
        }
    }
}
#pragma warning restore
#endif