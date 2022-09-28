#if !BESTHTTP_DISABLE_ALTERNATE_SSL && (!UNITY_WEBGL || UNITY_EDITOR)
#pragma warning disable
namespace BestHTTP.SecureProtocol.Org.BouncyCastle.Bcpg
{
    using System;

    public class UnsupportedPacketVersionException
        : Exception
    {
        public UnsupportedPacketVersionException(string msg)
            : base(msg)
        {
        }
    }
}
#pragma warning restore
#endif