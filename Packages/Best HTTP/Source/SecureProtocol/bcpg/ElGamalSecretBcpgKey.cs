#if !BESTHTTP_DISABLE_ALTERNATE_SSL && (!UNITY_WEBGL || UNITY_EDITOR)
#pragma warning disable
namespace BestHTTP.SecureProtocol.Org.BouncyCastle.Bcpg
{
	using System;
	using BestHTTP.SecureProtocol.Org.BouncyCastle.Math;

	/// <remarks>Base class for an ElGamal secret key.</remarks>
    public class ElGamalSecretBcpgKey
        : BcpgObject, IBcpgKey
    {
        internal MPInteger x;

        /**
		* @param in
		*/
        public ElGamalSecretBcpgKey(
            BcpgInputStream bcpgIn)
        {
            this.x = new MPInteger(bcpgIn);
        }

        /**
		* @param x
		*/
        public ElGamalSecretBcpgKey(
            BigInteger x)
        {
            this.x = new MPInteger(x);
        }

        /// <summary>The format, as a string, always "PGP".</summary>
        public string Format => "PGP";

        public BigInteger X => this.x.Value;

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
            bcpgOut.WriteObject(this.x);
        }
    }
}
#pragma warning restore
#endif