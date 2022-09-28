#if !BESTHTTP_DISABLE_ALTERNATE_SSL && (!UNITY_WEBGL || UNITY_EDITOR)
#pragma warning disable
namespace BestHTTP.SecureProtocol.Org.BouncyCastle.Math.EC.Rfc7748
{
    using System.Diagnostics;
    using BestHTTP.SecureProtocol.Org.BouncyCastle.Math.EC.Rfc8032;
    using BestHTTP.SecureProtocol.Org.BouncyCastle.Security;
    using BestHTTP.SecureProtocol.Org.BouncyCastle.Utilities;

    public abstract class X448
    {
        public const int PointSize = 56;
        public const int ScalarSize = 56;

        private class F : X448Field
        {
        }

        private const uint C_A = 156326;
        private const uint C_A24 = (C_A + 2)/4;

        //private static readonly uint[] Sqrt156324 = { 0x0551B193U, 0x07A21E17U, 0x0E635AD3U, 0x00812ABBU, 0x025B3F99U, 0x01605224U,
        //    0x0AF8CB32U, 0x0D2E7D68U, 0x06BA50FDU, 0x08E55693U, 0x0CB08EB4U, 0x02ABEBC1U, 0x051BA0BBU, 0x02F8812EU, 0x0829B611U,
        //    0x0BA4D3A0U };

        public static bool CalculateAgreement(byte[] k, int kOff, byte[] u, int uOff, byte[] r, int rOff)
        {
            ScalarMult(k, kOff, u, uOff, r, rOff);
            return !Arrays.AreAllZeroes(r, rOff, PointSize);
        }

        private static uint Decode32(byte[] bs, int off)
        {
            uint n = bs[off];
            n |= (uint)bs[++off] << 8;
            n |= (uint)bs[++off] << 16;
            n |= (uint)bs[++off] << 24;
            return n;
        }

        private static void DecodeScalar(byte[] k, int kOff, uint[] n)
        {
            for (int i = 0; i < 14; ++i)
            {
                n[i] = Decode32(k, kOff + i * 4);
            }

            n[ 0] &= 0xFFFFFFFCU;
            n[13] |= 0x80000000U;
        }

        public static void GeneratePrivateKey(SecureRandom random, byte[] k)
        {
            random.NextBytes(k);

            k[0] &= 0xFC;
            k[ScalarSize - 1] |= 0x80;
        }

        public static void GeneratePublicKey(byte[] k, int kOff, byte[] r, int rOff)
        {
            ScalarMultBase(k, kOff, r, rOff);
        }

        private static void PointDouble(uint[] x, uint[] z)
        {
            var a = X448Field.Create();
            var b = X448Field.Create();

            //F.Apm(x, z, a, b);
            X448Field.Add(x, z, a);
            X448Field.Sub(x, z, b);
            X448Field.Sqr(a, a);
            X448Field.Sqr(b, b);
            X448Field.Mul(a, b, x);
            X448Field.Sub(a, b, a);
            X448Field.Mul(a, C_A24, z);
            X448Field.Add(z, b, z);
            X448Field.Mul(z, a, z);
        }

        public static void Precompute()
        {
            Ed448.Precompute();
        }

        public static void ScalarMult(byte[] k, int kOff, byte[] u, int uOff, byte[] r, int rOff)
        {
            uint[] n = new uint[14];    DecodeScalar(k, kOff, n);

            var x1 = X448Field.Create();
            X448Field.Decode(u, uOff, x1);
            var x2 = X448Field.Create();
            X448Field.Copy(x1, 0, x2, 0);
            var z2 = X448Field.Create();
            z2[0] = 1;
            var x3 = X448Field.Create();
            x3[0] = 1;
            var z3 = X448Field.Create();

            var t1 = X448Field.Create();
            var t2 = X448Field.Create();

            Debug.Assert(n[13] >> 31 == 1U);

            int bit = 447, swap = 1;
            do
            {
                //F.Apm(x3, z3, t1, x3);
                X448Field.Add(x3, z3, t1);
                X448Field.Sub(x3, z3, x3);
                //F.Apm(x2, z2, z3, x2);
                X448Field.Add(x2, z2, z3);
                X448Field.Sub(x2, z2, x2);

                X448Field.Mul(t1, x2, t1);
                X448Field.Mul(x3, z3, x3);
                X448Field.Sqr(z3, z3);
                X448Field.Sqr(x2, x2);

                X448Field.Sub(z3, x2, t2);
                X448Field.Mul(t2, C_A24, z2);
                X448Field.Add(z2, x2, z2);
                X448Field.Mul(z2, t2, z2);
                X448Field.Mul(x2, z3, x2);

                //F.Apm(t1, x3, x3, z3);
                X448Field.Sub(t1, x3, z3);
                X448Field.Add(t1, x3, x3);
                X448Field.Sqr(x3, x3);
                X448Field.Sqr(z3, z3);
                X448Field.Mul(z3, x1, z3);

                --bit;

                int word = bit >> 5, shift = bit & 0x1F;
                int kt = (int)(n[word] >> shift) & 1;
                swap ^= kt;
                X448Field.CSwap(swap, x2, x3);
                X448Field.CSwap(swap, z2, z3);
                swap = kt;
            }
            while (bit >= 2);

            Debug.Assert(swap == 0);

            for (int i = 0; i < 2; ++i)
            {
                PointDouble(x2, z2);
            }

            X448Field.Inv(z2, z2);
            X448Field.Mul(x2, z2, x2);

            X448Field.Normalize(x2);
            X448Field.Encode(x2, r, rOff);
        }

        public static void ScalarMultBase(byte[] k, int kOff, byte[] r, int rOff)
        {
            var x = X448Field.Create();
            var y = X448Field.Create();

            Ed448.ScalarMultBaseXY(k, kOff, x, y);

            X448Field.Inv(x, x);
            X448Field.Mul(x, y, x);
            X448Field.Sqr(x, x);

            X448Field.Normalize(x);
            X448Field.Encode(x, r, rOff);
        }
    }
}
#pragma warning restore
#endif
