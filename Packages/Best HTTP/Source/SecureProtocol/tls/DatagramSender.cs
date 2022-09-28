#if !BESTHTTP_DISABLE_ALTERNATE_SSL && (!UNITY_WEBGL || UNITY_EDITOR)
#pragma warning disable
namespace BestHTTP.SecureProtocol.Org.BouncyCastle.Tls
{
    using System.IO;

    public interface DatagramSender
    {
        /// <exception cref="IOException" />
        int GetSendLimit();

        /// <exception cref="IOException" />
        void Send(byte[] buf, int off, int len);
    }
}
#pragma warning restore
#endif