#if !BESTHTTP_DISABLE_ALTERNATE_SSL && (!UNITY_WEBGL || UNITY_EDITOR)
#pragma warning disable
namespace BestHTTP.SecureProtocol.Org.BouncyCastle.Tls.Crypto
{
    using System;

    /// <summary>Basic exception class for crypto services to pass back a cause.</summary>
    public class TlsCryptoException
        : TlsException
    {
        public TlsCryptoException(string msg)
            : base(msg)
        {
        }

        public TlsCryptoException(string msg, Exception cause)
            : base(msg, cause)
        {
        }
    }
}
#pragma warning restore
#endif