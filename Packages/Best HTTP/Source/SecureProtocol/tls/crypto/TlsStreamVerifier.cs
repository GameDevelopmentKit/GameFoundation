#if !BESTHTTP_DISABLE_ALTERNATE_SSL && (!UNITY_WEBGL || UNITY_EDITOR)
#pragma warning disable
namespace BestHTTP.SecureProtocol.Org.BouncyCastle.Tls.Crypto
{
    using System.IO;

    public interface TlsStreamVerifier
    {
        /// <exception cref="IOException" />
        Stream GetOutputStream();

        /// <exception cref="IOException" />
        bool IsVerified();
    }
}
#pragma warning restore
#endif