#if !BESTHTTP_DISABLE_ALTERNATE_SSL && (!UNITY_WEBGL || UNITY_EDITOR)
#pragma warning disable
namespace BestHTTP.SecureProtocol.Org.BouncyCastle.Tsp
{
    using System;
    using System.Collections;
    using System.IO;
    using System.Text;
    using BestHTTP.SecureProtocol.Org.BouncyCastle.Asn1;
    using BestHTTP.SecureProtocol.Org.BouncyCastle.Asn1.Cmp;
    using BestHTTP.SecureProtocol.Org.BouncyCastle.Asn1.Ess;
    using BestHTTP.SecureProtocol.Org.BouncyCastle.Asn1.Oiw;
    using BestHTTP.SecureProtocol.Org.BouncyCastle.Asn1.Pkcs;
    using BestHTTP.SecureProtocol.Org.BouncyCastle.Asn1.Tsp;
    using BestHTTP.SecureProtocol.Org.BouncyCastle.Asn1.X509;
    using BestHTTP.SecureProtocol.Org.BouncyCastle.Cms;
    using BestHTTP.SecureProtocol.Org.BouncyCastle.Crypto;
    using BestHTTP.SecureProtocol.Org.BouncyCastle.Crypto.Operators;
    using BestHTTP.SecureProtocol.Org.BouncyCastle.Math;
    using BestHTTP.SecureProtocol.Org.BouncyCastle.Utilities;
    using BestHTTP.SecureProtocol.Org.BouncyCastle.X509;
    using BestHTTP.SecureProtocol.Org.BouncyCastle.X509.Store;
    using AttributeTable = BestHTTP.SecureProtocol.Org.BouncyCastle.Asn1.Cms.AttributeTable;

    public enum Resolution
    {
        R_SECONDS,
        R_TENTHS_OF_SECONDS,
        R_HUNDREDTHS_OF_SECONDS,
        R_MILLISECONDS
    }

    public class TimeStampTokenGenerator
    {
        private          int                 accuracySeconds = -1;
        private          int                 accuracyMillis  = -1;
        private          int                 accuracyMicros  = -1;
        private          bool                ordering;
        private          GeneralName         tsa;
        private readonly DerObjectIdentifier tsaPolicyOID;

        private          IX509Store          x509Certs;
        private          IX509Store          x509Crls;
        private readonly SignerInfoGenerator signerInfoGenerator;
        private          IDigestFactory      digestCalculator;

        public Resolution Resolution { get; set; } = Resolution.R_SECONDS;

        /**
		 * basic creation - only the default attributes will be included here.
		 */
        public TimeStampTokenGenerator(
            AsymmetricKeyParameter key,
            X509Certificate cert,
            string digestOID,
            string tsaPolicyOID)
            : this(key, cert, digestOID, tsaPolicyOID, null, null)
        {
        }


