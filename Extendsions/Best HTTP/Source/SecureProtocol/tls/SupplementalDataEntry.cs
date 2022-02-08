#if !BESTHTTP_DISABLE_ALTERNATE_SSL && (!UNITY_WEBGL || UNITY_EDITOR)
#pragma warning disable
namespace BestHTTP.SecureProtocol.Org.BouncyCastle.Tls
{
    public sealed class SupplementalDataEntry
    {
        public SupplementalDataEntry(int dataType, byte[] data)
        {
            this.DataType = dataType;
            this.Data     = data;
        }

        public int DataType { get; }

        public byte[] Data { get; }
    }
}
#pragma warning restore
#endif