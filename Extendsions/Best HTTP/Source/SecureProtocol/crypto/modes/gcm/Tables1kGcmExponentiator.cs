#if !BESTHTTP_DISABLE_ALTERNATE_SSL && (!UNITY_WEBGL || UNITY_EDITOR)
#pragma warning disable
namespace BestHTTP.SecureProtocol.Org.BouncyCastle.Crypto.Modes.Gcm
{
    using System.Collections;
    using BestHTTP.SecureProtocol.Org.BouncyCastle.Utilities;

    public class Tables1kGcmExponentiator
        : IGcmExponentiator
    {
        // A lookup table of the power-of-two powers of 'x'
        // - lookupPowX2[i] = x^(2^i)
        private IList lookupPowX2;

        public void Init(byte[] x)
        {
            var y = GcmUtilities.AsUlongs(x);
            if (this.lookupPowX2 != null && Arrays.AreEqual(y, (ulong[])this.lookupPowX2[0]))
                return;

            lookupPowX2 = Platform.CreateArrayList(8);
            lookupPowX2.Add(y);
        }

        public void ExponentiateX(long pow, byte[] output)
        {
            var y   = GcmUtilities.OneAsUlongs();
            int bit = 0;
            while (pow > 0)
            {
                if ((pow & 1L) != 0)
                {
                    EnsureAvailable(bit);
                    GcmUtilities.Multiply(y, (ulong[])this.lookupPowX2[bit]);
                }
                ++bit;
                pow >>= 1;
            }

            GcmUtilities.AsBytes(y, output);
        }

        private void EnsureAvailable(int bit)
        {
            int count = lookupPowX2.Count;
            if (count <= bit)
            {
                var tmp = (ulong[])this.lookupPowX2[count - 1];
                do
                {
                    tmp = Arrays.Clone(tmp);
                    GcmUtilities.Square(tmp, tmp);
                    lookupPowX2.Add(tmp);
                }
                while (++count <= bit);
            }
        }
    }
}
#pragma warning restore
#endif
