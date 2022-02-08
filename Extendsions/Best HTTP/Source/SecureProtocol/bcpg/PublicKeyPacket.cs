#if !BESTHTTP_DISABLE_ALTERNATE_SSL && (!UNITY_WEBGL || UNITY_EDITOR)
#pragma warning disable
namespace BestHTTP.SecureProtocol.Org.BouncyCastle.Bcpg
{
    using System;
    using System.IO;
    using BestHTTP.SecureProtocol.Org.BouncyCastle.Utilities.Date;

    /// <remarks>Basic packet for a PGP public key.</remarks>
    public class PublicKeyPacket
        : ContainedPacket //, PublicKeyAlgorithmTag
    {
        private readonly int                   version;
        private readonly long                  time;
        private readonly int                   validDays;
        private readonly PublicKeyAlgorithmTag algorithm;
        private readonly IBcpgKey              key;

        internal PublicKeyPacket(
            BcpgInputStream bcpgIn)
        {
            this.version = bcpgIn.ReadByte();

            this.time = ((uint)bcpgIn.ReadByte() << 24) | ((uint)bcpgIn.ReadByte() << 16)
                                                        | ((uint)bcpgIn.ReadByte() << 8) | (uint)bcpgIn.ReadByte();

            if (this.version <= 3) this.validDays = (bcpgIn.ReadByte() << 8) | bcpgIn.ReadByte();

            this.algorithm = (PublicKeyAlgorithmTag)bcpgIn.ReadByte();

            switch (this.algorithm)
            {
                case PublicKeyAlgorithmTag.RsaEncrypt:
                case PublicKeyAlgorithmTag.RsaGeneral:
                case PublicKeyAlgorithmTag.RsaSign:
                    this.key = new RsaPublicBcpgKey(bcpgIn);
                    break;
                case PublicKeyAlgorithmTag.Dsa:
                    this.key = new DsaPublicBcpgKey(bcpgIn);
                    break;
                case PublicKeyAlgorithmTag.ElGamalEncrypt:
                case PublicKeyAlgorithmTag.ElGamalGeneral:
                    this.key = new ElGamalPublicBcpgKey(bcpgIn);
                    break;
                case PublicKeyAlgorithmTag.ECDH:
                    this.key = new ECDHPublicBcpgKey(bcpgIn);
                    break;
                case PublicKeyAlgorithmTag.ECDsa:
                    this.key = new ECDsaPublicBcpgKey(bcpgIn);
                    break;
                default:
                    throw new IOException("unknown PGP public key algorithm encountered");
            }
        }

        /// <summary>Construct a version 4 public key packet.</summary>
        public PublicKeyPacket(
            PublicKeyAlgorithmTag algorithm,
            DateTime time,
            IBcpgKey key)
        {
            this.version   = 4;
            this.time      = DateTimeUtilities.DateTimeToUnixMs(time) / 1000L;
            this.algorithm = algorithm;
            this.key       = key;
        }

        public virtual int Version => this.version;

        public virtual PublicKeyAlgorithmTag Algorithm => this.algorithm;

        public virtual int ValidDays => this.validDays;

        public virtual DateTime GetTime() { return DateTimeUtilities.UnixMsToDateTime(this.time * 1000L); }

        public virtual IBcpgKey Key => this.key;

        public virtual byte[] GetEncodedContents()
        {
            var bOut = new MemoryStream();
            var pOut = new BcpgOutputStream(bOut);

            pOut.WriteByte((byte)this.version);
            pOut.WriteInt((int)this.time);

            if (this.version <= 3) pOut.WriteShort((short)this.validDays);

            pOut.WriteByte((byte)this.algorithm);

            pOut.WriteObject((BcpgObject)this.key);

            return bOut.ToArray();
        }

        public override void Encode(
            BcpgOutputStream bcpgOut)
        {
            bcpgOut.WritePacket(PacketTag.PublicKey, this.GetEncodedContents(), true);
        }
    }
}
#pragma warning restore
#endif