        public TimeStampTokenGenerator(
            SignerInfoGenerator signerInfoGen,
            IDigestFactory digestCalculator,
            DerObjectIdentifier tsaPolicy,
            bool isIssuerSerialIncluded)
        {
            this.signerInfoGenerator = signerInfoGen;
            this.digestCalculator    = digestCalculator;
            this.tsaPolicyOID        = tsaPolicy;

            if (this.signerInfoGenerator.certificate == null) throw new ArgumentException("SignerInfoGenerator must have an associated certificate");

            var assocCert = this.signerInfoGenerator.certificate;
            TspUtil.ValidateCertificate(assocCert);

            try
            {
                var calculator = digestCalculator.CreateCalculator();
                var stream     = calculator.Stream;
                var certEnc    = assocCert.GetEncoded();
                stream.Write(certEnc, 0, certEnc.Length);
                stream.Flush();
                Platform.Dispose(stream);

                if (((AlgorithmIdentifier)digestCalculator.AlgorithmDetails).Algorithm.Equals(OiwObjectIdentifiers.IdSha1))
                {
                    var essCertID = new EssCertID(
                        ((IBlockResult)calculator.GetResult()).Collect(),
                        isIssuerSerialIncluded
                            ? new IssuerSerial(
                                new GeneralNames(
                                    new GeneralName(assocCert.IssuerDN)),
                                new DerInteger(assocCert.SerialNumber))
                            : null);

                    this.signerInfoGenerator = signerInfoGen.NewBuilder()
                        .WithSignedAttributeGenerator(new TableGen(signerInfoGen, essCertID))
                        .Build(signerInfoGen.contentSigner, signerInfoGen.certificate);
                }
                else
                {
                    var digestAlgID = new AlgorithmIdentifier(
                        ((AlgorithmIdentifier)digestCalculator.AlgorithmDetails).Algorithm);

                    var essCertID = new EssCertIDv2(
                        ((IBlockResult)calculator.GetResult()).Collect(),
                        isIssuerSerialIncluded
                            ? new IssuerSerial(
                                new GeneralNames(
                                    new GeneralName(assocCert.IssuerDN)),
                                new DerInteger(assocCert.SerialNumber))
                            : null);

                    this.signerInfoGenerator = signerInfoGen.NewBuilder()
                        .WithSignedAttributeGenerator(new TableGen2(signerInfoGen, essCertID))
                        .Build(signerInfoGen.contentSigner, signerInfoGen.certificate);
                }
            }
            catch (Exception ex)
            {
                throw new TspException("Exception processing certificate", ex);
            }
        }

        /**
         * create with a signer with extra signed/unsigned attributes.
         */
        public TimeStampTokenGenerator(
            AsymmetricKeyParameter key,
            X509Certificate cert,
            string digestOID,
            string tsaPolicyOID,
            AttributeTable signedAttr,
            AttributeTable unsignedAttr) : this(
            makeInfoGenerator(key, cert, digestOID, signedAttr, unsignedAttr),
            Asn1DigestFactory.Get(OiwObjectIdentifiers.IdSha1),
            tsaPolicyOID != null ? new DerObjectIdentifier(tsaPolicyOID) : null, false)
        {
        }


        internal static SignerInfoGenerator makeInfoGenerator(
            AsymmetricKeyParameter key,
            X509Certificate cert,
            string digestOID,
            AttributeTable signedAttr,
            AttributeTable unsignedAttr)
        {
            TspUtil.ValidateCertificate(cert);

            //
            // Add the ESSCertID attribute
            //
            IDictionary signedAttrs;
            if (signedAttr != null)
                signedAttrs = signedAttr.ToDictionary();
            else
                signedAttrs = Platform.CreateHashtable();

            //try
            //{
            //    byte[] hash = DigestUtilities.CalculateDigest("SHA-1", cert.GetEncoded());

            //    EssCertID essCertid = new EssCertID(hash);

            //    Asn1.Cms.Attribute attr = new Asn1.Cms.Attribute(
            //        PkcsObjectIdentifiers.IdAASigningCertificate,
            //        new DerSet(new SigningCertificate(essCertid)));

            //    signedAttrs[attr.AttrType] = attr;
            //}
            //catch (CertificateEncodingException e)
            //{
            //    throw new TspException("Exception processing certificate.", e);
            //}
            //catch (SecurityUtilityException e)
            //{
            //    throw new TspException("Can't find a SHA-1 implementation.", e);
            //}


            var digestName    = CmsSignedHelper.Instance.GetDigestAlgName(digestOID);
            var signatureName = digestName + "with" + CmsSignedHelper.Instance.GetEncryptionAlgName(CmsSignedHelper.Instance.GetEncOid(key, digestOID));

            var sigfact = new Asn1SignatureFactory(signatureName, key);
            return new SignerInfoGeneratorBuilder()
                .WithSignedAttributeGenerator(
                    new DefaultSignedAttributeTableGenerator(
                        new AttributeTable(signedAttrs)))
                .WithUnsignedAttributeGenerator(
                    new SimpleAttributeTableGenerator(unsignedAttr))
                .Build(sigfact, cert);
        }


