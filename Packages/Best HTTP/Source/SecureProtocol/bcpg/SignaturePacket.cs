#if !BESTHTTP_DISABLE_ALTERNATE_SSL && (!UNITY_WEBGL || UNITY_EDITOR)
#pragma warning disable
namespace BestHTTP.SecureProtocol.Org.BouncyCastle.Bcpg
{
	using System;
	using System.IO;
	using BestHTTP.SecureProtocol.Org.BouncyCastle.Bcpg.Sig;
	using BestHTTP.SecureProtocol.Org.BouncyCastle.Utilities;
	using BestHTTP.SecureProtocol.Org.BouncyCastle.Utilities.Date;
	using BestHTTP.SecureProtocol.Org.BouncyCastle.Utilities.IO;

	/// <remarks>Generic signature packet.</remarks>
    public class SignaturePacket
        : ContainedPacket //, PublicKeyAlgorithmTag
    {
        private readonly MPInteger[]          signature;
        private readonly byte[]               fingerprint;
        private readonly SignatureSubpacket[] hashedData;
        private readonly SignatureSubpacket[] unhashedData;
        private readonly byte[]               signatureEncoding;

        internal SignaturePacket(
            BcpgInputStream bcpgIn)
        {
            this.Version = bcpgIn.ReadByte();

            if (this.Version == 3 || this.Version == 2)
            {
//                int l =
                bcpgIn.ReadByte();

                this.SignatureType = bcpgIn.ReadByte();
                this.CreationTime = (((long)bcpgIn.ReadByte() << 24) | ((long)bcpgIn.ReadByte() << 16)
                                                                     | ((long)bcpgIn.ReadByte() << 8) | (uint)bcpgIn.ReadByte()) * 1000L;

                this.KeyId |= (long)bcpgIn.ReadByte() << 56;
                this.KeyId |= (long)bcpgIn.ReadByte() << 48;
                this.KeyId |= (long)bcpgIn.ReadByte() << 40;
                this.KeyId |= (long)bcpgIn.ReadByte() << 32;
                this.KeyId |= (long)bcpgIn.ReadByte() << 24;
                this.KeyId |= (long)bcpgIn.ReadByte() << 16;
                this.KeyId |= (long)bcpgIn.ReadByte() << 8;
                this.KeyId |= (uint)bcpgIn.ReadByte();

                this.KeyAlgorithm  = (PublicKeyAlgorithmTag)bcpgIn.ReadByte();
                this.HashAlgorithm = (HashAlgorithmTag)bcpgIn.ReadByte();
            }
            else if (this.Version == 4)
            {
                this.SignatureType = bcpgIn.ReadByte();
                this.KeyAlgorithm  = (PublicKeyAlgorithmTag)bcpgIn.ReadByte();
                this.HashAlgorithm = (HashAlgorithmTag)bcpgIn.ReadByte();

                var hashedLength = (bcpgIn.ReadByte() << 8) | bcpgIn.ReadByte();
                var hashed       = new byte[hashedLength];

                bcpgIn.ReadFully(hashed);

                //
                // read the signature sub packet data.
                //
                var sIn = new SignatureSubpacketsParser(
                    new MemoryStream(hashed, false));

                var                v = Platform.CreateArrayList();
                SignatureSubpacket sub;
                while ((sub = sIn.ReadPacket()) != null) v.Add(sub);

                this.hashedData = new SignatureSubpacket[v.Count];

                for (var i = 0; i != this.hashedData.Length; i++)
                {
                    var p = (SignatureSubpacket)v[i];
                    if (p is IssuerKeyId)
                        this.KeyId = ((IssuerKeyId)p).KeyId;
                    else if (p is SignatureCreationTime)
                        this.CreationTime = DateTimeUtilities.DateTimeToUnixMs(
                            ((SignatureCreationTime)p).GetTime());

                    this.hashedData[i] = p;
                }

                var unhashedLength = (bcpgIn.ReadByte() << 8) | bcpgIn.ReadByte();
                var unhashed       = new byte[unhashedLength];

                bcpgIn.ReadFully(unhashed);

                sIn = new SignatureSubpacketsParser(new MemoryStream(unhashed, false));

                v.Clear();

                while ((sub = sIn.ReadPacket()) != null) v.Add(sub);

                this.unhashedData = new SignatureSubpacket[v.Count];

                for (var i = 0; i != this.unhashedData.Length; i++)
                {
                    var p                            = (SignatureSubpacket)v[i];
                    if (p is IssuerKeyId) this.KeyId = ((IssuerKeyId)p).KeyId;

                    this.unhashedData[i] = p;
                }
            }
            else
            {
                Streams.Drain(bcpgIn);

                throw new UnsupportedPacketVersionException("unsupported version: " + this.Version);
            }

            this.fingerprint = new byte[2];
            bcpgIn.ReadFully(this.fingerprint);

            switch (this.KeyAlgorithm)
            {
                case PublicKeyAlgorithmTag.RsaGeneral:
                case PublicKeyAlgorithmTag.RsaSign:
                    var v = new MPInteger(bcpgIn);
                    this.signature = new[] { v };
                    break;
                case PublicKeyAlgorithmTag.Dsa:
                    var r = new MPInteger(bcpgIn);
                    var s = new MPInteger(bcpgIn);
                    this.signature = new[] { r, s };
                    break;
                case PublicKeyAlgorithmTag.ElGamalEncrypt: // yep, this really does happen sometimes.
                case PublicKeyAlgorithmTag.ElGamalGeneral:
                    var p = new MPInteger(bcpgIn);
                    var g = new MPInteger(bcpgIn);
                    var y = new MPInteger(bcpgIn);
                    this.signature = new[] { p, g, y };
                    break;
                case PublicKeyAlgorithmTag.ECDsa:
                    var ecR = new MPInteger(bcpgIn);
                    var ecS = new MPInteger(bcpgIn);
                    this.signature = new[] { ecR, ecS };
                    break;
                default:
                    if (this.KeyAlgorithm >= PublicKeyAlgorithmTag.Experimental_1 && this.KeyAlgorithm <= PublicKeyAlgorithmTag.Experimental_11)
                    {
                        this.signature = null;
                        var bOut = new MemoryStream();
                        int ch;
                        while ((ch = bcpgIn.ReadByte()) >= 0) bOut.WriteByte((byte)ch);
                        this.signatureEncoding = bOut.ToArray();
                    }
                    else
                    {
                        throw new IOException("unknown signature key algorithm: " + this.KeyAlgorithm);
                    }

                    break;
            }
        }

        /**
         * Generate a version 4 signature packet.
         * 
         * @param signatureType
         * @param keyAlgorithm
         * @param hashAlgorithm
         * @param hashedData
         * @param unhashedData
         * @param fingerprint
         * @param signature
         */
        public SignaturePacket(
            int signatureType,
            long keyId,
            PublicKeyAlgorithmTag keyAlgorithm,
            HashAlgorithmTag hashAlgorithm,
            SignatureSubpacket[] hashedData,
            SignatureSubpacket[] unhashedData,
            byte[] fingerprint,
            MPInteger[] signature)
            : this(4, signatureType, keyId, keyAlgorithm, hashAlgorithm, hashedData, unhashedData, fingerprint, signature)
        {
        }

        /**
         * Generate a version 2/3 signature packet.
         * 
         * @param signatureType
         * @param keyAlgorithm
         * @param hashAlgorithm
         * @param fingerprint
         * @param signature
         */
        public SignaturePacket(
            int version,
            int signatureType,
            long keyId,
            PublicKeyAlgorithmTag keyAlgorithm,
            HashAlgorithmTag hashAlgorithm,
            long creationTime,
            byte[] fingerprint,
            MPInteger[] signature)
            : this(version, signatureType, keyId, keyAlgorithm, hashAlgorithm, null, null, fingerprint, signature)
        {
            this.CreationTime = creationTime;
        }

        public SignaturePacket(
            int version,
            int signatureType,
            long keyId,
            PublicKeyAlgorithmTag keyAlgorithm,
            HashAlgorithmTag hashAlgorithm,
            SignatureSubpacket[] hashedData,
            SignatureSubpacket[] unhashedData,
            byte[] fingerprint,
            MPInteger[] signature)
        {
            this.Version       = version;
            this.SignatureType = signatureType;
            this.KeyId         = keyId;
            this.KeyAlgorithm  = keyAlgorithm;
            this.HashAlgorithm = hashAlgorithm;
            this.hashedData    = hashedData;
            this.unhashedData  = unhashedData;
            this.fingerprint   = fingerprint;
            this.signature     = signature;

            if (hashedData != null) this.setCreationTime();
        }

        public int Version { get; }

        public int SignatureType { get; }

        /**
         * return the keyId
         * @return the keyId that created the signature.
         */
        public long KeyId { get; }

        /**
         * return the signature trailer that must be included with the data
         * to reconstruct the signature
         * 
         * @return byte[]
         */
        public byte[] GetSignatureTrailer()
        {
            byte[] trailer = null;

            if (this.Version == 3)
            {
                trailer = new byte[5];

                var time = this.CreationTime / 1000L;

                trailer[0] = (byte)this.SignatureType;
                trailer[1] = (byte)(time >> 24);
                trailer[2] = (byte)(time >> 16);
                trailer[3] = (byte)(time >> 8);
                trailer[4] = (byte)time;
            }
            else
            {
                var sOut = new MemoryStream();

                sOut.WriteByte((byte)this.Version);
                sOut.WriteByte((byte)this.SignatureType);
                sOut.WriteByte((byte)this.KeyAlgorithm);
                sOut.WriteByte((byte)this.HashAlgorithm);

                var hOut   = new MemoryStream();
                var hashed = this.GetHashedSubPackets();

                for (var i = 0; i != hashed.Length; i++) hashed[i].Encode(hOut);

                var data = hOut.ToArray();

                sOut.WriteByte((byte)(data.Length >> 8));
                sOut.WriteByte((byte)data.Length);
                sOut.Write(data, 0, data.Length);

                var hData = sOut.ToArray();

                sOut.WriteByte((byte)this.Version);
                sOut.WriteByte(0xff);
                sOut.WriteByte((byte)(hData.Length >> 24));
                sOut.WriteByte((byte)(hData.Length >> 16));
                sOut.WriteByte((byte)(hData.Length >> 8));
                sOut.WriteByte((byte)hData.Length);

                trailer = sOut.ToArray();
            }

            return trailer;
        }

        public PublicKeyAlgorithmTag KeyAlgorithm { get; }

        public HashAlgorithmTag HashAlgorithm { get; }

        /**
         * * return the signature as a set of integers - note this is normalised to be the
         * * ASN.1 encoding of what appears in the signature packet.
         */
        public MPInteger[] GetSignature() { return this.signature; }

        /**
         * Return the byte encoding of the signature section.
         * @return uninterpreted signature bytes.
         */
        public byte[] GetSignatureBytes()
        {
            if (this.signatureEncoding != null) return (byte[])this.signatureEncoding.Clone();

            var bOut  = new MemoryStream();
            var bcOut = new BcpgOutputStream(bOut);

            foreach (var sigObj in this.signature)
                try
                {
                    bcOut.WriteObject(sigObj);
                }
                catch (IOException e)
                {
                    throw new Exception("internal error: " + e);
                }

            return bOut.ToArray();
        }

        public SignatureSubpacket[] GetHashedSubPackets() { return this.hashedData; }

        public SignatureSubpacket[] GetUnhashedSubPackets() { return this.unhashedData; }

        /// <summary>Return the creation time in milliseconds since 1 Jan., 1970 UTC.</summary>
        public long CreationTime { get; private set; }

        public override void Encode(
            BcpgOutputStream bcpgOut)
        {
            var bOut = new MemoryStream();
            var pOut = new BcpgOutputStream(bOut);

            pOut.WriteByte((byte)this.Version);

            if (this.Version == 3 || this.Version == 2)
            {
                pOut.Write(
                    5, // the length of the next block
                    (byte)this.SignatureType);

                pOut.WriteInt((int)(this.CreationTime / 1000L));

                pOut.WriteLong(this.KeyId);

                pOut.Write(
                    (byte)this.KeyAlgorithm,
                    (byte)this.HashAlgorithm);
            }
            else if (this.Version == 4)
            {
                pOut.Write(
                    (byte)this.SignatureType,
                    (byte)this.KeyAlgorithm,
                    (byte)this.HashAlgorithm);

                EncodeLengthAndData(pOut, GetEncodedSubpackets(this.hashedData));

                EncodeLengthAndData(pOut, GetEncodedSubpackets(this.unhashedData));
            }
            else
            {
                throw new IOException("unknown version: " + this.Version);
            }

            pOut.Write(this.fingerprint);

            if (this.signature != null)
                pOut.WriteObjects(this.signature);
            else
                pOut.Write(this.signatureEncoding);

            bcpgOut.WritePacket(PacketTag.Signature, bOut.ToArray(), true);
        }

        private static void EncodeLengthAndData(
            BcpgOutputStream pOut,
            byte[] data)
        {
            pOut.WriteShort((short)data.Length);
            pOut.Write(data);
        }

        private static byte[] GetEncodedSubpackets(
            SignatureSubpacket[] ps)
        {
            var sOut = new MemoryStream();

            foreach (var p in ps) p.Encode(sOut);

            return sOut.ToArray();
        }

        private void setCreationTime()
        {
            foreach (var p in this.hashedData)
                if (p is SignatureCreationTime)
                {
                    this.CreationTime = DateTimeUtilities.DateTimeToUnixMs(
                        ((SignatureCreationTime)p).GetTime());
                    break;
                }
        }
        public static SignaturePacket FromByteArray(byte[] data)
        {
            var input = BcpgInputStream.Wrap(new MemoryStream(data));

            return new SignaturePacket(input);
        }
    }
}
#pragma warning restore
#endif