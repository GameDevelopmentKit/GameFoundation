#if !BESTHTTP_DISABLE_ALTERNATE_SSL && (!UNITY_WEBGL || UNITY_EDITOR)
#pragma warning disable
using System;

using BestHTTP.SecureProtocol.Org.BouncyCastle.Crypto.Utilities;

namespace BestHTTP.SecureProtocol.Org.BouncyCastle.Crypto.Engines
{
    /// <summary>
    /// Implementation of Daniel J. Bernstein's ChaCha stream cipher.
    /// </summary>
    [BestHTTP.PlatformSupport.IL2CPP.Il2CppSetOption(BestHTTP.PlatformSupport.IL2CPP.Option.NullChecks, false)]
    [BestHTTP.PlatformSupport.IL2CPP.Il2CppSetOption(BestHTTP.PlatformSupport.IL2CPP.Option.ArrayBoundsChecks, false)]
    [BestHTTP.PlatformSupport.IL2CPP.Il2CppSetOption(BestHTTP.PlatformSupport.IL2CPP.Option.DivideByZeroChecks, false)]
    [BestHTTP.PlatformSupport.IL2CPP.Il2CppEagerStaticClassConstructionAttribute]
    public class ChaCha7539Engine
        : Salsa20Engine
    {
        /// <summary>
        /// Creates a 20 rounds ChaCha engine.
        /// </summary>
        public ChaCha7539Engine()
            : base()
        {
        }

        public override string AlgorithmName
        {
            get { return "ChaCha7539"; }
        }

        protected override int NonceSize
        {
            get { return 12; }
        }

        protected override void AdvanceCounter()
        {
            if (++engineState[12] == 0)
                throw new InvalidOperationException("attempt to increase counter past 2^32.");
        }

        protected override void ResetCounter()
        {
            engineState[12] = 0;
        }

        protected override void SetKey(byte[] keyBytes, byte[] ivBytes)
        {
            if (keyBytes != null)
            {
                if (keyBytes.Length != 32)
                    throw new ArgumentException(AlgorithmName + " requires 256 bit key");

                PackTauOrSigma(keyBytes.Length, engineState, 0);

                // Key
                Pack.LE_To_UInt32(keyBytes, 0, engineState, 4, 8);
            }

            // IV
            Pack.LE_To_UInt32(ivBytes, 0, engineState, 13, 3);
        }

        protected unsafe override void GenerateKeyStream(byte[] output)
        {
            ChaChaEngine.ChachaCore(rounds, engineState, x);
            //Pack.UInt32_To_LE(x, output, 0);

            fixed (uint* ns = x)
            fixed (byte* bs = output)
            {
                int off = 0;
                for (int i = 0; i < x.Length; ++i)
                {
                    //UInt32_To_LE(ns[i], bs, off);
                    //UInt32_To_LE(uint n, byte[] bs, int off)
                    {
                        uint n = ns[i];

                        bs[off++] = (byte)(n);
                        bs[off++] = (byte)(n >> 8);
                        bs[off++] = (byte)(n >> 16);
                        bs[off++] = (byte)(n >> 24);
                    }
                }
            }
        }
    }
}

#pragma warning restore
#endif
