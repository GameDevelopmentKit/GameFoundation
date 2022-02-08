#if !BESTHTTP_DISABLE_ALTERNATE_SSL && (!UNITY_WEBGL || UNITY_EDITOR)
#pragma warning disable
namespace BestHTTP.SecureProtocol.Org.BouncyCastle.Tls
{
    using System;
    using System.Collections;
    using System.IO;
    using BestHTTP.SecureProtocol.Org.BouncyCastle.Tls.Crypto;
    using BestHTTP.SecureProtocol.Org.BouncyCastle.Utilities;

    /// <summary>Buffers input until the hash algorithm is determined.</summary>
    internal sealed class DeferredHash
        : TlsHandshakeHash
    {
        private const int BufferingHashLimit = 4;

        private readonly TlsContext m_context;

        private          DigestInputBuffer m_buf;
        private readonly IDictionary       m_hashes;
        private          bool              m_forceBuffering;
        private          bool              m_sealed;

        internal DeferredHash(TlsContext context)
        {
            this.m_context        = context;
            this.m_buf            = new DigestInputBuffer();
            this.m_hashes         = Platform.CreateHashtable();
            this.m_forceBuffering = false;
            this.m_sealed         = false;
        }

        private DeferredHash(TlsContext context, IDictionary hashes)
        {
            this.m_context        = context;
            this.m_buf            = null;
            this.m_hashes         = hashes;
            this.m_forceBuffering = false;
            this.m_sealed         = true;
        }

        /// <exception cref="IOException" />
        public void CopyBufferTo(Stream output)
        {
            if (this.m_buf == null)
                // If you see this, you need to call forceBuffering() before SealHashAlgorithms()
                throw new InvalidOperationException("Not buffering");

            this.m_buf.CopyTo(output);
        }

        public void ForceBuffering()
        {
            if (this.m_sealed)
                throw new InvalidOperationException("Too late to force buffering");

            this.m_forceBuffering = true;
        }

        public void NotifyPrfDetermined()
        {
            var securityParameters = this.m_context.SecurityParameters;

            switch (securityParameters.PrfAlgorithm)
            {
                case PrfAlgorithm.ssl_prf_legacy:
                case PrfAlgorithm.tls_prf_legacy:
                {
                    this.CheckTrackingHash(CryptoHashAlgorithm.md5);
                    this.CheckTrackingHash(CryptoHashAlgorithm.sha1);
                    break;
                }
                default:
                {
                    this.CheckTrackingHash(securityParameters.PrfCryptoHashAlgorithm);
                    break;
                }
            }
        }

        public void TrackHashAlgorithm(int cryptoHashAlgorithm)
        {
            if (this.m_sealed)
                throw new InvalidOperationException("Too late to track more hash algorithms");

            this.CheckTrackingHash(cryptoHashAlgorithm);
        }

        public void SealHashAlgorithms()
        {
            if (this.m_sealed)
                throw new InvalidOperationException("Already sealed");

            this.m_sealed = true;
            this.CheckStopBuffering();
        }

        public TlsHandshakeHash StopTracking()
        {
            var securityParameters = this.m_context.SecurityParameters;

            var newHashes = Platform.CreateHashtable();
            switch (securityParameters.PrfAlgorithm)
            {
                case PrfAlgorithm.ssl_prf_legacy:
                case PrfAlgorithm.tls_prf_legacy:
                {
                    this.CloneHash(newHashes, HashAlgorithm.md5);
                    this.CloneHash(newHashes, HashAlgorithm.sha1);
                    break;
                }
                default:
                {
                    this.CloneHash(newHashes, securityParameters.PrfCryptoHashAlgorithm);
                    break;
                }
            }

            return new DeferredHash(this.m_context, newHashes);
        }

        public TlsHash ForkPrfHash()
        {
            this.CheckStopBuffering();

            var securityParameters = this.m_context.SecurityParameters;

            TlsHash prfHash;
            switch (securityParameters.PrfAlgorithm)
            {
                case PrfAlgorithm.ssl_prf_legacy:
                case PrfAlgorithm.tls_prf_legacy:
                {
                    prfHash = new CombinedHash(this.m_context, this.CloneHash(HashAlgorithm.md5), this.CloneHash(HashAlgorithm.sha1));
                    break;
                }
                default:
                {
                    prfHash = this.CloneHash(securityParameters.PrfCryptoHashAlgorithm);
                    break;
                }
            }

            if (this.m_buf != null) this.m_buf.UpdateDigest(prfHash);

            return prfHash;
        }

        public byte[] GetFinalHash(int cryptoHashAlgorithm)
        {
            var d = (TlsHash)this.m_hashes[cryptoHashAlgorithm];
            if (d == null)
                throw new InvalidOperationException("CryptoHashAlgorithm." + cryptoHashAlgorithm
                                                                           + " is not being tracked");

            this.CheckStopBuffering();

            d = d.CloneHash();
            if (this.m_buf != null) this.m_buf.UpdateDigest(d);

            return d.CalculateHash();
        }

        public void Update(byte[] input, int inOff, int len)
        {
            if (this.m_buf != null)
            {
                this.m_buf.Write(input, inOff, len);
                return;
            }

            foreach (TlsHash hash in this.m_hashes.Values) hash.Update(input, inOff, len);
        }

        public byte[] CalculateHash() { throw new InvalidOperationException("Use 'ForkPrfHash' to get a definite hash"); }

        public TlsHash CloneHash() { throw new InvalidOperationException("attempt to clone a DeferredHash"); }

        public void Reset()
        {
            if (this.m_buf != null)
            {
                this.m_buf.SetLength(0);
                return;
            }

            foreach (TlsHash hash in this.m_hashes.Values) hash.Reset();
        }

        private void CheckStopBuffering()
        {
            if (!this.m_forceBuffering && this.m_sealed && this.m_buf != null && this.m_hashes.Count <= BufferingHashLimit)
            {
                foreach (TlsHash hash in this.m_hashes.Values) this.m_buf.UpdateDigest(hash);

                this.m_buf = null;
            }
        }

        private void CheckTrackingHash(int cryptoHashAlgorithm)
        {
            if (!this.m_hashes.Contains(cryptoHashAlgorithm))
            {
                var hash = this.m_context.Crypto.CreateHash(cryptoHashAlgorithm);
                this.m_hashes[cryptoHashAlgorithm] = hash;
            }
        }

        private TlsHash CloneHash(int cryptoHashAlgorithm) { return ((TlsHash)this.m_hashes[cryptoHashAlgorithm]).CloneHash(); }

        private void CloneHash(IDictionary newHashes, int cryptoHashAlgorithm)
        {
            var hash = this.CloneHash(cryptoHashAlgorithm);
            if (this.m_buf != null) this.m_buf.UpdateDigest(hash);
            newHashes[cryptoHashAlgorithm] = hash;
        }
    }
}
#pragma warning restore
#endif