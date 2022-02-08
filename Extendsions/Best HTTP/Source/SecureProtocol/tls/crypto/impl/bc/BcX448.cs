#if !BESTHTTP_DISABLE_ALTERNATE_SSL && (!UNITY_WEBGL || UNITY_EDITOR)
#pragma warning disable
namespace BestHTTP.SecureProtocol.Org.BouncyCastle.Tls.Crypto.Impl.BC
{
    using System;
    using BestHTTP.SecureProtocol.Org.BouncyCastle.Math.EC.Rfc7748;

    /// <summary>Support class for X448 using the BC light-weight library.</summary>
    public class BcX448
        : TlsAgreement
    {
        protected readonly BcTlsCrypto m_crypto;
        protected readonly byte[]      m_privateKey    = new byte[X448.ScalarSize];
        protected readonly byte[]      m_peerPublicKey = new byte[X448.PointSize];

        public BcX448(BcTlsCrypto crypto) { this.m_crypto = crypto; }

        public virtual byte[] GenerateEphemeral()
        {
            this.m_crypto.SecureRandom.NextBytes(this.m_privateKey);

            var publicKey = new byte[X448.PointSize];
            X448.ScalarMultBase(this.m_privateKey, 0, publicKey, 0);
            return publicKey;
        }

        public virtual void ReceivePeerValue(byte[] peerValue)
        {
            if (peerValue == null || peerValue.Length != X448.PointSize)
                throw new TlsFatalAlert(AlertDescription.illegal_parameter);

            Array.Copy(peerValue, 0, this.m_peerPublicKey, 0, X448.PointSize);
        }

        public virtual TlsSecret CalculateSecret()
        {
            try
            {
                var secret = new byte[X448.PointSize];
                if (!X448.CalculateAgreement(this.m_privateKey, 0, this.m_peerPublicKey, 0, secret, 0))
                    throw new TlsFatalAlert(AlertDescription.handshake_failure);

                return this.m_crypto.AdoptLocalSecret(secret);
            }
            finally
            {
                Array.Clear(this.m_privateKey, 0, this.m_privateKey.Length);
                Array.Clear(this.m_peerPublicKey, 0, this.m_peerPublicKey.Length);
            }
        }
    }
}
#pragma warning restore
#endif