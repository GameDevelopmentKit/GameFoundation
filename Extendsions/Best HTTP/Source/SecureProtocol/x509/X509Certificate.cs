#if !BESTHTTP_DISABLE_ALTERNATE_SSL && (!UNITY_WEBGL || UNITY_EDITOR)
#pragma warning disable
namespace BestHTTP.SecureProtocol.Org.BouncyCastle.X509
{
    using System;
    using System.Collections;
    using System.IO;
    using System.Text;
    using BestHTTP.SecureProtocol.Org.BouncyCastle.Asn1;
    using BestHTTP.SecureProtocol.Org.BouncyCastle.Asn1.Misc;
    using BestHTTP.SecureProtocol.Org.BouncyCastle.Asn1.Utilities;
    using BestHTTP.SecureProtocol.Org.BouncyCastle.Asn1.X509;
    using BestHTTP.SecureProtocol.Org.BouncyCastle.Crypto;
    using BestHTTP.SecureProtocol.Org.BouncyCastle.Crypto.Operators;
    using BestHTTP.SecureProtocol.Org.BouncyCastle.Math;
    using BestHTTP.SecureProtocol.Org.BouncyCastle.Security;
    using BestHTTP.SecureProtocol.Org.BouncyCastle.Security.Certificates;
    using BestHTTP.SecureProtocol.Org.BouncyCastle.Utilities;
    using BestHTTP.SecureProtocol.Org.BouncyCastle.Utilities.Encoders;
    using BestHTTP.SecureProtocol.Org.BouncyCastle.X509.Extension;

