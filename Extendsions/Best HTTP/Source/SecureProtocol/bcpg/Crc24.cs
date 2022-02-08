#if !BESTHTTP_DISABLE_ALTERNATE_SSL && (!UNITY_WEBGL || UNITY_EDITOR)
#pragma warning disable
namespace BestHTTP.SecureProtocol.Org.BouncyCastle.Bcpg
{
    public class Crc24
    {
        private const int Crc24Init = 0x0b704ce;
        private const int Crc24Poly = 0x1864cfb;

        public void Update(
            int b)
        {
            this.Value ^= b << 16;
            for (var i = 0; i < 8; i++)
            {
                this.Value <<= 1;
                if ((this.Value & 0x1000000) != 0) this.Value ^= Crc24Poly;
            }
        }


        public int GetValue() { return this.Value; }

        public int Value { get; private set; } = Crc24Init;

        public void Reset() { this.Value = Crc24Init; }
    }
}
#pragma warning restore
#endif