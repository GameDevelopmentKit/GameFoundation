#if !BESTHTTP_DISABLE_ALTERNATE_SSL && (!UNITY_WEBGL || UNITY_EDITOR)
#pragma warning disable
namespace BestHTTP.SecureProtocol.Org.BouncyCastle.Bcpg.Sig
{
    /**
    * packet giving signature creation time.
    */
    public class PreferredAlgorithms
        : SignatureSubpacket
    {
        private static byte[] IntToByteArray(
            int[] v)
        {
            var data = new byte[v.Length];

            for (var i = 0; i != v.Length; i++) data[i] = (byte)v[i];

            return data;
        }

        public PreferredAlgorithms(
            SignatureSubpacketTag type,
            bool critical,
            bool isLongLength,
            byte[] data)
            : base(type, critical, isLongLength, data)
        {
        }

        public PreferredAlgorithms(
            SignatureSubpacketTag type,
            bool critical,
            int[] preferences)
            : base(type, critical, false, IntToByteArray(preferences))
        {
        }

        public int[] GetPreferences()
        {
            var v = new int[this.data.Length];

            for (var i = 0; i != v.Length; i++) v[i] = this.data[i] & 0xff;

            return v;
        }
    }
}
#pragma warning restore
#endif