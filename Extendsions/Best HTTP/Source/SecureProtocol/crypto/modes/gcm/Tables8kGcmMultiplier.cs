#if !BESTHTTP_DISABLE_ALTERNATE_SSL && (!UNITY_WEBGL || UNITY_EDITOR)
#pragma warning disable
using System;

using BestHTTP.SecureProtocol.Org.BouncyCastle.Crypto.Utilities;
using BestHTTP.SecureProtocol.Org.BouncyCastle.Utilities;

namespace BestHTTP.SecureProtocol.Org.BouncyCastle.Crypto.Modes.Gcm
{
    [BestHTTP.PlatformSupport.IL2CPP.Il2CppSetOption(BestHTTP.PlatformSupport.IL2CPP.Option.NullChecks, false)]
    [BestHTTP.PlatformSupport.IL2CPP.Il2CppSetOption(BestHTTP.PlatformSupport.IL2CPP.Option.ArrayBoundsChecks, false)]
    [BestHTTP.PlatformSupport.IL2CPP.Il2CppSetOption(BestHTTP.PlatformSupport.IL2CPP.Option.DivideByZeroChecks, false)]
    [BestHTTP.PlatformSupport.IL2CPP.Il2CppEagerStaticClassConstructionAttribute]
    public sealed class Tables8kGcmMultiplier
        : IGcmMultiplier
    {
        private byte[] H;
        private uint[][][] M;

        public void Init(byte[] H)
        {
            if (M == null)
            {
                M = new uint[32][][];
            }
            else if (Arrays.AreEqual(this.H, H))
            {
                return;
            }

            this.H = Arrays.Clone(H);

            M[0] = new uint[16][];
            M[1] = new uint[16][];
            M[0][0] = new uint[4];
            M[1][0] = new uint[4];
            M[1][8] = GcmUtilities.AsUints(H);

            for (int j = 4; j >= 1; j >>= 1)
            {
                uint[] tmp = (uint[])M[1][j + j].Clone();
                GcmUtilities.MultiplyP(tmp);
                M[1][j] = tmp;
            }

            {
                uint[] tmp = (uint[])M[1][1].Clone();
                GcmUtilities.MultiplyP(tmp);
                M[0][8] = tmp;
            }

            for (int j = 4; j >= 1; j >>= 1)
            {
                uint[] tmp = (uint[])M[0][j + j].Clone();
                GcmUtilities.MultiplyP(tmp);
                M[0][j] = tmp;
            }

            for (int i = 0; ; )
            {
                for (int j = 2; j < 16; j += j)
                {
                    for (int k = 1; k < j; ++k)
                    {
                        uint[] tmp = (uint[])M[i][j].Clone();
                        GcmUtilities.Xor(tmp, M[i][k]);
                        M[i][j + k] = tmp;
                    }
                }

                if (++i == 32) return;

                if (i > 1)
                {
                    M[i] = new uint[16][];
                    M[i][0] = new uint[4];
                    for (int j = 8; j > 0; j >>= 1)
                    {
                        uint[] tmp = (uint[])M[i - 2][j].Clone();
                        GcmUtilities.MultiplyP8(tmp);
                        M[i][j] = tmp;
                    }
                }
            }
        }
        uint[] z = new uint[4];

        public unsafe void MultiplyH(byte[] x)
        {
            fixed (byte* px = x)
                fixed (uint* pz = z)
            {
                ulong* pulongZ = (ulong*)pz;
                pulongZ[0] = 0;
                pulongZ[1] = 0;

                for (int i = 15; i >= 0; --i)
                {
                    uint[] m = M[i + i][px[i] & 0x0f];
                    fixed (uint* pm = m)
                    {
                        ulong* pulongm = (ulong*)pm;
                        
                        pulongZ[0] ^= pulongm[0];
                        pulongZ[1] ^= pulongm[1];
                    }

                    m = M[i + i + 1][(px[i] & 0xf0) >> 4];
                    fixed (uint* pm = m)
                    {
                        ulong* pulongm = (ulong*)pm;

                        pulongZ[0] ^= pulongm[0];
                        pulongZ[1] ^= pulongm[1];
                    }
                }

                int off = 0;

                for (int i = 0; i < 4; ++i)
                {
                    uint n = pz[i];
                    px[off] =     (byte)(n >> 24);
                    px[off + 1] = (byte)(n >> 16);
                    px[off + 2] = (byte)(n >> 8);
                    px[off + 3] = (byte)(n);
                    
                    off += 4;
                }
            }
        }
    }
}
#pragma warning restore
#endif
