#if !BESTHTTP_DISABLE_ALTERNATE_SSL && (!UNITY_WEBGL || UNITY_EDITOR)
#pragma warning disable
namespace BestHTTP.SecureProtocol.Org.BouncyCastle.Tls
{
    using System;
    using System.IO;
    using BestHTTP.SecureProtocol.Org.BouncyCastle.Tls.Crypto;

    internal sealed class DtlsEpoch
    {
        private long m_sequenceNumber;

        internal DtlsEpoch(int epoch, TlsCipher cipher)
        {
            if (epoch < 0)
                throw new ArgumentException("must be >= 0", "epoch");
            if (cipher == null)
                throw new ArgumentNullException("cipher");

            this.Epoch  = epoch;
            this.Cipher = cipher;
        }

        /// <exception cref="IOException" />
        internal long AllocateSequenceNumber()
        {
            lock (this)
            {
                if (this.m_sequenceNumber >= 1L << 48)
                    throw new TlsFatalAlert(AlertDescription.internal_error);

                return this.m_sequenceNumber++;
            }
        }

        internal TlsCipher Cipher { get; }

        internal int Epoch { get; }

        internal DtlsReplayWindow ReplayWindow { get; } = new();

        internal long SequenceNumber
        {
            get
            {
                lock (this)
                {
                    return this.m_sequenceNumber;
                }
            }
            set
            {
                lock (this)
                {
                    this.m_sequenceNumber = value;
                }
            }
        }
    }
}
#pragma warning restore
#endif