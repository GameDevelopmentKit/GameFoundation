#if !BESTHTTP_DISABLE_ALTERNATE_SSL && (!UNITY_WEBGL || UNITY_EDITOR)
#pragma warning disable
namespace BestHTTP.SecureProtocol.Org.BouncyCastle.Crypto.Fpe
{
    using System;
    using BestHTTP.SecureProtocol.Org.BouncyCastle.Crypto.Engines;
    using BestHTTP.SecureProtocol.Org.BouncyCastle.Crypto.Parameters;
    using BestHTTP.SecureProtocol.Org.BouncyCastle.Crypto.Utilities;

    public class FpeFf1Engine
        : FpeEngine
    {
        public FpeFf1Engine()
            : this(new AesEngine())
        {
        }

        public FpeFf1Engine(IBlockCipher baseCipher)
            : base(baseCipher)
        {
            if (IsOverrideSet(SP80038G.FPE_DISABLED) ||
                IsOverrideSet(SP80038G.FF1_DISABLED))
                throw new InvalidOperationException("FF1 encryption disabled");
        }

        public override void Init(bool forEncryption, ICipherParameters parameters)
        {
            this.forEncryption = forEncryption;
            this.fpeParameters = (FpeParameters)parameters;

            this.baseCipher.Init(!this.fpeParameters.UseInverseFunction, this.fpeParameters.Key);
        }

        protected override int EncryptBlock(byte[] inBuf, int inOff, int length, byte[] outBuf, int outOff)
        {
            byte[] enc;

            if (this.fpeParameters.Radix > 256)
            {
                if ((length & 1) != 0)
                    throw new ArgumentException("input must be an even number of bytes for a wide radix");

                var u16In = Pack.BE_To_UInt16(inBuf, inOff, length);
                var u16Out = SP80038G.EncryptFF1w(this.baseCipher, this.fpeParameters.Radix, this.fpeParameters.GetTweak(),
                    u16In, 0, u16In.Length);
                enc = Pack.UInt16_To_BE(u16Out, 0, u16Out.Length);
            }
            else
            {
                enc = SP80038G.EncryptFF1(this.baseCipher, this.fpeParameters.Radix, this.fpeParameters.GetTweak(), inBuf, inOff, length);
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
                var u16Out = SP80038G.DecryptFF1w(this.baseCipher, this.fpeParameters.Radix, this.fpeParameters.GetTweak(),
                    u16In, 0, u16In.Length);
                dec = Pack.UInt16_To_BE(u16Out, 0, u16Out.Length);
            }
            else
            {
                dec = SP80038G.DecryptFF1(this.baseCipher, this.fpeParameters.Radix, this.fpeParameters.GetTweak(), inBuf, inOff, length);
            }

            Array.Copy(dec, 0, outBuf, outOff, length);

            return length;
        }
    }
}
#pragma warning restore
#endif