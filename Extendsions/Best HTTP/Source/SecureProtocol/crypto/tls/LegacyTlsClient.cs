#if !BESTHTTP_DISABLE_ALTERNATE_SSL && (!UNITY_WEBGL || UNITY_EDITOR)

using System;
using BestHTTP.SecureProtocol.Org.BouncyCastle.Crypto.Tls;

namespace BestHTTP.SecureProtocol.Org.BouncyCastle.Crypto.Tls
{
    /// <summary>
    /// A temporary class to use LegacyTlsAuthentication
    /// </summary>
    public class LegacyTlsClient : DefaultTlsClient
    {
        public readonly Uri TargetUri;
        public readonly ICertificateVerifyer verifyer;
        public readonly IClientCredentialsProvider credProvider;

        public LegacyTlsClient(Uri targetUri, ICertificateVerifyer verifyer, IClientCredentialsProvider prov,
                               System.Collections.Generic.List<string> hostNames,
                               System.Collections.Generic.List<string> clientSupportedProtocols)
            :base()
        {
            this.TargetUri = targetUri;
            this.verifyer = verifyer;
            this.credProvider = prov;
            base.HostNames = hostNames;
            base.ClientSupportedProtocols = clientSupportedProtocols;
        }

        public override TlsAuthentication GetAuthentication()
        {
            return new LegacyTlsAuthentication(this.TargetUri, verifyer, credProvider);
        }
    }
}

#endif
