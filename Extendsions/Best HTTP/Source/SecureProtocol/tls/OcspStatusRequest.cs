#if !BESTHTTP_DISABLE_ALTERNATE_SSL && (!UNITY_WEBGL || UNITY_EDITOR)
#pragma warning disable
namespace BestHTTP.SecureProtocol.Org.BouncyCastle.Tls
{
    using System.Collections;
    using System.IO;
    using BestHTTP.SecureProtocol.Org.BouncyCastle.Asn1;
    using BestHTTP.SecureProtocol.Org.BouncyCastle.Asn1.Ocsp;
    using BestHTTP.SecureProtocol.Org.BouncyCastle.Asn1.X509;
    using BestHTTP.SecureProtocol.Org.BouncyCastle.Utilities;
    using BestHTTP.SecureProtocol.Org.BouncyCastle.Utilities.IO;

    /// <summary>RFC 3546 3.6</summary>
    public sealed class OcspStatusRequest
    {
        /// <param name="responderIDList">
        ///     an <see cref="IList" /> of <see cref="ResponderID" />, specifying the list of
        ///     trusted OCSP responders. An empty list has the special meaning that the responders are implicitly known to
        ///     the server - e.g., by prior arrangement.
        /// </param>
        /// <param name="requestExtensions">
        ///     OCSP request extensions. A null value means that there are no extensions.
        /// </param>
        public OcspStatusRequest(IList responderIDList, X509Extensions requestExtensions)
        {
            this.ResponderIDList   = responderIDList;
            this.RequestExtensions = requestExtensions;
        }

        /// <returns>an <see cref="IList" /> of <see cref="ResponderID" />.</returns>
        public IList ResponderIDList { get; }

        /// <returns>OCSP request extensions.</returns>
        public X509Extensions RequestExtensions { get; }

        /// <summary>Encode this <see cref="OcspStatusRequest" /> to a <see cref="Stream" />.</summary>
        /// <param name="output">the <see cref="Stream" /> to encode to.</param>
        /// <exception cref="IOException" />
        public void Encode(Stream output)
        {
            if (this.ResponderIDList == null || this.ResponderIDList.Count < 1)
            {
                TlsUtilities.WriteUint16(0, output);
            }
            else
            {
                var buf = new MemoryStream();
                foreach (ResponderID responderID in this.ResponderIDList)
                {
                    var derEncoding = responderID.GetEncoded(Asn1Encodable.Der);
                    TlsUtilities.WriteOpaque16(derEncoding, buf);
                }

                TlsUtilities.CheckUint16(buf.Length);
                TlsUtilities.WriteUint16((int)buf.Length, output);
                Streams.WriteBufTo(buf, output);
            }

            if (this.RequestExtensions == null)
            {
                TlsUtilities.WriteUint16(0, output);
            }
            else
            {
                var derEncoding = this.RequestExtensions.GetEncoded(Asn1Encodable.Der);
                TlsUtilities.CheckUint16(derEncoding.Length);
                TlsUtilities.WriteUint16(derEncoding.Length, output);
                output.Write(derEncoding, 0, derEncoding.Length);
            }
        }

        /// <summary>Parse an <see cref="OcspStatusRequest" /> from a <see cref="Stream" />.</summary>
        /// <param name="input">the <see cref="Stream" /> to parse from.</param>
        /// <returns>an <see cref="OcspStatusRequest" /> object.</returns>
        /// <exception cref="IOException" />
        public static OcspStatusRequest Parse(Stream input)
        {
            var responderIDList = Platform.CreateArrayList();
            {
                var data = TlsUtilities.ReadOpaque16(input);
                if (data.Length > 0)
                {
                    var buf = new MemoryStream(data, false);
                    do
                    {
                        var derEncoding = TlsUtilities.ReadOpaque16(buf, 1);
                        var responderID = ResponderID.GetInstance(TlsUtilities.ReadDerObject(derEncoding));
                        responderIDList.Add(responderID);
                    } while (buf.Position < buf.Length);
                }
            }

            X509Extensions requestExtensions = null;
            {
                var derEncoding                               = TlsUtilities.ReadOpaque16(input);
                if (derEncoding.Length > 0) requestExtensions = X509Extensions.GetInstance(TlsUtilities.ReadDerObject(derEncoding));
            }

            return new OcspStatusRequest(responderIDList, requestExtensions);
        }
    }
}
#pragma warning restore
#endif