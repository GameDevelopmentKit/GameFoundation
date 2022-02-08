#if !BESTHTTP_DISABLE_ALTERNATE_SSL && (!UNITY_WEBGL || UNITY_EDITOR)
#pragma warning disable
namespace BestHTTP.SecureProtocol.Org.BouncyCastle.Bcpg
{
	using System;
	using BestHTTP.SecureProtocol.Org.BouncyCastle.Math;

	/// <remarks>Base class for an RSA public key.</remarks>
    public class RsaPublicBcpgKey
        : BcpgObject, IBcpgKey
    {
        private readonly MPInteger n, e;

        /// <summary>Construct an RSA public key from the passed in stream.</summary>
        public RsaPublicBcpgKey(
            BcpgInputStream bcpgIn)
        {
            this.n = new MPInteger(bcpgIn);
            this.e = new MPInteger(bcpgIn);
        }

        /// <param name="n">The modulus.</param>
        /// <param name="e">The public exponent.</param>
        public RsaPublicBcpgKey(
            BigInteger n,
            BigInteger e)
        {
            this.n = new MPInteger(n);
            this.e = new MPInteger(e);
        }

        public BigInteger PublicExponent => this.e.Value;

        public BigInteger Modulus => this.n.Value;

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

        public override void Encode(
            BcpgOutputStream bcpgOut)
        {
            bcpgOut.WriteObjects(this.n, this.e);
        }
    }
}
#pragma warning restore
#endif