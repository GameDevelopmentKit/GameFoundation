#if !BESTHTTP_DISABLE_ALTERNATE_SSL && (!UNITY_WEBGL || UNITY_EDITOR)
#pragma warning disable
namespace BestHTTP.SecureProtocol.Org.BouncyCastle.Crypto.Modes.Gcm
{
    using BestHTTP.SecureProtocol.Org.BouncyCastle.Utilities;

    public class BasicGcmExponentiator
        : IGcmExponentiator
    {
        private ulong[] x;

        public void Init(byte[] x)
        {
            this.x = GcmUtilities.AsUlongs(x);
        }

        public void ExponentiateX(long pow, byte[] output)
        {
            // Initial value is little-endian 1
            var y = GcmUtilities.OneAsUlongs();

            if (pow > 0)
            {
                var powX = Arrays.Clone(this.x);
                do
                {
                    if ((pow & 1L) != 0)
                    {
                        GcmUtilities.Multiply(y, powX);
                    }

                    GcmUtilities.Square(powX, powX);
                    pow >>= 1;
                }
                while (pow > 0);
            }

            GcmUtilities.AsBytes(y, output);
        }
    }
}
#pragma warning restore
#endif
