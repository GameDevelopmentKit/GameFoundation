#if !BESTHTTP_DISABLE_ALTERNATE_SSL && (!UNITY_WEBGL || UNITY_EDITOR)
#pragma warning disable
namespace BestHTTP.SecureProtocol.Org.BouncyCastle.Bcpg
{
    using System.IO;
    using BestHTTP.SecureProtocol.Org.BouncyCastle.Utilities;
    using BestHTTP.SecureProtocol.Org.BouncyCastle.Utilities.IO;

    /// <remarks>The string to key specifier class.</remarks>
    public class S2k
        : BcpgObject
    {
        private const int ExpBias = 6;

        public const int Simple                        = 0;
        public const int Salted                        = 1;
        public const int SaltedAndIterated             = 3;
        public const int GnuDummyS2K                   = 101;
        public const int GnuProtectionModeNoPrivateKey = 1;
        public const int GnuProtectionModeDivertToCard = 2;

        internal int              type;
        internal HashAlgorithmTag algorithm;
        internal byte[]           iv;
        internal int              itCount        = -1;
        internal int              protectionMode = -1;

        internal S2k(
            Stream inStr)
        {
            this.type      = inStr.ReadByte();
            this.algorithm = (HashAlgorithmTag)inStr.ReadByte();

            //
            // if this happens we have a dummy-S2k packet.
            //
            if (this.type != GnuDummyS2K)
            {
                if (this.type != 0)
                {
                    this.iv = new byte[8];
                    if (Streams.ReadFully(inStr, this.iv, 0, this.iv.Length) < this.iv.Length)
                        throw new EndOfStreamException();

                    if (this.type == 3) this.itCount = inStr.ReadByte();
                }
            }
            else
            {
                inStr.ReadByte(); // G
                inStr.ReadByte(); // N
                inStr.ReadByte(); // U
                this.protectionMode = inStr.ReadByte(); // protection mode
            }
        }

        public S2k(
            HashAlgorithmTag algorithm)
        {
            this.type      = 0;
            this.algorithm = algorithm;
        }

        public S2k(
            HashAlgorithmTag algorithm,
            byte[] iv)
        {
            this.type      = 1;
            this.algorithm = algorithm;
            this.iv        = iv;
        }

        public S2k(
            HashAlgorithmTag algorithm,
            byte[] iv,
            int itCount)
        {
            this.type      = 3;
            this.algorithm = algorithm;
            this.iv        = iv;
            this.itCount   = itCount;
        }

        public virtual int Type => this.type;

        /// <summary>The hash algorithm.</summary>
        public virtual HashAlgorithmTag HashAlgorithm => this.algorithm;

        /// <summary>The IV for the key generation algorithm.</summary>
        public virtual byte[] GetIV() { return Arrays.Clone(this.iv); }


        public long GetIterationCount() { return this.IterationCount; }

        /// <summary>The iteration count</summary>
        public virtual long IterationCount => (16 + (this.itCount & 15)) << ((this.itCount >> 4) + ExpBias);

        /// <summary>The protection mode - only if GnuDummyS2K</summary>
        public virtual int ProtectionMode => this.protectionMode;

        public override void Encode(
            BcpgOutputStream bcpgOut)
        {
            bcpgOut.WriteByte((byte)this.type);
            bcpgOut.WriteByte((byte)this.algorithm);

            if (this.type != GnuDummyS2K)
            {
                if (this.type != 0) bcpgOut.Write(this.iv);

                if (this.type == 3) bcpgOut.WriteByte((byte)this.itCount);
            }
            else
            {
                bcpgOut.WriteByte((byte)'G');
                bcpgOut.WriteByte((byte)'N');
                bcpgOut.WriteByte((byte)'U');
                bcpgOut.WriteByte((byte)this.protectionMode);
            }
        }
    }
}
#pragma warning restore
#endif