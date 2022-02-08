#if !BESTHTTP_DISABLE_ALTERNATE_SSL && (!UNITY_WEBGL || UNITY_EDITOR)
#pragma warning disable
namespace BestHTTP.SecureProtocol.Org.BouncyCastle.Crypto.Fpe
{
    using System;
    using BestHTTP.SecureProtocol.Org.BouncyCastle.Crypto.Engines;
    using BestHTTP.SecureProtocol.Org.BouncyCastle.Crypto.Parameters;
    using BestHTTP.SecureProtocol.Org.BouncyCastle.Crypto.Utilities;
    using BestHTTP.SecureProtocol.Org.BouncyCastle.Utilities;

    public class FpeFf3_1Engine
        : FpeEngine
    {
        public FpeFf3_1Engine()
            : this(new AesEngine())
        {
        }

        public FpeFf3_1Engine(IBlockCipher baseCipher)
            : base(baseCipher)
        {
            if (IsOverrideSet(SP80038G.FPE_DISABLED)) throw new InvalidOperationException("FPE disabled");
        }

        public override void Init(bool forEncryption, ICipherParameters parameters)
        {
            this.forEncryption = forEncryption;
            this.fpeParameters = (FpeParameters)parameters;

            this.baseCipher.Init(!this.fpeParameters.UseInverseFunction, new KeyParameter(Arrays.Reverse(this.fpeParameters.Key.GetKey())));

            if (this.fpeParameters.GetTweak().Length != 7)
                throw new ArgumentException("tweak should be 56 bits");
        }

        protected override int EncryptBlock(byte[] inBuf, int inOff, int length, byte[] outBuf, int outOff)
        {
            byte[] enc;

            if (this.fpeParameters.Radix > 256)
            {
                if ((length & 1) != 0)
                    throw new ArgumentException("input must be an even number of bytes for a wide radix");

                var u16In = Pack.BE_To_UInt16(inBuf, inOff, length);
                var u16Out = SP80038G.EncryptFF3_1w(this.baseCipher, this.fpeParameters.Radix, this.fpeParameters.GetTweak(),
                    u16In, 0, u16In.Length);
                enc = Pack.UInt16_To_BE(u16Out, 0, u16Out.Length);
            }
            else
            {
                enc = SP80038G.EncryptFF3_1(this.baseCipher, this.fpeParameters.Radix, this.fpeParameters.GetTweak(), inBuf, inOff, length);
            }

            Array.Copy(enc, 0, outBuf, outOff, length);

            return length;
        }

        protected override int DecryptBlock(byte[] inBuf, int inOff, int length, byte[] outBuf, int outOff)
        {
            byte[] dec;

            if (this.fpeParameters.Radix > 256)
            {
                if ((length & 1) != 0)
                    throw new ArgumentException("input must be an even number of bytes for a wide radix");

                var u16In = Pack.BE_To_UInt16(inBuf, inOff, length);
                var u16Out = SP80038G.DecryptFF3_1w(this.baseCipher, this.fpeParameters.Radix, this.fpeParameters.GetTweak(),
                    u16In, 0, u16In.Length);
                dec = Pack.UInt16_To_BE(u16Out, 0, u16Out.Length);
            }
            else
            {
                dec = SP80038G.DecryptFF3_1(this.baseCipher, this.fpeParameters.Radix, this.fpeParameters.GetTweak(), inBuf, inOff, length);
            }

            Array.Copy(dec, 0, outBuf, outOff, length);

            return length;
        }
    }
}
#pragma warning restore
#endif