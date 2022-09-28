#if !BESTHTTP_DISABLE_ALTERNATE_SSL && (!UNITY_WEBGL || UNITY_EDITOR)
#pragma warning disable
namespace BestHTTP.SecureProtocol.Org.BouncyCastle.Tls.Crypto.Impl
{
    using System;
    using System.IO;
    using BestHTTP.SecureProtocol.Org.BouncyCastle.Asn1;
    using BestHTTP.SecureProtocol.Org.BouncyCastle.Asn1.Nist;
    using BestHTTP.SecureProtocol.Org.BouncyCastle.Asn1.Pkcs;
    using BestHTTP.SecureProtocol.Org.BouncyCastle.Asn1.X509;
    using BestHTTP.SecureProtocol.Org.BouncyCastle.Utilities;

    public abstract class RsaUtilities
    {
        private static readonly byte[] RSAPSSParams_256_A, RSAPSSParams_384_A, RSAPSSParams_512_A;
        private static readonly byte[] RSAPSSParams_256_B, RSAPSSParams_384_B, RSAPSSParams_512_B;

        static RsaUtilities()
        {
            /*
             * RFC 4055
             */

            var sha256Identifier_A = new AlgorithmIdentifier(NistObjectIdentifiers.IdSha256);
            var sha384Identifier_A = new AlgorithmIdentifier(NistObjectIdentifiers.IdSha384);
            var sha512Identifier_A = new AlgorithmIdentifier(NistObjectIdentifiers.IdSha512);
            var sha256Identifier_B = new AlgorithmIdentifier(NistObjectIdentifiers.IdSha256, DerNull.Instance);
            var sha384Identifier_B = new AlgorithmIdentifier(NistObjectIdentifiers.IdSha384, DerNull.Instance);
            var sha512Identifier_B = new AlgorithmIdentifier(NistObjectIdentifiers.IdSha512, DerNull.Instance);

            var mgf1SHA256Identifier_A = new AlgorithmIdentifier(PkcsObjectIdentifiers.IdMgf1, sha256Identifier_A);
            var mgf1SHA384Identifier_A = new AlgorithmIdentifier(PkcsObjectIdentifiers.IdMgf1, sha384Identifier_A);
            var mgf1SHA512Identifier_A = new AlgorithmIdentifier(PkcsObjectIdentifiers.IdMgf1, sha512Identifier_A);
            var mgf1SHA256Identifier_B = new AlgorithmIdentifier(PkcsObjectIdentifiers.IdMgf1, sha256Identifier_B);
            var mgf1SHA384Identifier_B = new AlgorithmIdentifier(PkcsObjectIdentifiers.IdMgf1, sha384Identifier_B);
            var mgf1SHA512Identifier_B = new AlgorithmIdentifier(PkcsObjectIdentifiers.IdMgf1, sha512Identifier_B);

            var sha256Size = new DerInteger(TlsCryptoUtilities.GetHashOutputSize(CryptoHashAlgorithm.sha256));
            var sha384Size = new DerInteger(TlsCryptoUtilities.GetHashOutputSize(CryptoHashAlgorithm.sha384));
            var sha512Size = new DerInteger(TlsCryptoUtilities.GetHashOutputSize(CryptoHashAlgorithm.sha512));

            var trailerField = new DerInteger(1);

            try
            {
                RSAPSSParams_256_A = new RsassaPssParameters(sha256Identifier_A, mgf1SHA256Identifier_A, sha256Size, trailerField)
                    .GetEncoded(Asn1Encodable.Der);
                RSAPSSParams_384_A = new RsassaPssParameters(sha384Identifier_A, mgf1SHA384Identifier_A, sha384Size, trailerField)
                    .GetEncoded(Asn1Encodable.Der);
                RSAPSSParams_512_A = new RsassaPssParameters(sha512Identifier_A, mgf1SHA512Identifier_A, sha512Size, trailerField)
                    .GetEncoded(Asn1Encodable.Der);
                RSAPSSParams_256_B = new RsassaPssParameters(sha256Identifier_B, mgf1SHA256Identifier_B, sha256Size, trailerField)
                    .GetEncoded(Asn1Encodable.Der);
                RSAPSSParams_384_B = new RsassaPssParameters(sha384Identifier_B, mgf1SHA384Identifier_B, sha384Size, trailerField)
                    .GetEncoded(Asn1Encodable.Der);
                RSAPSSParams_512_B = new RsassaPssParameters(sha512Identifier_B, mgf1SHA512Identifier_B, sha512Size, trailerField)
                    .GetEncoded(Asn1Encodable.Der);
            }
            catch (IOException e)
            {
                throw new InvalidOperationException(e.Message);
            }
        }

        public static bool SupportsPkcs1(AlgorithmIdentifier pubKeyAlgID)
        {
            var oid = pubKeyAlgID.Algorithm;
            return PkcsObjectIdentifiers.RsaEncryption.Equals(oid)
                   || X509ObjectIdentifiers.IdEARsa.Equals(oid);
        }

        public static bool SupportsPss_Pss(short signatureAlgorithm, AlgorithmIdentifier pubKeyAlgID)
        {
            var oid = pubKeyAlgID.Algorithm;
            if (!PkcsObjectIdentifiers.IdRsassaPss.Equals(oid))
                return false;

            /*
             * TODO ASN.1 NULL shouldn't really be allowed here; it's a workaround for e.g. Oracle JDK
             * 1.8.0_241, where the X.509 certificate implementation adds the NULL when re-encoding the
             * original parameters. It appears it was fixed at some later date (OpenJDK 12.0.2 does not
             * have the issue), but not sure exactly when.
             */
            var pssParams = pubKeyAlgID.Parameters;
            if (null == pssParams || pssParams is DerNull)
                switch (signatureAlgorithm)
                {
                    case SignatureAlgorithm.rsa_pss_pss_sha256:
                    case SignatureAlgorithm.rsa_pss_pss_sha384:
                    case SignatureAlgorithm.rsa_pss_pss_sha512:
                        return true;
                    default:
                        return false;
                }

            byte[] encoded;
            try
            {
                encoded = pssParams.ToAsn1Object().GetEncoded(Asn1Encodable.Der);
            }
            catch (Exception)
            {
                return false;
            }

            byte[] expected_A, expected_B;
            switch (signatureAlgorithm)
            {
                case SignatureAlgorithm.rsa_pss_pss_sha256:
                    expected_A = RSAPSSParams_256_A;
                    expected_B = RSAPSSParams_256_B;
                    break;
                case SignatureAlgorithm.rsa_pss_pss_sha384:
                    expected_A = RSAPSSParams_384_A;
                    expected_B = RSAPSSParams_384_B;
                    break;
                case SignatureAlgorithm.rsa_pss_pss_sha512:
                    expected_A = RSAPSSParams_512_A;
                    expected_B = RSAPSSParams_512_B;
                    break;
                default:
                    return false;
            }

            return Arrays.AreEqual(expected_A, encoded)
                   || Arrays.AreEqual(expected_B, encoded);
        }

        public static bool SupportsPss_Rsae(AlgorithmIdentifier pubKeyAlgID)
        {
            var oid = pubKeyAlgID.Algorithm;
            return PkcsObjectIdentifiers.RsaEncryption.Equals(oid);
        }
    }
}
#pragma warning restore
#endif