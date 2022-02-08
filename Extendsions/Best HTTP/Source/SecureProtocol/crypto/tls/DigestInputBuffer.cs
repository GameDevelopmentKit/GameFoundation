#if !BESTHTTP_DISABLE_ALTERNATE_SSL && (!UNITY_WEBGL || UNITY_EDITOR)
#pragma warning disable
namespace BestHTTP.SecureProtocol.Org.BouncyCastle.Crypto.Tls
{
    using System.IO;
    using BestHTTP.SecureProtocol.Org.BouncyCastle.Crypto.IO;
    using BestHTTP.SecureProtocol.Org.BouncyCastle.Utilities.IO;

    internal class DigestInputBuffer
        :   MemoryStream
    {
        internal void UpdateDigest(IDigest d)
        {
            Streams.WriteBufTo(this, new DigestSink(d));
        }
    }
}
#pragma warning restore
#endif
