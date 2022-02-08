#if !BESTHTTP_DISABLE_ALTERNATE_SSL && (!UNITY_WEBGL || UNITY_EDITOR)
#pragma warning disable
namespace BestHTTP.SecureProtocol.Org.BouncyCastle.Tls.Crypto.Impl.BC
{
    using System;
    using System.IO;
    using BestHTTP.SecureProtocol.Org.BouncyCastle.Asn1;
    using BestHTTP.SecureProtocol.Org.BouncyCastle.Asn1.X509;
    using BestHTTP.SecureProtocol.Org.BouncyCastle.Crypto;
    using BestHTTP.SecureProtocol.Org.BouncyCastle.Crypto.Parameters;
    using BestHTTP.SecureProtocol.Org.BouncyCastle.Math;
    using BestHTTP.SecureProtocol.Org.BouncyCastle.Security;
    using BestHTTP.SecureProtocol.Org.BouncyCastle.Utilities;

    /// <summary>Implementation class for a single X.509 certificate based on the BC light-weight API.</summary>
    public class BcTlsCertificate
        : TlsCertificate
    {
        /// <exception cref="IOException" />
        public static BcTlsCertificate Convert(BcTlsCrypto crypto, TlsCertificate certificate)
        {
            if (certificate is BcTlsCertificate)
                return (BcTlsCertificate)certificate;

            return new BcTlsCertificate(crypto, certificate.GetEncoded());
        }

        /// <exception cref="IOException" />
        public static X509CertificateStructure ParseCertificate(byte[] encoding)
        {
            try
            {
                return X509CertificateStructure.GetInstance(encoding);
            }
            catch (Exception e)
            {
                throw new TlsFatalAlert(AlertDescription.bad_certificate, e);
            }
        }

        protected readonly BcTlsCrypto              m_crypto;
        protected readonly X509CertificateStructure m_certificate;

        protected DHPublicKeyParameters      m_pubKeyDH;
        protected ECPublicKeyParameters      m_pubKeyEC;
        protected Ed25519PublicKeyParameters m_pubKeyEd25519 = null;
        protected Ed448PublicKeyParameters   m_pubKeyEd448   = null;
        protected RsaKeyParameters           m_pubKeyRsa;

        /// <exception cref="IOException" />
        public BcTlsCertificate(BcTlsCrypto crypto, byte[] encoding)
            : this(crypto, ParseCertificate(encoding))
        {
        }

        public BcTlsCertificate(BcTlsCrypto crypto, X509CertificateStructure certificate)
        {
            this.m_crypto      = crypto;
            this.m_certificate = certificate;
        }

        /// <exception cref="IOException" />
        public virtual TlsEncryptor CreateEncryptor(int tlsCertificateRole)
        {
            this.ValidateKeyUsage(KeyUsage.KeyEncipherment);

            switch (tlsCertificateRole)
            {
                case TlsCertificateRole.RsaEncryption:
                {
                    this.m_pubKeyRsa = this.GetPubKeyRsa();
                    return new BcTlsRsaEncryptor(this.m_crypto, this.m_pubKeyRsa);
                }
                // TODO[gmssl]
                //case TlsCertificateRole.Sm2Encryption:
                //{
                //    this.m_pubKeyEC = GetPubKeyEC();
                //    return new BcTlsSM2Encryptor(m_crypto, m_pubKeyEC);
                //}
            }

            throw new TlsFatalAlert(AlertDescription.certificate_unknown);
        }

        /// <exception cref="IOException" />
        public virtual TlsVerifier CreateVerifier(short signatureAlgorithm)
        {
            switch (signatureAlgorithm)
            {
                case SignatureAlgorithm.rsa_pss_rsae_sha256:
                case SignatureAlgorithm.rsa_pss_rsae_sha384:
                case SignatureAlgorithm.rsa_pss_rsae_sha512:
                case SignatureAlgorithm.ed25519:
                case SignatureAlgorithm.ed448:
                case SignatureAlgorithm.rsa_pss_pss_sha256:
                case SignatureAlgorithm.rsa_pss_pss_sha384:
                case SignatureAlgorithm.rsa_pss_pss_sha512:
                    return this.CreateVerifier(SignatureScheme.From(HashAlgorithm.Intrinsic, signatureAlgorithm));
            }

            this.ValidateKeyUsage(KeyUsage.DigitalSignature);

            switch (signatureAlgorithm)
            {
                case SignatureAlgorithm.rsa:
                    this.ValidateRsa_Pkcs1();
                    return new BcTlsRsaVerifier(this.m_crypto, this.GetPubKeyRsa());

                case SignatureAlgorithm.dsa:
                    return new BcTlsDsaVerifier(this.m_crypto, this.GetPubKeyDss());

                case SignatureAlgorithm.ecdsa:
                    return new BcTlsECDsaVerifier(this.m_crypto, this.GetPubKeyEC());

                default:
                    throw new TlsFatalAlert(AlertDescription.certificate_unknown);
            }
        }

        /// <exception cref="IOException" />
        public virtual TlsVerifier CreateVerifier(int signatureScheme)
        {
            this.ValidateKeyUsage(KeyUsage.DigitalSignature);

            switch (signatureScheme)
            {
                case SignatureScheme.ecdsa_brainpoolP256r1tls13_sha256:
                case SignatureScheme.ecdsa_brainpoolP384r1tls13_sha384:
                case SignatureScheme.ecdsa_brainpoolP512r1tls13_sha512:
                case SignatureScheme.ecdsa_secp256r1_sha256:
                case SignatureScheme.ecdsa_secp384r1_sha384:
                case SignatureScheme.ecdsa_secp521r1_sha512:
                case SignatureScheme.ecdsa_sha1:
                    return new BcTlsECDsa13Verifier(this.m_crypto, this.GetPubKeyEC(), signatureScheme);

                case SignatureScheme.ed25519:
                    return new BcTlsEd25519Verifier(this.m_crypto, this.GetPubKeyEd25519());

                case SignatureScheme.ed448:
                    return new BcTlsEd448Verifier(this.m_crypto, this.GetPubKeyEd448());

                case SignatureScheme.rsa_pkcs1_sha1:
                case SignatureScheme.rsa_pkcs1_sha256:
                case SignatureScheme.rsa_pkcs1_sha384:
                case SignatureScheme.rsa_pkcs1_sha512:
                {
                    this.ValidateRsa_Pkcs1();
                    return new BcTlsRsaVerifier(this.m_crypto, this.GetPubKeyRsa());
                }

                case SignatureScheme.rsa_pss_pss_sha256:
                case SignatureScheme.rsa_pss_pss_sha384:
                case SignatureScheme.rsa_pss_pss_sha512:
                {
                    this.ValidateRsa_Pss_Pss(SignatureScheme.GetSignatureAlgorithm(signatureScheme));
                    return new BcTlsRsaPssVerifier(this.m_crypto, this.GetPubKeyRsa(), signatureScheme);
                }

                case SignatureScheme.rsa_pss_rsae_sha256:
                case SignatureScheme.rsa_pss_rsae_sha384:
                case SignatureScheme.rsa_pss_rsae_sha512:
                {
                    this.ValidateRsa_Pss_Rsae();
                    return new BcTlsRsaPssVerifier(this.m_crypto, this.GetPubKeyRsa(), signatureScheme);
                }

                // TODO[RFC 8998]
                //case SignatureScheme.sm2sig_sm3:
                //    return new BcTlsSM2Verifier(m_crypto, GetPubKeyEC(), Strings.ToByteArray("TLSv1.3+GM+Cipher+Suite"));

                default:
                    throw new TlsFatalAlert(AlertDescription.certificate_unknown);
            }
        }

        /// <exception cref="IOException" />
        public virtual byte[] GetEncoded() { return this.m_certificate.GetEncoded(Asn1Encodable.Der); }

        /// <exception cref="IOException" />
        public virtual byte[] GetExtension(DerObjectIdentifier extensionOid)
        {
            var extensions = this.m_certificate.TbsCertificate.Extensions;
            if (extensions != null)
            {
                var extension = extensions.GetExtension(extensionOid);
                if (extension != null) return Arrays.Clone(extension.Value.GetOctets());
            }

            return null;
        }

        public virtual BigInteger SerialNumber => this.m_certificate.SerialNumber.Value;

        public virtual string SigAlgOid => this.m_certificate.SignatureAlgorithm.Algorithm.Id;

        public virtual Asn1Encodable GetSigAlgParams() { return this.m_certificate.SignatureAlgorithm.Parameters; }

        /// <exception cref="IOException" />
        public virtual short GetLegacySignatureAlgorithm()
        {
            var publicKey = this.GetPublicKey();
            if (publicKey.IsPrivate)
                throw new TlsFatalAlert(AlertDescription.internal_error);

            if (!this.SupportsKeyUsage(KeyUsage.DigitalSignature))
                return -1;

            /*
             * RFC 5246 7.4.6. Client Certificate
             */

            /*
             * RSA public key; the certificate MUST allow the key to be used for signing with the
             * signature scheme and hash algorithm that will be employed in the certificate verify
             * message.
             */
            if (publicKey is RsaKeyParameters)
                return SignatureAlgorithm.rsa;

            /*
                * DSA public key; the certificate MUST allow the key to be used for signing with the
                * hash algorithm that will be employed in the certificate verify message.
                */
            if (publicKey is DsaPublicKeyParameters)
                return SignatureAlgorithm.dsa;

            /*
             * ECDSA-capable public key; the certificate MUST allow the key to be used for signing
             * with the hash algorithm that will be employed in the certificate verify message; the
             * public key MUST use a curve and point format supported by the server.
             */
            if (publicKey is ECPublicKeyParameters)
                // TODO Check the curve and point format
                return SignatureAlgorithm.ecdsa;

            return -1;
        }

        /// <exception cref="IOException" />
        public virtual DHPublicKeyParameters GetPubKeyDH()
        {
            try
            {
                return (DHPublicKeyParameters)this.GetPublicKey();
            }
            catch (InvalidCastException e)
            {
                throw new TlsFatalAlert(AlertDescription.certificate_unknown, e);
            }
        }

        /// <exception cref="IOException" />
        public virtual DsaPublicKeyParameters GetPubKeyDss()
        {
            try
            {
                return (DsaPublicKeyParameters)this.GetPublicKey();
            }
            catch (InvalidCastException e)
            {
                throw new TlsFatalAlert(AlertDescription.certificate_unknown, e);
            }
        }

        /// <exception cref="IOException" />
        public virtual ECPublicKeyParameters GetPubKeyEC()
        {
            try
            {
                return (ECPublicKeyParameters)this.GetPublicKey();
            }
            catch (InvalidCastException e)
            {
                throw new TlsFatalAlert(AlertDescription.certificate_unknown, e);
            }
        }

        /// <exception cref="IOException" />
        public virtual Ed25519PublicKeyParameters GetPubKeyEd25519()
        {
            try
            {
                return (Ed25519PublicKeyParameters)this.GetPublicKey();
            }
            catch (InvalidCastException e)
            {
                throw new TlsFatalAlert(AlertDescription.certificate_unknown, e);
            }
        }

        /// <exception cref="IOException" />
        public virtual Ed448PublicKeyParameters GetPubKeyEd448()
        {
            try
            {
                return (Ed448PublicKeyParameters)this.GetPublicKey();
            }
            catch (InvalidCastException e)
            {
                throw new TlsFatalAlert(AlertDescription.certificate_unknown, e);
            }
        }

        /// <exception cref="IOException" />
        public virtual RsaKeyParameters GetPubKeyRsa()
        {
            try
            {
                return (RsaKeyParameters)this.GetPublicKey();
            }
            catch (InvalidCastException e)
            {
                throw new TlsFatalAlert(AlertDescription.certificate_unknown, e);
            }
        }

        /// <exception cref="IOException" />
        public virtual bool SupportsSignatureAlgorithm(short signatureAlgorithm) { return this.SupportsSignatureAlgorithm(signatureAlgorithm, KeyUsage.DigitalSignature); }

        /// <exception cref="IOException" />
        public virtual bool SupportsSignatureAlgorithmCA(short signatureAlgorithm) { return this.SupportsSignatureAlgorithm(signatureAlgorithm, KeyUsage.KeyCertSign); }

        /// <exception cref="IOException" />
        public virtual TlsCertificate CheckUsageInRole(int tlsCertificateRole)
        {
            switch (tlsCertificateRole)
            {
                case TlsCertificateRole.DH:
                {
                    this.ValidateKeyUsage(KeyUsage.KeyAgreement);
                    this.m_pubKeyDH = this.GetPubKeyDH();
                    return this;
                }
                case TlsCertificateRole.ECDH:
                {
                    this.ValidateKeyUsage(KeyUsage.KeyAgreement);
                    this.m_pubKeyEC = this.GetPubKeyEC();
                    return this;
                }
            }

            throw new TlsFatalAlert(AlertDescription.certificate_unknown);
        }

        /// <exception cref="IOException" />
        protected virtual AsymmetricKeyParameter GetPublicKey()
        {
            var keyInfo = this.m_certificate.SubjectPublicKeyInfo;
            try
            {
                return PublicKeyFactory.CreateKey(keyInfo);
            }
            catch (Exception e)
            {
                throw new TlsFatalAlert(AlertDescription.unsupported_certificate, e);
            }
        }

        protected virtual bool SupportsKeyUsage(int keyUsageBits)
        {
            var exts = this.m_certificate.TbsCertificate.Extensions;
            if (exts != null)
            {
                var ku = KeyUsage.FromExtensions(exts);
                if (ku != null)
                {
                    var bits = ku.GetBytes()[0] & 0xff;
                    if ((bits & keyUsageBits) != keyUsageBits)
                        return false;
                }
            }

            return true;
        }

        protected virtual bool SupportsRsa_Pkcs1()
        {
            var pubKeyAlgID = this.m_certificate.SubjectPublicKeyInfo.AlgorithmID;
            return RsaUtilities.SupportsPkcs1(pubKeyAlgID);
        }

        protected virtual bool SupportsRsa_Pss_Pss(short signatureAlgorithm)
        {
            var pubKeyAlgID = this.m_certificate.SubjectPublicKeyInfo.AlgorithmID;
            return RsaUtilities.SupportsPss_Pss(signatureAlgorithm, pubKeyAlgID);
        }

        protected virtual bool SupportsRsa_Pss_Rsae()
        {
            var pubKeyAlgID = this.m_certificate.SubjectPublicKeyInfo.AlgorithmID;
            return RsaUtilities.SupportsPss_Rsae(pubKeyAlgID);
        }

        /// <exception cref="IOException" />
        protected virtual bool SupportsSignatureAlgorithm(short signatureAlgorithm, int keyUsage)
        {
            if (!this.SupportsKeyUsage(keyUsage))
                return false;

            var publicKey = this.GetPublicKey();

            switch (signatureAlgorithm)
            {
                case SignatureAlgorithm.rsa:
                    return this.SupportsRsa_Pkcs1()
                           && publicKey is RsaKeyParameters;

                case SignatureAlgorithm.dsa:
                    return publicKey is DsaPublicKeyParameters;

                case SignatureAlgorithm.ecdsa:
                case SignatureAlgorithm.ecdsa_brainpoolP256r1tls13_sha256:
                case SignatureAlgorithm.ecdsa_brainpoolP384r1tls13_sha384:
                case SignatureAlgorithm.ecdsa_brainpoolP512r1tls13_sha512:
                    return publicKey is ECPublicKeyParameters;

                case SignatureAlgorithm.ed25519:
                    return publicKey is Ed25519PublicKeyParameters;

                case SignatureAlgorithm.ed448:
                    return publicKey is Ed448PublicKeyParameters;

                case SignatureAlgorithm.rsa_pss_rsae_sha256:
                case SignatureAlgorithm.rsa_pss_rsae_sha384:
                case SignatureAlgorithm.rsa_pss_rsae_sha512:
                    return this.SupportsRsa_Pss_Rsae()
                           && publicKey is RsaKeyParameters;

                case SignatureAlgorithm.rsa_pss_pss_sha256:
                case SignatureAlgorithm.rsa_pss_pss_sha384:
                case SignatureAlgorithm.rsa_pss_pss_sha512:
                    return this.SupportsRsa_Pss_Pss(signatureAlgorithm)
                           && publicKey is RsaKeyParameters;

                default:
                    return false;
            }
        }

        /// <exception cref="IOException" />
        public virtual void ValidateKeyUsage(int keyUsageBits)
        {
            if (!this.SupportsKeyUsage(keyUsageBits))
                throw new TlsFatalAlert(AlertDescription.certificate_unknown);
        }

        /// <exception cref="IOException" />
        protected virtual void ValidateRsa_Pkcs1()
        {
            if (!this.SupportsRsa_Pkcs1())
                throw new TlsFatalAlert(AlertDescription.certificate_unknown);
        }

        /// <exception cref="IOException" />
        protected virtual void ValidateRsa_Pss_Pss(short signatureAlgorithm)
        {
            if (!this.SupportsRsa_Pss_Pss(signatureAlgorithm))
                throw new TlsFatalAlert(AlertDescription.certificate_unknown);
        }

        /// <exception cref="IOException" />
        protected virtual void ValidateRsa_Pss_Rsae()
        {
            if (!this.SupportsRsa_Pss_Rsae())
                throw new TlsFatalAlert(AlertDescription.certificate_unknown);
        }
    }
}
#pragma warning restore
#endif