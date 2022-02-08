#if !BESTHTTP_DISABLE_ALTERNATE_SSL && (!UNITY_WEBGL || UNITY_EDITOR)
#pragma warning disable
namespace BestHTTP.SecureProtocol.Org.BouncyCastle.Crypto.Modes.Gcm
{
    using BestHTTP.SecureProtocol.Org.BouncyCastle.Crypto.Utilities;
    using BestHTTP.SecureProtocol.Org.BouncyCastle.Utilities;

    public class Tables64kGcmMultiplier
        : IGcmMultiplier
    {
        private byte[]    H;
        private ulong[][] T;

        public void Init(byte[] H)
        {
            if (this.T == null)
            {
                this.T = new ulong[16][];
            }
            else if (Arrays.AreEqual(this.H, H))
            {
                return;
            }

            this.H = Arrays.Clone(H);

            for (var i = 0; i < 16; ++i)
            {
                var t = this.T[i] = new ulong[512];

                // t[0] = 0

                if (i == 0)
                {
                    // t[1] = H.p^7
                    GcmUtilities.AsUlongs(this.H, t, 2);
                    GcmUtilities.MultiplyP7(t, 2, t, 2);
                }
                else
                {
                    // t[1] = T[i-1][1].p^8
                    GcmUtilities.MultiplyP8(this.T[i - 1], 2, t, 2);
                }

                for (var n = 2; n < 256; n += 2)
                {
                    // t[2.n] = t[n].p^-1
                    GcmUtilities.DivideP(t, n, t, n << 1);

                    // t[2.n + 1] = t[2.n] + t[1]
                    GcmUtilities.Xor(t, n << 1, t, 2, t, (n + 1) << 1);
                }
            }
        }

        public void MultiplyH(byte[] x)
        {
            //ulong[] z = new ulong[2];
            //for (int i = 15; i >= 0; --i)
            //{
            //    GcmUtilities.Xor(z, 0, T[i], x[i] << 1);
            //}
            //Pack.UInt64_To_BE(z, x, 0);

            var   t    = this.T[15];
            var   tPos = x[15] << 1;
            ulong z0   = t[tPos + 0], z1 = t[tPos + 1];

            for (var i = 14; i >= 0; --i)
            {
                t    =  this.T[i];
                tPos =  x[i] << 1;
                z0   ^= t[tPos + 0];
                z1   ^= t[tPos + 1];
            }

            Pack.UInt64_To_BE(z0, x, 0);
            Pack.UInt64_To_BE(z1, x, 8);
        }
    }
}
#pragma warning restore
#endif
