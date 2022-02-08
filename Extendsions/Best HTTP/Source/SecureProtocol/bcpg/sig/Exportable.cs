#if !BESTHTTP_DISABLE_ALTERNATE_SSL && (!UNITY_WEBGL || UNITY_EDITOR)
#pragma warning disable
namespace BestHTTP.SecureProtocol.Org.BouncyCastle.Bcpg.Sig
{
    /**
    * packet giving signature creation time.
    */
    public class Exportable
        : SignatureSubpacket
    {
        private static byte[] BooleanToByteArray(bool val)
        {
            var data = new byte[1];

            if (val)
            {
                data[0] = 1;
                return data;
            }

            return data;
        }

        public Exportable(
            bool critical,
            bool isLongLength,
            byte[] data)
            : base(SignatureSubpacketTag.Exportable, critical, isLongLength, data)
        {
        }

        public Exportable(
            bool critical,
            bool isExportable)
            : base(SignatureSubpacketTag.Exportable, critical, false, BooleanToByteArray(isExportable))
        {
        }

        public bool IsExportable() { return this.data[0] != 0; }
    }
}
#pragma warning restore
#endif