        public void SetCertificates(
            IX509Store certificates)
        {
            this.x509Certs = certificates;
        }

        public void SetCrls(
            IX509Store crls)
        {
            this.x509Crls = crls;
        }

        public void SetAccuracySeconds(
            int accuracySeconds)
        {
            this.accuracySeconds = accuracySeconds;
        }

        public void SetAccuracyMillis(
            int accuracyMillis)
        {
            this.accuracyMillis = accuracyMillis;
        }

        public void SetAccuracyMicros(
            int accuracyMicros)
        {
            this.accuracyMicros = accuracyMicros;
        }

        public void SetOrdering(
            bool ordering)
        {
            this.ordering = ordering;
        }

        public void SetTsa(
            GeneralName tsa)
        {
            this.tsa = tsa;
        }

        //------------------------------------------------------------------------------

        public TimeStampToken Generate(
            TimeStampRequest request,
            BigInteger serialNumber,
            DateTime genTime)
        {
            return this.Generate(request, serialNumber, genTime, null);
        }


        public TimeStampToken Generate(
            TimeStampRequest request,
            BigInteger serialNumber,
            DateTime genTime, X509Extensions additionalExtensions)
        {
            var digestAlgOID = new DerObjectIdentifier(request.MessageImprintAlgOid);

            var algID          = new AlgorithmIdentifier(digestAlgOID, DerNull.Instance);
            var messageImprint = new MessageImprint(algID, request.GetMessageImprintDigest());

            Accuracy accuracy = null;
            if (this.accuracySeconds > 0 || this.accuracyMillis > 0 || this.accuracyMicros > 0)
            {
                DerInteger seconds                    = null;
                if (this.accuracySeconds > 0) seconds = new DerInteger(this.accuracySeconds);

                DerInteger millis                   = null;
                if (this.accuracyMillis > 0) millis = new DerInteger(this.accuracyMillis);

                DerInteger micros                   = null;
                if (this.accuracyMicros > 0) micros = new DerInteger(this.accuracyMicros);

                accuracy = new Accuracy(seconds, millis, micros);
            }

            DerBoolean derOrdering         = null;
            if (this.ordering) derOrdering = DerBoolean.GetInstance(this.ordering);

            DerInteger nonce                 = null;
            if (request.Nonce != null) nonce = new DerInteger(request.Nonce);

            var tsaPolicy                            = this.tsaPolicyOID;
            if (request.ReqPolicy != null) tsaPolicy = new DerObjectIdentifier(request.ReqPolicy);

            if (tsaPolicy == null) throw new TspValidationException("request contains no policy", PkiFailureInfo.UnacceptedPolicy);

            var respExtensions = request.Extensions;
            if (additionalExtensions != null)
            {
                var extGen = new X509ExtensionsGenerator();

                if (respExtensions != null)
                    foreach (var oid in respExtensions.ExtensionOids)
                    {
                        var id = DerObjectIdentifier.GetInstance(oid);
                        extGen.AddExtension(id, respExtensions.GetExtension(DerObjectIdentifier.GetInstance(id)));
                    }

                foreach (var oid in additionalExtensions.ExtensionOids)
                {
                    var id = DerObjectIdentifier.GetInstance(oid);
                    extGen.AddExtension(id, additionalExtensions.GetExtension(DerObjectIdentifier.GetInstance(id)));
                }

                respExtensions = extGen.Generate();
            }


            DerGeneralizedTime generalizedTime;
            if (this.Resolution != Resolution.R_SECONDS)
                generalizedTime = new DerGeneralizedTime(this.createGeneralizedTime(genTime));
            else
                generalizedTime = new DerGeneralizedTime(genTime);


            var tstInfo = new TstInfo(tsaPolicy, messageImprint,
                new DerInteger(serialNumber), generalizedTime, accuracy,
                derOrdering, nonce, this.tsa, respExtensions);

            try
            {
                var signedDataGenerator = new CmsSignedDataGenerator();

                var derEncodedTstInfo = tstInfo.GetDerEncoded();

                if (request.CertReq) signedDataGenerator.AddCertificates(this.x509Certs);

                signedDataGenerator.AddCrls(this.x509Crls);

                signedDataGenerator.AddSignerInfoGenerator(this.signerInfoGenerator);

                var signedData = signedDataGenerator.Generate(
                    PkcsObjectIdentifiers.IdCTTstInfo.Id,
                    new CmsProcessableByteArray(derEncodedTstInfo),
                    true);

                return new TimeStampToken(signedData);
            }
            catch (CmsException cmsEx)
            {
                throw new TspException("Error generating time-stamp token", cmsEx);
            }
            catch (IOException e)
            {
                throw new TspException("Exception encoding info", e);
            }
            catch (X509StoreException e)
            {
                throw new TspException("Exception handling CertStore", e);
            }
            //			catch (InvalidAlgorithmParameterException e)
            //			{
            //				throw new TspException("Exception handling CertStore CRLs", e);
            //			}
        }

