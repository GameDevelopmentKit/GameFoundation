#if !BESTHTTP_DISABLE_ALTERNATE_SSL && (!UNITY_WEBGL || UNITY_EDITOR)
#pragma warning disable
namespace BestHTTP.SecureProtocol.Org.BouncyCastle.Tls.Crypto
{
    using System.IO;

    public interface TlsStreamSigner
    {
        /// <exception cref="IOException" />
        Stream GetOutputStream();

        /// <exception cref="IOException" />
        byte[] GetSignature();
    }
}
#pragma warning restore
#endif