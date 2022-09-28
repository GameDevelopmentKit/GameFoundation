#if !BESTHTTP_DISABLE_ALTERNATE_SSL && (!UNITY_WEBGL || UNITY_EDITOR)
#pragma warning disable
namespace BestHTTP.SecureProtocol.Org.BouncyCastle.Tls
{
    public sealed class RecordPreview
    {
        internal static RecordPreview CombineAppData(RecordPreview a, RecordPreview b) { return new RecordPreview(a.RecordSize + b.RecordSize, a.ContentLimit + b.ContentLimit); }

        internal static RecordPreview ExtendRecordSize(RecordPreview a, int recordSize) { return new RecordPreview(a.RecordSize + recordSize, a.ContentLimit); }

        internal RecordPreview(int recordSize, int contentLimit)
        {
            this.RecordSize   = recordSize;
            this.ContentLimit = contentLimit;
        }

        public int ContentLimit { get; }

        public int RecordSize { get; }
    }
}
#pragma warning restore
#endif