        private string createGeneralizedTime(DateTime genTime)
        {
            var format = "yyyyMMddHHmmss.fff";

            var sBuild   = new StringBuilder(genTime.ToString(format));
            var dotIndex = sBuild.ToString().IndexOf(".");

            if (dotIndex < 0)
            {
                sBuild.Append("Z");
                return sBuild.ToString();
            }

            switch (this.Resolution)
            {
                case Resolution.R_TENTHS_OF_SECONDS:
                    if (sBuild.Length > dotIndex + 2) sBuild.Remove(dotIndex + 2, sBuild.Length - (dotIndex + 2));
                    break;
                case Resolution.R_HUNDREDTHS_OF_SECONDS:
                    if (sBuild.Length > dotIndex + 3) sBuild.Remove(dotIndex + 3, sBuild.Length - (dotIndex + 3));
                    break;


                case Resolution.R_SECONDS:
                case Resolution.R_MILLISECONDS:
                    // do nothing.
                    break;
            }


            while (sBuild[sBuild.Length - 1] == '0') sBuild.Remove(sBuild.Length - 1, 1);

            if (sBuild.Length - 1 == dotIndex) sBuild.Remove(sBuild.Length - 1, 1);

            sBuild.Append("Z");
            return sBuild.ToString();
        }

        private class TableGen : CmsAttributeTableGenerator
        {
            private readonly SignerInfoGenerator infoGen;
            private readonly EssCertID           essCertID;


            public TableGen(SignerInfoGenerator infoGen, EssCertID essCertID)
            {
                this.infoGen   = infoGen;
                this.essCertID = essCertID;
            }

            public AttributeTable GetAttributes(IDictionary parameters)
            {
                var tab = this.infoGen.signedGen.GetAttributes(parameters);
                if (tab[PkcsObjectIdentifiers.IdAASigningCertificate] == null) return tab.Add(PkcsObjectIdentifiers.IdAASigningCertificate, new SigningCertificate(this.essCertID));
                return tab;
            }
        }

        private class TableGen2 : CmsAttributeTableGenerator
        {
            private readonly SignerInfoGenerator infoGen;
            private readonly EssCertIDv2         essCertID;


            public TableGen2(SignerInfoGenerator infoGen, EssCertIDv2 essCertID)
            {
                this.infoGen   = infoGen;
                this.essCertID = essCertID;
            }

            public AttributeTable GetAttributes(IDictionary parameters)
            {
                var tab = this.infoGen.signedGen.GetAttributes(parameters);
                if (tab[PkcsObjectIdentifiers.IdAASigningCertificateV2] == null) return tab.Add(PkcsObjectIdentifiers.IdAASigningCertificateV2, new SigningCertificateV2(this.essCertID));
                return tab;
            }
        }
    }
}
#pragma warning restore
#endif
