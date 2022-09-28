#if !BESTHTTP_DISABLE_ALTERNATE_SSL && (!UNITY_WEBGL || UNITY_EDITOR)
#pragma warning disable
namespace BestHTTP.SecureProtocol.Org.BouncyCastle.Tls
{
    using System;
    using System.IO;

    public class TlsException
        : IOException
    {
        public TlsException() { }

        public TlsException(string message)
            : base(message)
        {
        }

        public TlsException(string message, Exception cause)
            : base(message, cause)
        {
        }
    }
}
#pragma warning restore
#endif