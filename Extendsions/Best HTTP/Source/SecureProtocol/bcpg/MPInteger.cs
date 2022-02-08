#if !BESTHTTP_DISABLE_ALTERNATE_SSL && (!UNITY_WEBGL || UNITY_EDITOR)
#pragma warning disable
namespace BestHTTP.SecureProtocol.Org.BouncyCastle.Bcpg
{
	using System;
	using BestHTTP.SecureProtocol.Org.BouncyCastle.Math;

	/// <remarks>A multiple precision integer</remarks>
    public class MPInteger
        : BcpgObject
    {
        public MPInteger(
            BcpgInputStream bcpgIn)
        {
            if (bcpgIn == null)
                throw new ArgumentNullException("bcpgIn");

            var length = (bcpgIn.ReadByte() << 8) | bcpgIn.ReadByte();
            var bytes  = new byte[(length + 7) / 8];

            bcpgIn.ReadFully(bytes);

            this.Value = new BigInteger(1, bytes);
        }

        public MPInteger(
            BigInteger val)
        {
            if (val == null)
                throw new ArgumentNullException("val");
            if (val.SignValue < 0)
                throw new ArgumentException("Values must be positive", "val");

            this.Value = val;
        }

        public BigInteger Value { get; }

        public override void Encode(
            BcpgOutputStream bcpgOut)
        {
            bcpgOut.WriteShort((short)this.Value.BitLength);
            bcpgOut.Write(this.Value.ToByteArrayUnsigned());
        }

        internal static void Encode(
            BcpgOutputStream bcpgOut,
            BigInteger val)
        {
            bcpgOut.WriteShort((short)val.BitLength);
            bcpgOut.Write(val.ToByteArrayUnsigned());
        }
    }
}
#pragma warning restore
#endif