    /// <summary>
    /// An Object representing an X509 Certificate.
    /// Has static methods for loading Certificates encoded in many forms that return X509Certificate Objects.
    /// </summary>
    public class X509Certificate
        : X509ExtensionBase
        //		, PKCS12BagAttributeCarrier
    {
        private class CachedEncoding
        {
            private readonly CertificateEncodingException exception;

            internal CachedEncoding(byte[] encoding, CertificateEncodingException exception)
            {
                this.Encoding  = encoding;
                this.exception = exception;
            }

            internal byte[] Encoding { get; }

            internal byte[] GetEncoded()
            {
                if (null != this.exception)
                    throw this.exception;

                if (null == this.Encoding)
                    throw new CertificateEncodingException();

                return this.Encoding;
            }
        }

        private readonly X509CertificateStructure c;
        //private Hashtable pkcs12Attributes = BestHTTP.SecureProtocol.Org.BouncyCastle.Utilities.Platform.CreateHashtable();
        //private ArrayList pkcs12Ordering = BestHTTP.SecureProtocol.Org.BouncyCastle.Utilities.Platform.CreateArrayList();
        private readonly string           sigAlgName;
        private readonly byte[]           sigAlgParams;
        private readonly BasicConstraints basicConstraints;
        private readonly bool[]           keyUsage;

        private readonly object                 cacheLock = new object();
        private          AsymmetricKeyParameter publicKeyValue;
        private          CachedEncoding         cachedEncoding;

        private volatile bool hashValueSet;
        private volatile int  hashValue;

        protected X509Certificate() { }

        public X509Certificate(byte[] certData)
            : this(X509CertificateStructure.GetInstance(certData))
        {
        }

        public X509Certificate(X509CertificateStructure c)
        {
            this.c = c;

            try
            {
                this.sigAlgName = X509SignatureUtilities.GetSignatureName(c.SignatureAlgorithm);

                Asn1Encodable parameters = c.SignatureAlgorithm.Parameters;
                this.sigAlgParams = (null == parameters) ? null : parameters.GetEncoded(Asn1Encodable.Der);
            }
            catch (Exception e)
            {
                throw new CertificateParsingException("Certificate contents invalid: " + e);
            }

            try
            {
                var str = this.GetExtensionValue(new DerObjectIdentifier("2.5.29.19"));

                if (str != null)
                    this.basicConstraints = BasicConstraints.GetInstance(
                        X509ExtensionUtilities.FromExtensionValue(str));
            }
            catch (Exception e)
            {
                throw new CertificateParsingException("cannot construct BasicConstraints: " + e);
            }

            try
            {
                var str = this.GetExtensionValue(new DerObjectIdentifier("2.5.29.15"));

                if (str != null)
                {
                    var bits = DerBitString.GetInstance(
                        X509ExtensionUtilities.FromExtensionValue(str));

                    var bytes  = bits.GetBytes();
                    var length = bytes.Length * 8 - bits.PadBits;

                    this.keyUsage = new bool[length < 9 ? 9 : length];

                    for (var i = 0; i != length; i++) this.keyUsage[i] = (bytes[i / 8] & (0x80 >> (i % 8))) != 0;
                }
                else
                {
                    this.keyUsage = null;
                }
            }
            catch (Exception e)
            {
                throw new CertificateParsingException("cannot construct KeyUsage: " + e);
            }
        }

        //		internal X509Certificate(
        //			Asn1Sequence seq)
        //        {
        //            this.c = X509CertificateStructure.GetInstance(seq);
        //        }

        //		/// <summary>
        //        /// Load certificate from byte array.
        //        /// </summary>
        //        /// <param name="encoded">Byte array containing encoded X509Certificate.</param>
        //        public X509Certificate(
        //            byte[] encoded)
        //			: this((Asn1Sequence) new Asn1InputStream(encoded).ReadObject())
        //		{
        //        }
        //
        //        /// <summary>
        //        /// Load certificate from Stream.
        //        /// Must be positioned at start of certificate.
        //        /// </summary>
        //        /// <param name="input"></param>
        //        public X509Certificate(
        //            Stream input)
        //			: this((Asn1Sequence) new Asn1InputStream(input).ReadObject())
        //        {
        //        }

        public virtual X509CertificateStructure CertificateStructure => this.c;

        /// <summary>
        /// Return true if the current time is within the start and end times nominated on the certificate.
        /// </summary>
        /// <returns>true id certificate is valid for the current time.</returns>
        public virtual bool IsValidNow => this.IsValid(DateTime.UtcNow);

        /// <summary>
        /// Return true if the nominated time is within the start and end times nominated on the certificate.
        /// </summary>
        /// <param name="time">The time to test validity against.</param>
        /// <returns>True if certificate is valid for nominated time.</returns>
        public virtual bool IsValid(
            DateTime time)
        {
            return time.CompareTo(NotBefore) >= 0 && time.CompareTo(NotAfter) <= 0;
        }

        /// <summary>
        ///     Checks if the current date is within certificate's validity period.
        /// </summary>
        public virtual void CheckValidity() { this.CheckValidity(DateTime.UtcNow); }

        /// <summary>
        ///     Checks if the given date is within certificate's validity period.
        /// </summary>
        /// <exception cref="CertificateExpiredException">if the certificate is expired by given date</exception>
        /// <exception cref="CertificateNotYetValidException">if the certificate is not yet valid on given date</exception>
        public virtual void CheckValidity(
            DateTime time)
        {
            if (time.CompareTo(this.NotAfter) > 0)
                throw new CertificateExpiredException("certificate expired on " + this.c.EndDate.GetTime());
            if (time.CompareTo(this.NotBefore) < 0)
                throw new CertificateNotYetValidException("certificate not valid until " + this.c.StartDate.GetTime());
        }

        /// <summary>
        /// Return the certificate's version.
        /// </summary>
        /// <returns>An integer whose value Equals the version of the cerficate.</returns>
        public virtual int Version
        {
            get { return c.Version; }
        }

        /// <summary>
        /// Return a <see cref="BestHTTP.SecureProtocol.Org.BouncyCastle.Math.BigInteger">BigInteger</see> containing the serial number.
        /// </summary>
        /// <returns>The Serial number.</returns>
        public virtual BigInteger SerialNumber
        {
            get { return c.SerialNumber.Value; }
        }

        /// <summary>
        /// Get the Issuer Distinguished Name. (Who signed the certificate.)
        /// </summary>
        /// <returns>And X509Object containing name and value pairs.</returns>
        //        public IPrincipal IssuerDN
        public virtual X509Name IssuerDN => c.Issuer;

        /// <summary>
        /// Get the subject of this certificate.
        /// </summary>
        /// <returns>An X509Name object containing name and value pairs.</returns>
        //        public IPrincipal SubjectDN
        public virtual X509Name SubjectDN => c.Subject;

        /// <summary>
        ///     The time that this certificate is valid from.
        /// </summary>
        /// <returns>A DateTime object representing that time in the local time zone.</returns>
        public virtual DateTime NotBefore => this.c.StartDate.ToDateTime();

        /// <summary>
        /// The time that this certificate is valid up to.
        /// </summary>
        /// <returns>A DateTime object representing that time in the local time zone.</returns>
        public virtual DateTime NotAfter => this.c.EndDate.ToDateTime();

        /// <summary>
        ///     Return the Der encoded TbsCertificate data.
        ///     This is the certificate component less the signature.
        ///     To Get the whole certificate call the GetEncoded() member.
        /// </summary>
        /// <returns>A byte array containing the Der encoded Certificate component.</returns>
        public virtual byte[] GetTbsCertificate() { return this.c.TbsCertificate.GetDerEncoded(); }

        /// <summary>
        ///     The signature.
        /// </summary>
        /// <returns>A byte array containg the signature of the certificate.</returns>
        public virtual byte[] GetSignature() { return this.c.GetSignatureOctets(); }

        /// <summary>
		/// A meaningful version of the Signature Algorithm. (EG SHA1WITHRSA)
		/// </summary>
		/// <returns>A sting representing the signature algorithm.</returns>
		public virtual string SigAlgName => sigAlgName;

        /// <summary>
        ///     Get the Signature Algorithms Object ID.
        /// </summary>
        /// <returns>A string containg a '.' separated object id.</returns>
        public virtual string SigAlgOid => c.SignatureAlgorithm.Algorithm.Id;

        /// <summary>
        ///     Get the signature algorithms parameters. (EG DSA Parameters)
        /// </summary>
        /// <returns>A byte array containing the Der encoded version of the parameters or null if there are none.</returns>
        public virtual byte[] GetSigAlgParams()
        {
            return Arrays.Clone(sigAlgParams);
        }

        /// <summary>
        ///     Get the issuers UID.
        /// </summary>
        /// <returns>A DerBitString.</returns>
        public virtual DerBitString IssuerUniqueID => this.c.TbsCertificate.IssuerUniqueID;

        /// <summary>
        ///     Get the subjects UID.
        /// </summary>
        /// <returns>A DerBitString.</returns>
        public virtual DerBitString SubjectUniqueID => this.c.TbsCertificate.SubjectUniqueID;

        /// <summary>
        ///     Get a key usage guidlines.
        /// </summary>
        public virtual bool[] GetKeyUsage()
        {
            return Arrays.Clone(keyUsage);
        }

        // TODO Replace with something that returns a list of DerObjectIdentifier
        public virtual IList GetExtendedKeyUsage()
        {
            var str = this.GetExtensionValue(new DerObjectIdentifier("2.5.29.37"));

            if (str == null)
                return null;

            try
            {
                var seq = Asn1Sequence.GetInstance(
                    X509ExtensionUtilities.FromExtensionValue(str));

                IList list = Platform.CreateArrayList();

                foreach (DerObjectIdentifier oid in seq) list.Add(oid.Id);

                return list;
            }
            catch (Exception e)
            {
                throw new CertificateParsingException("error processing extended key usage extension", e);
            }
        }

        public virtual int GetBasicConstraints()
        {
            if (this.basicConstraints != null && this.basicConstraints.IsCA())
            {
                if (this.basicConstraints.PathLenConstraint == null) return int.MaxValue;

                return this.basicConstraints.PathLenConstraint.IntValue;
            }

            return -1;
        }

        public virtual ICollection GetSubjectAlternativeNames() { return this.GetAlternativeNames("2.5.29.17"); }

        public virtual ICollection GetIssuerAlternativeNames() { return this.GetAlternativeNames("2.5.29.18"); }

        protected virtual ICollection GetAlternativeNames(
            string oid)
        {
            var altNames = this.GetExtensionValue(new DerObjectIdentifier(oid));

            if (altNames == null)
                return null;

            var asn1Object = X509ExtensionUtilities.FromExtensionValue(altNames);

            var gns = GeneralNames.GetInstance(asn1Object);

            IList result = Platform.CreateArrayList();
            foreach (var gn in gns.GetNames())
            {
                IList entry = Platform.CreateArrayList();
                entry.Add(gn.TagNo);
                entry.Add(gn.Name.ToString());
                result.Add(entry);
            }

            return result;
        }

        protected override X509Extensions GetX509Extensions()
        {
            return this.c.Version >= 3
                ? this.c.TbsCertificate.Extensions
                : null;
        }

        /// <summary>
        ///     Get the public key of the subject of the certificate.
        /// </summary>
        /// <returns>The public key parameters.</returns>
        public virtual AsymmetricKeyParameter GetPublicKey()
        {
            // Cache the public key to support repeated-use optimizations
            lock (cacheLock)
            {
                if (null != publicKeyValue)
                    return publicKeyValue;
            }

            var temp = PublicKeyFactory.CreateKey(this.c.SubjectPublicKeyInfo);

            lock (cacheLock)
            {
                if (null == publicKeyValue)
                {
                    publicKeyValue = temp;
                }

                return publicKeyValue;
            }
        }

        /// <summary>
        ///     Return the DER encoding of this certificate.
        /// </summary>
        /// <returns>A byte array containing the DER encoding of this certificate.</returns>
        /// <exception cref="CertificateEncodingException">If there is an error encoding the certificate.</exception>
        public virtual byte[] GetEncoded() { return Arrays.Clone(this.GetCachedEncoding().GetEncoded()); }

        public override bool Equals(object other)
        {
            if (this == other)
                return true;

            var that = other as X509Certificate;
            if (null == that)
                return false;

            if (this.hashValueSet && that.hashValueSet)
            {
                if (this.hashValue != that.hashValue)
                    return false;
            }
            else if (null == this.cachedEncoding || null == that.cachedEncoding)
            {
                var signature = this.c.Signature;
                if (null != signature && !signature.Equals(that.c.Signature))
                    return false;
            }

            var thisEncoding = this.GetCachedEncoding().Encoding;
            var thatEncoding = that.GetCachedEncoding().Encoding;

            return null != thisEncoding
                   && null != thatEncoding
                   && Arrays.AreEqual(thisEncoding, thatEncoding);
        }

        public override int GetHashCode()
        {
            if (!this.hashValueSet)
            {
                var thisEncoding = this.GetCachedEncoding().Encoding;

                this.hashValue    = Arrays.GetHashCode(thisEncoding);
                this.hashValueSet = true;
            }

            return hashValue;
        }

        //		public void setBagAttribute(
        //			DERObjectIdentifier oid,
        //			DEREncodable        attribute)
        //		{
        //			pkcs12Attributes.put(oid, attribute);
        //			pkcs12Ordering.addElement(oid);
        //		}
        //
        //		public DEREncodable getBagAttribute(
        //			DERObjectIdentifier oid)
        //		{
        //			return (DEREncodable)pkcs12Attributes.get(oid);
        //		}
        //
        //		public Enumeration getBagAttributeKeys()
        //		{
        //			return pkcs12Ordering.elements();
        //		}

        public override string ToString()
        {
            var buf = new StringBuilder();
            var nl  = Platform.NewLine;

            buf.Append("  [0]         Version: ").Append(this.Version).Append(nl);
            buf.Append("         SerialNumber: ").Append(this.SerialNumber).Append(nl);
            buf.Append("             IssuerDN: ").Append(this.IssuerDN).Append(nl);
            buf.Append("           Start Date: ").Append(this.NotBefore).Append(nl);
            buf.Append("           Final Date: ").Append(this.NotAfter).Append(nl);
            buf.Append("            SubjectDN: ").Append(this.SubjectDN).Append(nl);
            buf.Append("           Public Key: ").Append(this.GetPublicKey()).Append(nl);
            buf.Append("  Signature Algorithm: ").Append(this.SigAlgName).Append(nl);

            var sig = this.GetSignature();
            buf.Append("            Signature: ").Append(Hex.ToHexString(sig, 0, 20)).Append(nl);

            for (var i = 20; i < sig.Length; i += 20)
            {
                var len = Math.Min(20, sig.Length - i);
                buf.Append("                       ").Append(Hex.ToHexString(sig, i, len)).Append(nl);
            }

            var extensions = this.c.TbsCertificate.Extensions;

            if (extensions != null)
            {
                var e = extensions.ExtensionOids.GetEnumerator();

                if (e.MoveNext()) buf.Append("       Extensions: \n");

                do
                {
                    var oid = (DerObjectIdentifier)e.Current;
                    var ext = extensions.GetExtension(oid);

                    if (ext.Value != null)
                    {
                        Asn1Object obj = X509ExtensionUtilities.FromExtensionValue(ext.Value);

                        buf.Append("                       critical(").Append(ext.IsCritical).Append(") ");
                        try
                        {
                            if (oid.Equals(X509Extensions.BasicConstraints))
                            {
                                buf.Append(BasicConstraints.GetInstance(obj));
                            }
                            else if (oid.Equals(X509Extensions.KeyUsage))
                            {
                                buf.Append(KeyUsage.GetInstance(obj));
                            }
                            else if (oid.Equals(MiscObjectIdentifiers.NetscapeCertType))
                            {
                                buf.Append(new NetscapeCertType((DerBitString)obj));
                            }
                            else if (oid.Equals(MiscObjectIdentifiers.NetscapeRevocationUrl))
                            {
                                buf.Append(new NetscapeRevocationUrl((DerIA5String)obj));
                            }
                            else if (oid.Equals(MiscObjectIdentifiers.VerisignCzagExtension))
                            {
                                buf.Append(new VerisignCzagExtension((DerIA5String)obj));
                            }
                            else
                            {
                                buf.Append(oid.Id);
                                buf.Append(" value = ").Append(Asn1Dump.DumpAsString(obj));
                                //buf.Append(" value = ").Append("*****").Append(nl);
                            }
                        }
                        catch (Exception)
                        {
                            buf.Append(oid.Id);
                            //buf.Append(" value = ").Append(new string(Hex.encode(ext.getValue().getOctets()))).Append(nl);
                            buf.Append(" value = ").Append("*****");
                        }
                    }

                    buf.Append(nl);
                } while (e.MoveNext());
            }

            return buf.ToString();
        }

        /// <summary>
        ///     Verify the certificate's signature using the nominated public key.
        /// </summary>
        /// <param name="key">An appropriate public key parameter object, RsaPublicKeyParameters, DsaPublicKeyParameters or ECDsaPublicKeyParameters</param>
        /// <returns>True if the signature is valid.</returns>
        /// <exception cref="Exception">If key submitted is not of the above nominated types.</exception>
        public virtual void Verify(
            AsymmetricKeyParameter key)
        {
            this.CheckSignature(new Asn1VerifierFactory(this.c.SignatureAlgorithm, key));
        }

        /// <summary>
        /// Verify the certificate's signature using a verifier created using the passed in verifier provider.
        /// </summary>
        /// <param name="verifierProvider">An appropriate provider for verifying the certificate's signature.</param>
        /// <returns>True if the signature is valid.</returns>
        /// <exception cref="Exception">If verifier provider is not appropriate or the certificate algorithm is invalid.</exception>
        public virtual void Verify(
            IVerifierFactoryProvider verifierProvider)
        {
            this.CheckSignature(verifierProvider.CreateVerifierFactory(this.c.SignatureAlgorithm));
        }

        protected virtual void CheckSignature(
            IVerifierFactory verifier)
        {
            if (!IsAlgIDEqual(this.c.SignatureAlgorithm, this.c.TbsCertificate.Signature))
                throw new CertificateException("signature algorithm in TBS cert not same as outer cert");

            var parameters = this.c.SignatureAlgorithm.Parameters;

            IStreamCalculator streamCalculator = verifier.CreateCalculator();

            var b = this.GetTbsCertificate();

            streamCalculator.Stream.Write(b, 0, b.Length);

            Platform.Dispose(streamCalculator.Stream);

            if (!((IVerifier)streamCalculator.GetResult()).IsVerified(this.GetSignature())) throw new InvalidKeyException("Public key presented not for certificate signature");
        }

        private CachedEncoding GetCachedEncoding()
        {
            lock (this.cacheLock)
            {
                if (null != this.cachedEncoding)
                    return this.cachedEncoding;
            }

            byte[]                       encoding  = null;
            CertificateEncodingException exception = null;
            try
            {
                encoding = this.c.GetEncoded(Asn1Encodable.Der);
            }
            catch (IOException e)
            {
                exception = new CertificateEncodingException("Failed to DER-encode certificate", e);
            }

            var temp = new CachedEncoding(encoding, exception);

            lock (this.cacheLock)
            {
                if (null == this.cachedEncoding) this.cachedEncoding = temp;

                return this.cachedEncoding;
            }
        }

        private static bool IsAlgIDEqual(AlgorithmIdentifier id1, AlgorithmIdentifier id2)
        {
            if (!id1.Algorithm.Equals(id2.Algorithm))
                return false;

            var p1 = id1.Parameters;
            var p2 = id2.Parameters;

            if (p1 == null == (p2 == null))
                return Equals(p1, p2);

            // Exactly one of p1, p2 is null at this point
            return p1 == null
                ? p2.ToAsn1Object() is Asn1Null
                : p1.ToAsn1Object() is Asn1Null;
        }
    }
}
#pragma warning restore
#endif
