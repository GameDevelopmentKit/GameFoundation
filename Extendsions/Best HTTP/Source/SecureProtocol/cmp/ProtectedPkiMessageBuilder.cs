#if !BESTHTTP_DISABLE_ALTERNATE_SSL && (!UNITY_WEBGL || UNITY_EDITOR)
#pragma warning disable
namespace BestHTTP.SecureProtocol.Org.BouncyCastle.Cmp
{
    using System;
    using System.Collections;
    using BestHTTP.SecureProtocol.Org.BouncyCastle.Asn1;
    using BestHTTP.SecureProtocol.Org.BouncyCastle.Asn1.Cmp;
    using BestHTTP.SecureProtocol.Org.BouncyCastle.Asn1.X509;
    using BestHTTP.SecureProtocol.Org.BouncyCastle.Crypto;
    using BestHTTP.SecureProtocol.Org.BouncyCastle.Crypto.Operators;
    using BestHTTP.SecureProtocol.Org.BouncyCastle.Utilities;
    using BestHTTP.SecureProtocol.Org.BouncyCastle.X509;

    public class ProtectedPkiMessageBuilder
    {
        private readonly PkiHeaderBuilder hdrBuilBuilder;
        private          PkiBody          body;
        private readonly IList            generalInfos = Platform.CreateArrayList();
        private readonly IList            extraCerts   = Platform.CreateArrayList();

        public ProtectedPkiMessageBuilder(GeneralName sender, GeneralName recipient)
            : this(PkiHeader.CMP_2000, sender, recipient)
        {
        }

        public ProtectedPkiMessageBuilder(int pvno, GeneralName sender, GeneralName recipient) { this.hdrBuilBuilder = new PkiHeaderBuilder(pvno, sender, recipient); }

        public ProtectedPkiMessageBuilder SetTransactionId(byte[] tid)
        {
            this.hdrBuilBuilder.SetTransactionID(tid);
            return this;
        }

        public ProtectedPkiMessageBuilder SetFreeText(PkiFreeText freeText)
        {
            this.hdrBuilBuilder.SetFreeText(freeText);
            return this;
        }

        public ProtectedPkiMessageBuilder AddGeneralInfo(InfoTypeAndValue genInfo)
        {
            this.generalInfos.Add(genInfo);
            return this;
        }

        public ProtectedPkiMessageBuilder SetMessageTime(DerGeneralizedTime generalizedTime)
        {
            this.hdrBuilBuilder.SetMessageTime(generalizedTime);
            return this;
        }

        public ProtectedPkiMessageBuilder SetRecipKID(byte[] id)
        {
            this.hdrBuilBuilder.SetRecipKID(id);
            return this;
        }

        public ProtectedPkiMessageBuilder SetRecipNonce(byte[] nonce)
        {
            this.hdrBuilBuilder.SetRecipNonce(nonce);
            return this;
        }

        public ProtectedPkiMessageBuilder SetSenderKID(byte[] id)
        {
            this.hdrBuilBuilder.SetSenderKID(id);
            return this;
        }

        public ProtectedPkiMessageBuilder SetSenderNonce(byte[] nonce)
        {
            this.hdrBuilBuilder.SetSenderNonce(nonce);
            return this;
        }

        public ProtectedPkiMessageBuilder SetBody(PkiBody body)
        {
            this.body = body;
            return this;
        }

        public ProtectedPkiMessageBuilder AddCmpCertificate(X509Certificate certificate)
        {
            this.extraCerts.Add(certificate);
            return this;
        }

        public ProtectedPkiMessage Build(ISignatureFactory signatureFactory)
        {
            if (null == this.body)
                throw new InvalidOperationException("body must be set before building");

            var calculator = signatureFactory.CreateCalculator();

            if (!(signatureFactory.AlgorithmDetails is AlgorithmIdentifier)) throw new ArgumentException("AlgorithmDetails is not AlgorithmIdentifier");

            this.FinalizeHeader((AlgorithmIdentifier)signatureFactory.AlgorithmDetails);
            var header     = this.hdrBuilBuilder.Build();
            var protection = new DerBitString(this.CalculateSignature(calculator, header, this.body));
            return this.FinalizeMessage(header, protection);
        }

        public ProtectedPkiMessage Build(IMacFactory factory)
        {
            if (null == this.body)
                throw new InvalidOperationException("body must be set before building");

            var calculator = factory.CreateCalculator();
            this.FinalizeHeader((AlgorithmIdentifier)factory.AlgorithmDetails);
            var header     = this.hdrBuilBuilder.Build();
            var protection = new DerBitString(this.CalculateSignature(calculator, header, this.body));
            return this.FinalizeMessage(header, protection);
        }

        private void FinalizeHeader(AlgorithmIdentifier algorithmIdentifier)
        {
            this.hdrBuilBuilder.SetProtectionAlg(algorithmIdentifier);
            if (this.generalInfos.Count > 0)
            {
                var genInfos                                          = new InfoTypeAndValue[this.generalInfos.Count];
                for (var t = 0; t < genInfos.Length; t++) genInfos[t] = (InfoTypeAndValue)this.generalInfos[t];

                this.hdrBuilBuilder.SetGeneralInfo(genInfos);
            }
        }

        private ProtectedPkiMessage FinalizeMessage(PkiHeader header, DerBitString protection)
        {
            if (this.extraCerts.Count > 0)
            {
                var cmpCertificates = new CmpCertificate[this.extraCerts.Count];
                for (var i = 0; i < cmpCertificates.Length; i++)
                {
                    var cert = ((X509Certificate)this.extraCerts[i]).GetEncoded();
                    cmpCertificates[i] = CmpCertificate.GetInstance(Asn1Object.FromByteArray(cert));
                }

                return new ProtectedPkiMessage(new PkiMessage(header, this.body, protection, cmpCertificates));
            }

            return new ProtectedPkiMessage(new PkiMessage(header, this.body, protection));
        }

        private byte[] CalculateSignature(IStreamCalculator signer, PkiHeader header, PkiBody body)
        {
            var avec = new Asn1EncodableVector();
            avec.Add(header);
            avec.Add(body);
            var encoded = new DerSequence(avec).GetEncoded();
            signer.Stream.Write(encoded, 0, encoded.Length);
            var result = signer.GetResult();

            if (result is DefaultSignatureResult)
                return ((DefaultSignatureResult)result).Collect();
            if (result is IBlockResult)
                return ((IBlockResult)result).Collect();
            if (result is byte[]) return (byte[])result;

            throw new InvalidOperationException("result is not byte[] or DefaultSignatureResult");
        }
    }
}
#pragma warning restore
#endif