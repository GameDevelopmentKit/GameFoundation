#if !BESTHTTP_DISABLE_ALTERNATE_SSL && (!UNITY_WEBGL || UNITY_EDITOR)
#pragma warning disable
using System;
using System.Text;

using BestHTTP.SecureProtocol.Org.BouncyCastle.Crypto.Parameters;
using BestHTTP.SecureProtocol.Org.BouncyCastle.Crypto.Utilities;
using BestHTTP.SecureProtocol.Org.BouncyCastle.Utilities;

namespace BestHTTP.SecureProtocol.Org.BouncyCastle.Crypto.Engines
{
    /// <summary>
    /// Implementation of Daniel J. Bernstein's Salsa20 stream cipher, Snuffle 2005
    /// </summary>
    [BestHTTP.PlatformSupport.IL2CPP.Il2CppSetOption(BestHTTP.PlatformSupport.IL2CPP.Option.NullChecks, false)]
    [BestHTTP.PlatformSupport.IL2CPP.Il2CppSetOption(BestHTTP.PlatformSupport.IL2CPP.Option.ArrayBoundsChecks, false)]
    [BestHTTP.PlatformSupport.IL2CPP.Il2CppSetOption(BestHTTP.PlatformSupport.IL2CPP.Option.DivideByZeroChecks, false)]
    [BestHTTP.PlatformSupport.IL2CPP.Il2CppEagerStaticClassConstructionAttribute]
    public class Salsa20Engine
		: IStreamCipher
	{
		public static readonly int DEFAULT_ROUNDS = 20;

		/** Constants */
		private const int StateSize = 16; // 16, 32 bit ints = 64 bytes

        private readonly static uint[] TAU_SIGMA = Pack.LE_To_UInt32(Strings.ToAsciiByteArray("expand 16-byte k" + "expand 32-byte k"), 0, 8);

        internal void PackTauOrSigma(int keyLength, uint[] state, int stateOffset)
        {
            int tsOff = (keyLength - 16) / 4;
            state[stateOffset] = TAU_SIGMA[tsOff];
            state[stateOffset + 1] = TAU_SIGMA[tsOff + 1];
            state[stateOffset + 2] = TAU_SIGMA[tsOff + 2];
            state[stateOffset + 3] = TAU_SIGMA[tsOff + 3];
        }

        [Obsolete]
        protected readonly static byte[]
			sigma = Strings.ToAsciiByteArray("expand 32-byte k"),
			tau = Strings.ToAsciiByteArray("expand 16-byte k");

		protected int rounds;

		/*
		 * variables to hold the state of the engine
		 * during encryption and decryption
		 */
		private int		 index = 0;
		internal uint[] engineState = new uint[StateSize]; // state
		internal uint[] x = new uint[StateSize]; // internal buffer
		private byte[]	 keyStream = new byte[StateSize * 4]; // expanded state, 64 bytes
		private bool	 initialised = false;

		/*
		 * internal counter
		 */
		private uint cW0, cW1, cW2;

		/// <summary>
		/// Creates a 20 round Salsa20 engine.
		/// </summary>
		public Salsa20Engine()
			: this(DEFAULT_ROUNDS)
		{
		}

		/// <summary>
		/// Creates a Salsa20 engine with a specific number of rounds.
		/// </summary>
		/// <param name="rounds">the number of rounds (must be an even number).</param>
		public Salsa20Engine(int rounds)
		{
			if (rounds <= 0 || (rounds & 1) != 0)
			{
				throw new ArgumentException("'rounds' must be a positive, even number");
			}

			this.rounds = rounds;
		}

        public virtual void Init(
			bool				forEncryption, 
			ICipherParameters	parameters)
		{
			/* 
			 * Salsa20 encryption and decryption is completely
			 * symmetrical, so the 'forEncryption' is 
			 * irrelevant. (Like 90% of stream ciphers)
			 */

			ParametersWithIV ivParams = parameters as ParametersWithIV;
			if (ivParams == null)
				throw new ArgumentException(AlgorithmName + " Init requires an IV", "parameters");

			byte[] iv = ivParams.GetIV();
			if (iv == null || iv.Length != NonceSize)
				throw new ArgumentException(AlgorithmName + " requires exactly " + NonceSize + " bytes of IV");

            ICipherParameters keyParam = ivParams.Parameters;
            if (keyParam == null)
            {
                if (!initialised)
                    throw new InvalidOperationException(AlgorithmName + " KeyParameter can not be null for first initialisation");

                SetKey(null, iv);
            }
            else if (keyParam is KeyParameter)
            {
                SetKey(((KeyParameter)keyParam).GetKey(), iv);
            }
            else
            {
                throw new ArgumentException(AlgorithmName + " Init parameters must contain a KeyParameter (or null for re-init)");
            }

            Reset();
			initialised = true;
		}

		protected virtual int NonceSize
		{
			get { return 8; }
		}

		public virtual string AlgorithmName
		{
			get
            { 
				string name = "Salsa20";
				if (rounds != DEFAULT_ROUNDS)
				{
					name += "/" + rounds;
				}
				return name;
			}
		}

        public virtual byte ReturnByte(
			byte input)
		{
			if (LimitExceeded())
			{
				throw new MaxBytesExceededException("2^70 byte limit per IV; Change IV");
			}

			if (index == 0)
			{
				GenerateKeyStream(keyStream);
				AdvanceCounter();
			}

			byte output = (byte)(keyStream[index] ^ input);
			index = (index + 1) & 63;

			return output;
		}

		protected virtual void AdvanceCounter()
		{
			if (++engineState[8] == 0)
			{
				++engineState[9];
			}
		}

        public unsafe virtual void ProcessBytes(
			byte[]	inBytes, 
			int		inOff, 
			int		len, 
			byte[]	outBytes, 
			int		outOff)
		{
			if (!initialised)
				throw new InvalidOperationException(AlgorithmName + " not initialised");

            Check.DataLength(inBytes, inOff, len, "input buffer too short");
            Check.OutputLength(outBytes, outOff, len, "output buffer too short");

            if (LimitExceeded((uint)len))
				throw new MaxBytesExceededException("2^70 byte limit per IV would be exceeded; Change IV");

            fixed (byte* pinBytes = inBytes, poutBytes = outBytes, pkeyStream = keyStream)
            {
                int ulongLen = len / sizeof(ulong);

                for (int i = 0; i < ulongLen; i++)
                {
                    if (index == 0)
                    {
                        GenerateKeyStream(keyStream);
                        AdvanceCounter();
                    }

                    ulong* pin = (ulong*)pinBytes;
                    ulong* pout = (ulong*)poutBytes;
                    ulong* pkey = (ulong*)pkeyStream;

                    pout[i + outOff] = pkey[index] ^ pin[i + inOff];
                    index = (index + 1) & ((64 / sizeof(ulong)) - 1);
                    //poutBytes[i + outOff] = (byte)(pkeyStream[index] ^ pinBytes[i + inOff]);
                    //index = (index + 1) & 63;
                }

                int remainingOffset = ulongLen * sizeof(ulong);
                index = (index * sizeof(ulong)) & 63;
                for (int i = remainingOffset; i < len; i++)
                {
                    if (index == 0)
                    {
                        GenerateKeyStream(keyStream);
                        AdvanceCounter();
                    }

                    poutBytes[i + outOff] = (byte)(pkeyStream[index] ^ pinBytes[i + inOff]);
                    index = (index + 1) & 63;
                }
            }
		}

        public virtual void Reset()
		{
			index = 0;
			ResetLimitCounter();
			ResetCounter();
		}

		protected virtual void ResetCounter()
		{
			engineState[8] = engineState[9] = 0;
		}

		protected virtual void SetKey(byte[] keyBytes, byte[] ivBytes)
		{
            if (keyBytes != null)
            {
                if ((keyBytes.Length != 16) && (keyBytes.Length != 32))
                    throw new ArgumentException(AlgorithmName + " requires 128 bit or 256 bit key");

                int tsOff = (keyBytes.Length - 16) / 4;
                engineState[0] = TAU_SIGMA[tsOff];
                engineState[5] = TAU_SIGMA[tsOff + 1];
                engineState[10] = TAU_SIGMA[tsOff + 2];
                engineState[15] = TAU_SIGMA[tsOff + 3];

                // Key
                Pack.LE_To_UInt32(keyBytes, 0, engineState, 1, 4);
                Pack.LE_To_UInt32(keyBytes, keyBytes.Length - 16, engineState, 11, 4);
            }

            // IV
            Pack.LE_To_UInt32(ivBytes, 0, engineState, 6, 2);
        }

        protected unsafe virtual void GenerateKeyStream(byte[] output)
		{
			SalsaCore(rounds, engineState, x);

            fixed (uint* ns = x)
            fixed (byte* bs = output)
            {
                int off = 0;
                uint* bsuint = (uint*)bs;
                for (int i = 0; i < 4; ++i)
                    bsuint[i] = ns[i];
            }
        }

		internal unsafe static void SalsaCore(int rounds, uint[] input, uint[] x)
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
                    uint tempX = (x00 + x12);
                    x04 ^= (tempX << 7) | (tempX >> (32 - 7));

                    tempX = (x04 + x00);
                    x08 ^= (tempX << 9) | (tempX >> (32 - 9));

                    tempX = (x08 + x04);
                    x12 ^= (tempX << 13) | (tempX >> (32 - 13));

                    tempX = (x12 + x08);
                    x00 ^= (tempX << 18) | (tempX >> (32 - 18));

                    tempX = (x05 + x01);
                    x09 ^= (tempX << 7) | (tempX >> (32 - 7));

                    tempX = (x09 + x05);
                    x13 ^= (tempX << 9) | (tempX >> (32 - 9));

                    tempX = (x13 + x09);
                    x01 ^= (tempX << 13) | (tempX >> (32 - 13));

                    tempX = (x01 + x13);
                    x05 ^= (tempX << 18) | (tempX >> (32 - 18));

                    tempX = (x10 + x06);
                    x14 ^= (tempX << 7) | (tempX >> (32 - 7));

                    tempX = (x14 + x10);
                    x02 ^= (tempX << 9) | (tempX >> (32 - 9));

                    tempX = (x02 + x14);
                    x06 ^= (tempX << 13) | (tempX >> (32 - 13));

                    tempX = (x06 + x02);
                    x10 ^= (tempX << 18) | (tempX >> (32 - 18));

                    tempX = (x15 + x11);
                    x03 ^= (tempX << 7) | (tempX >> (32 - 7));

                    tempX = (x03 + x15);
                    x07 ^= (tempX << 9) | (tempX >> (32 - 9));

                    tempX = (x07 + x03);
                    x11 ^= (tempX << 13) | (tempX >> (32 - 13));

                    tempX = (x11 + x07);
                    x15 ^= (tempX << 18) | (tempX >> (32 - 18));


                    tempX = (x00 + x03);
                    x01 ^= (tempX << 7) | (tempX >> (32 - 7));

                    tempX = (x01 + x00);
                    x02 ^= (tempX << 9) | (tempX >> (32 - 9));

                    tempX = (x02 + x01);
                    x03 ^= (tempX << 13) | (tempX >> (32 - 13));

                    tempX = (x03 + x02);
                    x00 ^= (tempX << 18) | (tempX >> (32 - 18));

                    tempX = (x05 + x04);
                    x06 ^= (tempX << 7) | (tempX >> (32 - 7));

                    tempX = (x06 + x05);
                    x07 ^= (tempX << 9) | (tempX >> (32 - 9));

                    tempX = (x07 + x06);
                    x04 ^= (tempX << 13) | (tempX >> (32 - 13));

                    tempX = (x04 + x07);
                    x05 ^= (tempX << 18) | (tempX >> (32 - 18));

                    tempX = x10 + x09;
                    x11 ^= (tempX << 7) | (tempX >> (32 - 7));

                    tempX = x11 + x10;
                    x08 ^= (tempX << 9) | (tempX >> (32 - 9));

                    tempX = x11 + x10;
                    x09 ^= (tempX << 13) | (tempX >> (32 - 13));

                    tempX = x09 + x08;
                    x10 ^= (tempX << 18) | (tempX >> (32 - 18));

                    tempX = x15 + x14;
                    x12 ^= (tempX << 7) | (tempX >> (32 - 7));

                    tempX = x12 + x15;
                    x13 ^= (tempX << 9) | (tempX >> (32 - 9));

                    tempX = x13 + x12;
                    x14 ^= (tempX << 13) | (tempX >> (32 - 13));

                    tempX = x14 + x13;
                    x15 ^= (tempX << 18) | (tempX >> (32 - 18));
                }

