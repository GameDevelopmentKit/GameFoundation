#if !BESTHTTP_DISABLE_ALTERNATE_SSL && (!UNITY_WEBGL || UNITY_EDITOR)
#pragma warning disable
namespace BestHTTP.SecureProtocol.Org.BouncyCastle.Tls
{
    using System.IO;
    using BestHTTP.SecureProtocol.Org.BouncyCastle.Tls.Crypto;
    using BestHTTP.SecureProtocol.Org.BouncyCastle.Utilities;

    public class DtlsVerifier
    {
        private static TlsMac CreateCookieMac(TlsCrypto crypto)
        {
            TlsMac mac = crypto.CreateHmac(MacAlgorithm.hmac_sha256);

            var secret = new byte[mac.MacLength];
            crypto.SecureRandom.NextBytes(secret);

            mac.SetKey(secret, 0, secret.Length);

            return mac;
        }

        private readonly TlsMac     m_cookieMac;
        private readonly TlsMacSink m_cookieMacSink;

        public DtlsVerifier(TlsCrypto crypto)
        {
            this.m_cookieMac     = CreateCookieMac(crypto);
            this.m_cookieMacSink = new TlsMacSink(this.m_cookieMac);
        }

        public virtual DtlsRequest VerifyRequest(byte[] clientID, byte[] data, int dataOff, int dataLen,
            DatagramSender sender)
        {
            lock (this)
            {
                var resetCookieMac = true;

                try
                {
                    this.m_cookieMac.Update(clientID, 0, clientID.Length);

                    var request = DtlsReliableHandshake.ReadClientRequest(data, dataOff, dataLen, this.m_cookieMacSink);
                    if (null != request)
                    {
                        var expectedCookie = this.m_cookieMac.CalculateMac();
                        resetCookieMac = false;

                        // TODO Consider stricter HelloVerifyRequest protocol
                        //switch (request.MessageSeq)
                        //{
                        //case 0:
                        //{
                        //    DtlsReliableHandshake.SendHelloVerifyRequest(sender, request.RecordSeq, expectedCookie);
                        //    break;
                        //}
                        //case 1:
                        //{
                        //    if (Arrays.ConstantTimeAreEqual(expectedCookie, request.ClientHello.Cookie))
                        //        return request;

                        //    break;
                        //}
                        //}

                        if (Arrays.ConstantTimeAreEqual(expectedCookie, request.ClientHello.Cookie))
                            return request;

                        DtlsReliableHandshake.SendHelloVerifyRequest(sender, request.RecordSeq, expectedCookie);
                    }
                }
                catch (IOException)
                {
                    // Ignore
                }
                finally
                {
                    if (resetCookieMac) this.m_cookieMac.Reset();
                }

                return null;
            }
        }
    }
}
#pragma warning restore
#endif