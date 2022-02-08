#if !BESTHTTP_DISABLE_ALTERNATE_SSL && (!UNITY_WEBGL || UNITY_EDITOR)
#pragma warning disable
namespace BestHTTP.SecureProtocol.Org.BouncyCastle.Bcpg
{
	using System;
	using BestHTTP.SecureProtocol.Org.BouncyCastle.Math;
	using BestHTTP.SecureProtocol.Org.BouncyCastle.Utilities;

	/// <remarks>Base class for an RSA secret (or priate) key.</remarks>
    public class RsaSecretBcpgKey
        : BcpgObject, IBcpgKey
    {
        private readonly MPInteger d, p, q, u;

        public RsaSecretBcpgKey(
            BcpgInputStream bcpgIn)
        {
            this.d = new MPInteger(bcpgIn);
            this.p = new MPInteger(bcpgIn);
            this.q = new MPInteger(bcpgIn);
            this.u = new MPInteger(bcpgIn);

            this.PrimeExponentP = this.d.Value.Remainder(this.p.Value.Subtract(BigInteger.One));
            this.PrimeExponentQ = this.d.Value.Remainder(this.q.Value.Subtract(BigInteger.One));
            this.CrtCoefficient = BigIntegers.ModOddInverse(this.p.Value, this.q.Value);
        }

        public RsaSecretBcpgKey(
            BigInteger d,
            BigInteger p,
            BigInteger q)
        {
            // PGP requires (p < q)
            var cmp = p.CompareTo(q);
            if (cmp >= 0)
            {
                if (cmp == 0)
                    throw new ArgumentException("p and q cannot be equal");

                var tmp = p;
                p = q;
                q = tmp;
            }

            this.d = new MPInteger(d);
            this.p = new MPInteger(p);
            this.q = new MPInteger(q);
            this.u = new MPInteger(BigIntegers.ModOddInverse(q, p));

            this.PrimeExponentP = d.Remainder(p.Subtract(BigInteger.One));
            this.PrimeExponentQ = d.Remainder(q.Subtract(BigInteger.One));
            this.CrtCoefficient = BigIntegers.ModOddInverse(p, q);
        }

        public BigInteger Modulus => this.p.Value.Multiply(this.q.Value);

        public BigInteger PrivateExponent => this.d.Value;

        public BigInteger PrimeP => this.p.Value;

        public BigInteger PrimeQ => this.q.Value;

        public BigInteger PrimeExponentP { get; }

        public BigInteger PrimeExponentQ { get; }

        public BigInteger CrtCoefficient { get; }

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
            bcpgOut.WriteObjects(this.d, this.p, this.q, this.u);
        }
    }
}
#pragma warning restore
#endif