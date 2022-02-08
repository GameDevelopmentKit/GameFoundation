#if !BESTHTTP_DISABLE_ALTERNATE_SSL && (!UNITY_WEBGL || UNITY_EDITOR)
#pragma warning disable
namespace BestHTTP.SecureProtocol.Org.BouncyCastle.Tls.Crypto.Impl
{
    using System;
    using System.IO;
    using BestHTTP.SecureProtocol.Org.BouncyCastle.Utilities;

    /// <summary>Base class for a TlsSecret implementation which captures common code and fields.</summary>
    public abstract class AbstractTlsSecret
        : TlsSecret
    {
        protected static byte[] CopyData(AbstractTlsSecret other) { return other.CopyData(); }

        protected byte[] m_data;

        /// <summary>Base constructor.</summary>
        /// <param name="data">the byte[] making up the secret value.</param>
        protected AbstractTlsSecret(byte[] data) { this.m_data = data; }

        protected virtual void CheckAlive()
        {
            if (this.m_data == null)
                throw new InvalidOperationException("Secret has already been extracted or destroyed");
        }

        protected abstract AbstractTlsCrypto Crypto { get; }

        public virtual byte[] CalculateHmac(int cryptoHashAlgorithm, byte[] buf, int off, int len)
        {
            lock (this)
            {
                this.CheckAlive();

                var hmac = this.Crypto.CreateHmacForHash(cryptoHashAlgorithm);
                hmac.SetKey(this.m_data, 0, this.m_data.Length);
                hmac.Update(buf, off, len);
                return hmac.CalculateMac();
            }
        }

        public abstract TlsSecret DeriveUsingPrf(int prfAlgorithm, string label, byte[] seed, int length);

        public virtual void Destroy()
        {
            lock (this)
            {
                if (this.m_data != null)
                {
                    // TODO Is there a way to ensure the data is really overwritten?
                    Array.Clear(this.m_data, 0, this.m_data.Length);
                    this.m_data = null;
                }
            }
        }

        /// <exception cref="IOException" />
        public virtual byte[] Encrypt(TlsEncryptor encryptor)
        {
            lock (this)
            {
                this.CheckAlive();

                return encryptor.Encrypt(this.m_data, 0, this.m_data.Length);
            }
        }

        public virtual byte[] Extract()
        {
            lock (this)
            {
                this.CheckAlive();

                var result = this.m_data;
                this.m_data = null;
                return result;
            }
        }

        public abstract TlsSecret HkdfExpand(int cryptoHashAlgorithm, byte[] info, int length);

        public abstract TlsSecret HkdfExtract(int cryptoHashAlgorithm, TlsSecret ikm);

        public virtual bool IsAlive()
        {
            lock (this)
            {
                return null != this.m_data;
            }
        }

        internal virtual byte[] CopyData()
        {
            lock (this)
            {
                return Arrays.Clone(this.m_data);
            }
        }
    }
}
#pragma warning restore
#endif