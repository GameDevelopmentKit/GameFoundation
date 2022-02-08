#if !BESTHTTP_DISABLE_ALTERNATE_SSL && (!UNITY_WEBGL || UNITY_EDITOR)
#pragma warning disable
namespace BestHTTP.SecureProtocol.Org.BouncyCastle.Bcpg
{
    using System.IO;

    /// <remarks>Base class for a PGP object.</remarks>
    public abstract class BcpgObject
    {
        public virtual byte[] GetEncoded()
        {
            var bOut = new MemoryStream();
            var pOut = new BcpgOutputStream(bOut);

            pOut.WriteObject(this);

            return bOut.ToArray();
        }

        public abstract void Encode(BcpgOutputStream bcpgOut);
    }
}

#pragma warning restore
#endif