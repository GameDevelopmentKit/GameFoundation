#if !BESTHTTP_DISABLE_ALTERNATE_SSL && (!UNITY_WEBGL || UNITY_EDITOR)
#pragma warning disable
namespace BestHTTP.SecureProtocol.Org.BouncyCastle.Tls
{
    using System;
    using System.Collections;
    using BestHTTP.SecureProtocol.Org.BouncyCastle.Utilities;

    internal sealed class DtlsReassembler
    {
        private readonly byte[] m_body;

        private readonly IList m_missing = Platform.CreateArrayList();

        internal DtlsReassembler(short msg_type, int length)
        {
            this.MsgType = msg_type;
            this.m_body  = new byte[length];
            this.m_missing.Add(new Range(0, length));
        }

        internal short MsgType { get; }

        internal byte[] GetBodyIfComplete() { return this.m_missing.Count > 0 ? null : this.m_body; }

        internal void ContributeFragment(short msg_type, int length, byte[] buf, int off, int fragment_offset,
            int fragment_length)
        {
            var fragment_end = fragment_offset + fragment_length;

            if (this.MsgType != msg_type || this.m_body.Length != length || fragment_end > length)
                return;

            if (fragment_length == 0)
            {
                // NOTE: Empty messages still require an empty fragment to complete it
                if (fragment_offset == 0 && this.m_missing.Count > 0)
                {
                    var firstRange = (Range)this.m_missing[0];
                    if (firstRange.End == 0) this.m_missing.RemoveAt(0);
                }

                return;
            }

            for (var i = 0; i < this.m_missing.Count; ++i)
            {
                var range = (Range)this.m_missing[i];
                if (range.Start >= fragment_end)
                    break;

                if (range.End > fragment_offset)
                {
                    var copyStart  = Math.Max(range.Start, fragment_offset);
                    var copyEnd    = Math.Min(range.End, fragment_end);
                    var copyLength = copyEnd - copyStart;

                    Array.Copy(buf, off + copyStart - fragment_offset, this.m_body, copyStart, copyLength);

                    if (copyStart == range.Start)
                    {
                        if (copyEnd == range.End)
                            this.m_missing.RemoveAt(i--);
                        else
                            range.Start = copyEnd;
                    }
                    else
                    {
                        if (copyEnd != range.End) this.m_missing.Insert(++i, new Range(copyEnd, range.End));
                        range.End = copyStart;
                    }
                }
            }
        }

        internal void Reset()
        {
            this.m_missing.Clear();
            this.m_missing.Add(new Range(0, this.m_body.Length));
        }

        private sealed class Range
        {
            internal Range(int start, int end)
            {
                this.Start = start;
                this.End   = end;
            }

            public int Start { get; set; }

            public int End { get; set; }
        }
    }
}
#pragma warning restore
#endif