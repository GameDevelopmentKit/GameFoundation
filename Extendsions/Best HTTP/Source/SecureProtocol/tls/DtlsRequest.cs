#if !BESTHTTP_DISABLE_ALTERNATE_SSL && (!UNITY_WEBGL || UNITY_EDITOR)
#pragma warning disable
namespace BestHTTP.SecureProtocol.Org.BouncyCastle.Tls
{
    public sealed class DtlsRequest
    {
        internal DtlsRequest(long recordSeq, byte[] message, ClientHello clientHello)
        {
            this.RecordSeq   = recordSeq;
            this.Message     = message;
            this.ClientHello = clientHello;
        }

        internal ClientHello ClientHello { get; }

        internal byte[] Message { get; }

        internal int MessageSeq => TlsUtilities.ReadUint16(this.Message, 4);

        internal long RecordSeq { get; }
    }
}
#pragma warning restore
#endif