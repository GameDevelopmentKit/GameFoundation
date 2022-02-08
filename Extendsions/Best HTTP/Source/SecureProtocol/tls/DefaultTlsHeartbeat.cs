#if !BESTHTTP_DISABLE_ALTERNATE_SSL && (!UNITY_WEBGL || UNITY_EDITOR)
#pragma warning disable
namespace BestHTTP.SecureProtocol.Org.BouncyCastle.Tls
{
    using System;
    using BestHTTP.SecureProtocol.Org.BouncyCastle.Crypto.Utilities;

    public class DefaultTlsHeartbeat
        : TlsHeartbeat
    {
        private uint counter;

        public DefaultTlsHeartbeat(int idleMillis, int timeoutMillis)
        {
            if (idleMillis <= 0)
                throw new ArgumentException("must be > 0", "idleMillis");
            if (timeoutMillis <= 0)
                throw new ArgumentException("must be > 0", "timeoutMillis");

            this.IdleMillis    = idleMillis;
            this.TimeoutMillis = timeoutMillis;
        }

        public virtual byte[] GeneratePayload()
        {
            lock (this)
            {
                // NOTE: The counter naturally wraps back to 0
                return Pack.UInt32_To_BE(++this.counter);
            }
        }

        public virtual int IdleMillis { get; }

        public virtual int TimeoutMillis { get; }
    }
}
#pragma warning restore
#endif