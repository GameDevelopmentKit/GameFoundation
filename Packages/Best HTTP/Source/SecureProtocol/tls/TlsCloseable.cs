#if !BESTHTTP_DISABLE_ALTERNATE_SSL && (!UNITY_WEBGL || UNITY_EDITOR)
#pragma warning disable
namespace BestHTTP.SecureProtocol.Org.BouncyCastle.Tls
{
    using System.IO;

    public interface TlsCloseable
    {
        /// <exception cref="IOException" />
        void Close();
    }
}
#pragma warning restore
#endif