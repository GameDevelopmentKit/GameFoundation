#if !BESTHTTP_DISABLE_ALTERNATE_SSL && (!UNITY_WEBGL || UNITY_EDITOR)
#pragma warning disable
namespace BestHTTP.SecureProtocol.Org.BouncyCastle.Bcpg
{
	using System;
	using BestHTTP.SecureProtocol.Org.BouncyCastle.Math;

	/// <remarks>Base class for an ElGamal public key.</remarks>
    public class ElGamalPublicBcpgKey
        : BcpgObject, IBcpgKey
    {
        internal MPInteger p, g, y;

        public ElGamalPublicBcpgKey(
            BcpgInputStream bcpgIn)
        {
            this.p = new MPInteger(bcpgIn);
            this.g = new MPInteger(bcpgIn);
            this.y = new MPInteger(bcpgIn);
        }

        public ElGamalPublicBcpgKey(
            BigInteger p,
            BigInteger g,
            BigInteger y)
        {
            this.p = new MPInteger(p);
            this.g = new MPInteger(g);
            this.y = new MPInteger(y);
        }

        /// <summary>The format, as a string, always "PGP".</summary>
        public string Format => "PGP";

        /// <summary>Return the standard PGP encoding of the key.</summary>
        public override byte[] GetEncoded()
        {
            try
            {
                return base.GetEncoded();
            }
            catch (Exception)
            {
                return null;
            }
        }

        public BigInteger P => this.p.Value;

        public BigInteger G => this.g.Value;

        public BigInteger Y => this.y.Value;

        public override void Encode(
            BcpgOutputStream bcpgOut)
        {
            bcpgOut.WriteObjects(this.p, this.g, this.y);
        }
    }
}
#pragma warning restore
#endif