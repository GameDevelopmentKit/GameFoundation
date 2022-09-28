#if !BESTHTTP_DISABLE_ALTERNATE_SSL && (!UNITY_WEBGL || UNITY_EDITOR)
#pragma warning disable
namespace BestHTTP.SecureProtocol.Org.BouncyCastle.Bcpg
{
    using System;
    using BestHTTP.SecureProtocol.Org.BouncyCastle.Math;

    /// <remarks>Base class for a DSA public key.</remarks>
    public class DsaPublicBcpgKey
        : BcpgObject, IBcpgKey
    {
        private readonly MPInteger p, q, g, y;

        /// <param name="bcpgIn">The stream to read the packet from.</param>
        public DsaPublicBcpgKey(
            BcpgInputStream bcpgIn)
        {
            this.p = new MPInteger(bcpgIn);
            this.q = new MPInteger(bcpgIn);
            this.g = new MPInteger(bcpgIn);
            this.y = new MPInteger(bcpgIn);
        }

        public DsaPublicBcpgKey(
            BigInteger p,
            BigInteger q,
            BigInteger g,
            BigInteger y)
        {
            this.p = new MPInteger(p);
            this.q = new MPInteger(q);
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

        public override void Encode(
            BcpgOutputStream bcpgOut)
        {
            bcpgOut.WriteObjects(this.p, this.q, this.g, this.y);
        }

        public BigInteger G => this.g.Value;

        public BigInteger P => this.p.Value;

        public BigInteger Q => this.q.Value;

        public BigInteger Y => this.y.Value;
    }
}
#pragma warning restore
#endif