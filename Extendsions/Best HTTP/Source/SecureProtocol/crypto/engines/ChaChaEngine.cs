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
    public sealed class ChaChaEngine
		: Salsa20Engine
	{
		/// <summary>
		/// Creates a 20 rounds ChaCha engine.
		/// </summary>
		public ChaChaEngine()
		{
		}

		/// <summary>
		/// Creates a ChaCha engine with a specific number of rounds.
		/// </summary>
		/// <param name="rounds">the number of rounds (must be an even number).</param>
		public ChaChaEngine(int rounds)
			: base(rounds)
		{
		}

		public override string AlgorithmName
		{
			get { return "ChaCha" + rounds; }
		}

		protected override void AdvanceCounter()
		{
			if (++engineState[12] == 0)
			{
				++engineState[13];
			}
		}

		protected override void ResetCounter()
		{
			engineState[12] = engineState[13] = 0;
		}

		protected override void SetKey(byte[] keyBytes, byte[] ivBytes)
		{
            if (keyBytes != null)
            {
                if ((keyBytes.Length != 16) && (keyBytes.Length != 32))
                    throw new ArgumentException(AlgorithmName + " requires 128 bit or 256 bit key");

                PackTauOrSigma(keyBytes.Length, engineState, 0);

                // Key
                Pack.LE_To_UInt32(keyBytes, 0, engineState, 4, 4);
                Pack.LE_To_UInt32(keyBytes, keyBytes.Length - 16, engineState, 8, 4);
            }

            // IV
            Pack.LE_To_UInt32(ivBytes, 0, engineState, 14, 2);
		}

		protected unsafe override void GenerateKeyStream(byte[] output)
		{
			ChachaCore(rounds, engineState, x);

            fixed (uint* ns = x)
            fixed (byte* bs = output)
            {
                int off = 0;
                uint* bsuint = (uint*)bs;
                for (int i = 0; i < 4; ++i)
                    bsuint[i] = ns[i];
            }
        }

		/// <summary>
		/// ChaCha function.
		/// </summary>
		/// <param name="rounds">The number of ChaCha rounds to execute</param>
		/// <param name="input">The input words.</param>
		/// <param name="x">The ChaCha state to modify.</param>
		internal unsafe static void ChachaCore(int rounds, uint[] input, uint[] x)
		{
            fixed (uint* pinput = input, px = x)
            {
                uint x00 = pinput[0];
                uint x01 = pinput[1];
                uint x02 = pinput[2];
                uint x03 = pinput[3];
                uint x04 = pinput[4];
                uint x05 = pinput[5];
                uint x06 = pinput[6];
                uint x07 = pinput[7];
                uint x08 = pinput[8];
                uint x09 = pinput[9];
                uint x10 = pinput[10];
                uint x11 = pinput[11];
                uint x12 = pinput[12];
                uint x13 = pinput[13];
                uint x14 = pinput[14];
                uint x15 = pinput[15];

                for (int i = rounds; i > 0; i -= 2)
                {
                    // R(x, y) => (tempX << y) | (tempX >> (32 - y))

                    x00 += x04;

                    uint tempX = x12 ^ x00;
                    x12 = (tempX << 16) | (tempX >> (32 - 16));

                    x08 += x12;
                    tempX = x04 ^ x08;
                    x04 = (tempX << 12) | (tempX >> (32 - 12));

                    x00 += x04;
                    tempX = x12 ^ x00;
                    x12 = (tempX << 8) | (tempX >> (32 - 8));

                    x08 += x12;
                    tempX = x04 ^ x08;
                    x04 = (tempX << 7) | (tempX >> (32 - 7));

                    x01 += x05;
                    tempX = x13 ^ x01;
                    x13 = (tempX << 16) | (tempX >> (32 - 16));

                    x09 += x13;
                    tempX = x05 ^ x09;
                    x05 = (tempX << 12) | (tempX >> (32 - 12));

                    x01 += x05;
                    tempX = x13 ^ x01;
                    x13 = (tempX << 8) | (tempX >> (32 - 8));

                    x09 += x13;
                    tempX = x05 ^ x09;
                    x05 = (tempX << 7) | (tempX >> (32 - 7));

                    x02 += x06;
                    tempX = x14 ^ x02;
                    x14 = (tempX << 16) | (tempX >> (32 - 16));

                    x10 += x14;
                    tempX = x06 ^ x10;
                    x06 = (tempX << 12) | (tempX >> (32 - 12));

                    x02 += x06;
                    tempX = x14 ^ x02;
                    x14 = (tempX << 8) | (tempX >> (32 - 8));

                    x10 += x14;
                    tempX = x06 ^ x10;
                    x06 = (tempX << 7) | (tempX >> (32 - 7));

                    x03 += x07;
                    tempX = x15 ^ x03;
                    x15 = (tempX << 16) | (tempX >> (32 - 16));

                    x11 += x15;
                    tempX = x07 ^ x11;
                    x07 = (tempX << 12) | (tempX >> (32 - 12));

                    x03 += x07;
                    tempX = x15 ^ x03;
                    x15 = (tempX << 8) | (tempX >> (32 - 8));

                    x11 += x15;
                    tempX = x07 ^ x11;
                    x07 = (tempX << 7) | (tempX >> (32 - 7));

                    x00 += x05;
                    tempX = x15 ^ x00;
                    x15 = (tempX << 16) | (tempX >> (32 - 16));

                    x10 += x15;
                    tempX = x05 ^ x10;
                    x05 = (tempX << 12) | (tempX >> (32 - 12));

                    x00 += x05;
                    tempX = x15 ^ x00;
                    x15 = (tempX << 8) | (tempX >> (32 - 8));

                    x10 += x15;
                    tempX = x05 ^ x10;
                    x05 = (tempX << 7) | (tempX >> (32 - 7));

                    x01 += x06;
                    tempX = x12 ^ x01;
                    x12 = (tempX << 16) | (tempX >> (32 - 16));

                    x11 += x12;
                    tempX = x06 ^ x11;
                    x06 = (tempX << 12) | (tempX >> (32 - 12));

                    x01 += x06;
                    tempX = x12 ^ x01;
                    x12 = (tempX << 8) | (tempX >> (32 - 8));

                    x11 += x12;
                    tempX = x06 ^ x11;
                    x06 = (tempX << 7) | (tempX >> (32 - 7));

                    x02 += x07;
                    tempX = x13 ^ x02;
                    x13 = (tempX << 16) | (tempX >> (32 - 16));

                    x08 += x13;
                    tempX = x07 ^ x08;
                    x07 = (tempX << 12) | (tempX >> (32 - 12));

                    x02 += x07;
                    tempX = x13 ^ x02;
                    x13 = (tempX << 8) | (tempX >> (32 - 8));

                    x08 += x13;
                    tempX = x07 ^ x08;
                    x07 = (tempX << 7) | (tempX >> (32 - 7));

                    x03 += x04;
                    tempX = x14 ^ x03;
                    x14 = (tempX << 16) | (tempX >> (32 - 16));

                    x09 += x14;
                    tempX = x04 ^ x09;
                    x04 = (tempX << 12) | (tempX >> (32 - 12));

                    x03 += x04;
                    tempX = x14 ^ x03;
                    x14 = (tempX << 8) | (tempX >> (32 - 8));

                    x09 += x14;
                    tempX = x04 ^ x09;
                    x04 = (tempX << 7) | (tempX >> (32 - 7));
                }

                px[0] = x00 +  pinput[0];
                px[1] = x01 +  pinput[1];
                px[2] = x02 +  pinput[2];
                px[3] = x03 +  pinput[3];
                px[4] = x04 +  pinput[4];
                px[5] = x05 +  pinput[5];
                px[6] = x06 +  pinput[6];
                px[7] = x07 +  pinput[7];
                px[8] = x08 +  pinput[8];
                px[9] = x09 +  pinput[9];
                px[10] = x10 + pinput[10];
                px[11] = x11 + pinput[11];
                px[12] = x12 + pinput[12];
                px[13] = x13 + pinput[13];
                px[14] = x14 + pinput[14];
                px[15] = x15 + pinput[15];
            }
		}
	}
}
#pragma warning restore
#endif
