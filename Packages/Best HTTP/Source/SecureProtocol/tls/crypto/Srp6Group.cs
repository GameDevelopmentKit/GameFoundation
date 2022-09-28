#if !BESTHTTP_DISABLE_ALTERNATE_SSL && (!UNITY_WEBGL || UNITY_EDITOR)
#pragma warning disable
namespace BestHTTP.SecureProtocol.Org.BouncyCastle.Tls.Crypto
{
    using BestHTTP.SecureProtocol.Org.BouncyCastle.Math;

    /// <summary>Carrier class for SRP-6 group parameters.</summary>
    public class Srp6Group
    {
        /// <summary>Base constructor.</summary>
        /// <param name="n">the n value.</param>
        /// <param name="g">the g value.</param>
        public Srp6Group(BigInteger n, BigInteger g)
        {
            this.N = n;
            this.G = g;
        }

        public virtual BigInteger G { get; }

        public virtual BigInteger N { get; }
    }
}
#pragma warning restore
#endif