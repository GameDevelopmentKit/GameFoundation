#if !BESTHTTP_DISABLE_ALTERNATE_SSL && (!UNITY_WEBGL || UNITY_EDITOR)
#pragma warning disable
namespace BestHTTP.SecureProtocol.Org.BouncyCastle.Asn1.X509
{
    using System;
    using System.Collections;
    using BestHTTP.SecureProtocol.Org.BouncyCastle.Utilities;

    /// <remarks>Generator for X.509 extensions</remarks>
    public class X509ExtensionsGenerator
    {
        private IDictionary extensions  = Platform.CreateHashtable();
        private IList       extOrdering = Platform.CreateArrayList();

        private static readonly IDictionary dupsAllowed = Platform.CreateHashtable();

        static X509ExtensionsGenerator()
        {
            dupsAllowed.Add(X509Extensions.SubjectAlternativeName, true);
            dupsAllowed.Add(X509Extensions.IssuerAlternativeName, true);
            dupsAllowed.Add(X509Extensions.SubjectDirectoryAttributes, true);
            dupsAllowed.Add(X509Extensions.CertificateIssuer, true);
        }


        /// <summary>Reset the generator</summary>
        public void Reset()
        {
            extensions = Platform.CreateHashtable();
            extOrdering = Platform.CreateArrayList();
        }

        /// <summary>
        ///     Add an extension with the given oid and the passed in value to be included
        ///     in the OCTET STRING associated with the extension.
        /// </summary>
        /// <param name="oid">OID for the extension.</param>
        /// <param name="critical">True if critical, false otherwise.</param>
        /// <param name="extValue">The ASN.1 object to be included in the extension.</param>
        public void AddExtension(
            DerObjectIdentifier oid,
            bool critical,
            Asn1Encodable extValue)
        {
            byte[] encoded;
            try
            {
                encoded = extValue.GetDerEncoded();
            }
            catch (Exception e)
            {
                throw new ArgumentException("error encoding value: " + e);
            }

            this.AddExtension(oid, critical, encoded);
        }

        /// <summary>
        ///     Add an extension with the given oid and the passed in byte array to be wrapped
        ///     in the OCTET STRING associated with the extension.
        /// </summary>
        /// <param name="oid">OID for the extension.</param>
        /// <param name="critical">True if critical, false otherwise.</param>
        /// <param name="extValue">The byte array to be wrapped.</param>
        public void AddExtension(
            DerObjectIdentifier oid,
            bool critical,
            byte[] extValue)
        {
            if (this.extensions.Contains(oid))
            {
                if (dupsAllowed.Contains(oid))
                {
                    var existingExtension = (X509Extension)this.extensions[oid];

                    var seq1  = Asn1Sequence.GetInstance(Asn1OctetString.GetInstance(existingExtension.Value).GetOctets());
                    var items = Asn1EncodableVector.FromEnumerable(seq1);
                    var seq2  = Asn1Sequence.GetInstance(extValue);

                    foreach (Asn1Encodable enc in seq2) items.Add(enc);

                    this.extensions[oid] = new X509Extension(existingExtension.IsCritical, new DerOctetString(new DerSequence(items).GetEncoded()));
                }
                else
                {
                    throw new ArgumentException("extension " + oid + " already added");
                }
            }
            else
            {
                this.extOrdering.Add(oid);
                this.extensions.Add(oid, new X509Extension(critical, new DerOctetString(extValue)));
            }
        }

        public void AddExtensions(X509Extensions extensions)
        {
            foreach (DerObjectIdentifier ident in extensions.ExtensionOids)
            {
                var ext = extensions.GetExtension(ident);
                this.AddExtension(ident, ext.critical, ext.Value.GetOctets());
            }
        }


        /// <summary>Return true if there are no extension present in this generator.</summary>
        /// <returns>True if empty, false otherwise</returns>
        public bool IsEmpty => this.extOrdering.Count < 1;

        /// <summary>Generate an X509Extensions object based on the current state of the generator.</summary>
        /// <returns>An <c>X509Extensions</c> object</returns>
        public X509Extensions Generate() { return new X509Extensions(this.extOrdering, this.extensions); }

        internal void AddExtension(DerObjectIdentifier oid, X509Extension x509Extension)
        {
            if (this.extensions.Contains(oid)) throw new ArgumentException("extension " + oid + " already added");

            this.extOrdering.Add(oid);
            this.extensions.Add(oid, x509Extension);
        }
    }
}
#pragma warning restore
#endif
