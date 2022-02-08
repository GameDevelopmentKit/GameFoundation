#if !BESTHTTP_DISABLE_ALTERNATE_SSL && (!UNITY_WEBGL || UNITY_EDITOR)
#pragma warning disable
namespace BestHTTP.SecureProtocol.Org.BouncyCastle.Bcpg
{
    using System.IO;
    using BestHTTP.SecureProtocol.Org.BouncyCastle.Asn1;
    using BestHTTP.SecureProtocol.Org.BouncyCastle.Math;
    using BestHTTP.SecureProtocol.Org.BouncyCastle.Math.EC;

    /// <remarks>Base class for an EC Public Key.</remarks>
    public abstract class ECPublicBcpgKey
        : BcpgObject, IBcpgKey
    {
        internal DerObjectIdentifier oid;
        internal BigInteger          point;

        /// <param name="bcpgIn">The stream to read the packet from.</param>
        protected ECPublicBcpgKey(
            BcpgInputStream bcpgIn)
        {
            this.oid   = DerObjectIdentifier.GetInstance(Asn1Object.FromByteArray(ReadBytesOfEncodedLength(bcpgIn)));
            this.point = new MPInteger(bcpgIn).Value;
        }

        protected ECPublicBcpgKey(
            DerObjectIdentifier oid,
            ECPoint point)
        {
            this.point = new BigInteger(1, point.GetEncoded(false));
            this.oid   = oid;
        }

        protected ECPublicBcpgKey(
            DerObjectIdentifier oid,
            BigInteger encodedPoint)
        {
            this.point = encodedPoint;
            this.oid   = oid;
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
            catch (IOException)
            {
                return null;
            }
        }

        public override void Encode(
            BcpgOutputStream bcpgOut)
        {
            var oid = this.oid.GetEncoded();
            bcpgOut.Write(oid, 1, oid.Length - 1);

            var point = new MPInteger(this.point);
            bcpgOut.WriteObject(point);
        }

        public virtual BigInteger EncodedPoint => this.point;

        public virtual DerObjectIdentifier CurveOid => this.oid;

        protected static byte[] ReadBytesOfEncodedLength(
            BcpgInputStream bcpgIn)
        {
            var length = bcpgIn.ReadByte();
            if (length < 0)
                throw new EndOfStreamException();
            if (length == 0 || length == 0xFF)
                throw new IOException("future extensions not yet implemented");
            if (length > 127)
                throw new IOException("unsupported OID");

            var buffer = new byte[length + 2];
            bcpgIn.ReadFully(buffer, 2, buffer.Length - 2);
            buffer[0] = 0x06;
            buffer[1] = (byte)length;

            return buffer;
        }
    }
}
#pragma warning restore
#endif