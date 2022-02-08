#if !BESTHTTP_DISABLE_ALTERNATE_SSL && (!UNITY_WEBGL || UNITY_EDITOR)
#pragma warning disable
namespace BestHTTP.SecureProtocol.Org.BouncyCastle.Tls
{
    using System.IO;

    public interface DatagramReceiver
    {
        /// <exception cref="IOException" />
        int GetReceiveLimit();

        /// <exception cref="IOException" />
        int Receive(byte[] buf, int off, int len, int waitMillis);
    }
}
#pragma warning restore
#endif