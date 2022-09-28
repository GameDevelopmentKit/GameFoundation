#if !BESTHTTP_DISABLE_ALTERNATE_SSL && (!UNITY_WEBGL || UNITY_EDITOR)
#pragma warning disable
namespace BestHTTP.SecureProtocol.Org.BouncyCastle.Tls.Crypto
{
    using BestHTTP.SecureProtocol.Org.BouncyCastle.Math;

    /// <summary>Carrier class for Diffie-Hellman group parameters.</summary>
    public class DHGroup
    {
        /// <summary>Base constructor with the prime factor of (p - 1).</summary>
        /// <param name="p">the prime modulus.</param>
        /// <param name="q">specifies the prime factor of (p - 1).</param>
        /// <param name="g">the base generator.</param>
        /// <param name="l"></param>
        public DHGroup(BigInteger p, BigInteger q, BigInteger g, int l)
        {
            this.P = p;
            this.G = g;
            this.Q = q;
            this.L = l;
        }

        public virtual BigInteger G { get; }

        public virtual int L { get; }

        public virtual BigInteger P { get; }

        public virtual BigInteger Q { get; }
    }
}
#pragma warning restore
#endif