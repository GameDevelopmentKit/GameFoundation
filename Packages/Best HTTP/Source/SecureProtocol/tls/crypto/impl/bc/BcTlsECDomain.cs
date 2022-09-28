#if !BESTHTTP_DISABLE_ALTERNATE_SSL && (!UNITY_WEBGL || UNITY_EDITOR)
#pragma warning disable
namespace BestHTTP.SecureProtocol.Org.BouncyCastle.Tls.Crypto.Impl.BC
{
    using System;
    using System.IO;
    using BestHTTP.SecureProtocol.Org.BouncyCastle.Asn1.X9;
    using BestHTTP.SecureProtocol.Org.BouncyCastle.Crypto;
    using BestHTTP.SecureProtocol.Org.BouncyCastle.Crypto.Agreement;
    using BestHTTP.SecureProtocol.Org.BouncyCastle.Crypto.EC;
    using BestHTTP.SecureProtocol.Org.BouncyCastle.Crypto.Generators;
    using BestHTTP.SecureProtocol.Org.BouncyCastle.Crypto.Parameters;
    using BestHTTP.SecureProtocol.Org.BouncyCastle.Math.EC;
    using BestHTTP.SecureProtocol.Org.BouncyCastle.Utilities;

    /**
     * EC domain class for generating key pairs and performing key agreement.
     */
    public class BcTlsECDomain
        : TlsECDomain
    {
        public static BcTlsSecret CalculateBasicAgreement(BcTlsCrypto crypto, ECPrivateKeyParameters privateKey,
            ECPublicKeyParameters publicKey)
        {
            var basicAgreement = new ECDHBasicAgreement();
            basicAgreement.Init(privateKey);
            var agreementValue = basicAgreement.CalculateAgreement(publicKey);

            /*
             * RFC 4492 5.10. Note that this octet string (Z in IEEE 1363 terminology) as output by
             * FE2OSP, the Field Element to Octet String Conversion Primitive, has constant length for
             * any given field; leading zeros found in this octet string MUST NOT be truncated.
             */
            var secret = BigIntegers.AsUnsignedByteArray(basicAgreement.GetFieldSize(), agreementValue);
            return crypto.AdoptLocalSecret(secret);
        }

        public static ECDomainParameters GetDomainParameters(TlsECConfig ecConfig) { return GetDomainParameters(ecConfig.NamedGroup); }

        public static ECDomainParameters GetDomainParameters(int namedGroup)
        {
            if (!NamedGroup.RefersToASpecificCurve(namedGroup))
                return null;

            // Parameters are lazily created the first time a particular curve is accessed

            var curveName = NamedGroup.GetCurveName(namedGroup);
            var ecP       = CustomNamedCurves.GetByName(curveName);
            if (ecP == null)
            {
                ecP = ECNamedCurveTable.GetByName(curveName);
                if (ecP == null)
                    return null;
            }

            // It's a bit inefficient to do this conversion every time
            return new ECDomainParameters(ecP.Curve, ecP.G, ecP.N, ecP.H, ecP.GetSeed());
        }

        protected readonly BcTlsCrypto        m_crypto;
        protected readonly TlsECConfig        m_ecConfig;
        protected readonly ECDomainParameters m_ecDomainParameters;

        public BcTlsECDomain(BcTlsCrypto crypto, TlsECConfig ecConfig)
        {
            this.m_crypto             = crypto;
            this.m_ecConfig           = ecConfig;
            this.m_ecDomainParameters = GetDomainParameters(ecConfig);
        }

        public virtual BcTlsSecret CalculateECDHAgreement(ECPrivateKeyParameters privateKey,
            ECPublicKeyParameters publicKey)
        {
            return CalculateBasicAgreement(this.m_crypto, privateKey, publicKey);
        }

        public virtual TlsAgreement CreateECDH() { return new BcTlsECDH(this); }

        public virtual ECPoint DecodePoint(byte[] encoding) { return this.m_ecDomainParameters.Curve.DecodePoint(encoding); }

        public virtual ECPublicKeyParameters DecodePublicKey(byte[] encoding)
        {
            try
            {
                var point = this.DecodePoint(encoding);

                return new ECPublicKeyParameters(point, this.m_ecDomainParameters);
            }
            catch (IOException e)
            {
                throw e;
            }
            catch (Exception e)
            {
                throw new TlsFatalAlert(AlertDescription.illegal_parameter, e);
            }
        }

        public virtual byte[] EncodePoint(ECPoint point) { return point.GetEncoded(false); }

        public virtual byte[] EncodePublicKey(ECPublicKeyParameters publicKey) { return this.EncodePoint(publicKey.Q); }

        public virtual AsymmetricCipherKeyPair GenerateKeyPair()
        {
            var keyPairGenerator = new ECKeyPairGenerator();
            keyPairGenerator.Init(new ECKeyGenerationParameters(this.m_ecDomainParameters, this.m_crypto.SecureRandom));
            return keyPairGenerator.GenerateKeyPair();
        }
    }
}
#pragma warning restore
#endif