#if !BESTHTTP_DISABLE_ALTERNATE_SSL && (!UNITY_WEBGL || UNITY_EDITOR)
#pragma warning disable
namespace BestHTTP.SecureProtocol.Org.BouncyCastle.Tls
{
    using System;

    /**
     * RFC 4347 4.1.2.5 Anti-replay
     * <p>
     *     Support fast rejection of duplicate records by maintaining a sliding receive window
     * </p>
     */
    internal sealed class DtlsReplayWindow
    {
        private const long ValidSeqMask = 0x0000FFFFFFFFFFFFL;

        private const long WindowSize = 64L;

        private long  m_latestConfirmedSeq = -1;
        private ulong m_bitmap;

        /// <summary>
        ///     Check whether a received record with the given sequence number should be rejected as a duplicate.
        /// </summary>
        /// <param name="seq">the 48-bit DTLSPlainText.sequence_number field of a received record.</param>
        /// <returns>true if the record should be discarded without further processing.</returns>
        internal bool ShouldDiscard(long seq)
        {
            if ((seq & ValidSeqMask) != seq)
                return true;

            if (seq <= this.m_latestConfirmedSeq)
            {
                var diff = this.m_latestConfirmedSeq - seq;
                if (diff >= WindowSize)
                    return true;

                if ((this.m_bitmap & (1UL << (int)diff)) != 0)
                    return true;
            }

            return false;
        }

        /// <summary>
        ///     Report that a received record with the given sequence number passed authentication checks.
        /// </summary>
        /// <param name="seq">the 48-bit DTLSPlainText.sequence_number field of an authenticated record.</param>
        internal void ReportAuthenticated(long seq)
        {
            if ((seq & ValidSeqMask) != seq)
                throw new ArgumentException("out of range", "seq");

            if (seq <= this.m_latestConfirmedSeq)
            {
                var diff                             = this.m_latestConfirmedSeq - seq;
                if (diff < WindowSize) this.m_bitmap |= 1UL << (int)diff;
            }
            else
            {
                var diff = seq - this.m_latestConfirmedSeq;
                if (diff >= WindowSize)
                {
                    this.m_bitmap = 1;
                }
                else
                {
                    this.m_bitmap <<= (int)diff; // for earlier JDKs
                    this.m_bitmap |=  1UL;
                }

                this.m_latestConfirmedSeq = seq;
            }
        }

        internal void Reset(long seq)
        {
            if ((seq & ValidSeqMask) != seq)
                throw new ArgumentException("out of range", "seq");

            // Discard future records unless sequence number > 'seq'
            this.m_latestConfirmedSeq = seq;
            this.m_bitmap             = ulong.MaxValue >> (int)Math.Max(0, 63 - seq);
        }
    }
}
#pragma warning restore
#endif