                px[0] = x00 + pinput[0];
                px[1] = x01 + pinput[1];
                px[2] = x02 + pinput[2];
                px[3] = x03 + pinput[3];
                px[4] = x04 + pinput[4];
                px[5] = x05 + pinput[5];
                px[6] = x06 + pinput[6];
                px[7] = x07 + pinput[7];
                px[8] = x08 + pinput[8];
                px[9] = x09 + pinput[9];
                px[10] = x10 + pinput[10];
                px[11] = x11 + pinput[11];
                px[12] = x12 + pinput[12];
                px[13] = x13 + pinput[13];
                px[14] = x14 + pinput[14];
                px[15] = x15 + pinput[15];
            }
		}

		/**
		 * Rotate left
		 *
		 * @param   x   value to rotate
		 * @param   y   amount to rotate x
		 *
		 * @return  rotated x
		 */
		//internal static uint R(uint x, int y)
		//{
		//	return (x << y) | (x >> (32 - y));
		//}

		private void ResetLimitCounter()
		{
			cW0 = 0;
			cW1 = 0;
			cW2 = 0;
		}

		private bool LimitExceeded()
		{
			if (++cW0 == 0)
			{
				if (++cW1 == 0)
				{
					return (++cW2 & 0x20) != 0;          // 2^(32 + 32 + 6)
				}
			}

			return false;
		}

		/*
		 * this relies on the fact len will always be positive.
		 */
		private bool LimitExceeded(
			uint len)
		{
			uint old = cW0;
			cW0 += len;
			if (cW0 < old)
			{
				if (++cW1 == 0)
				{
					return (++cW2 & 0x20) != 0;          // 2^(32 + 32 + 6)
				}
			}

			return false;
		}
	}
}
#pragma warning restore
#endif
