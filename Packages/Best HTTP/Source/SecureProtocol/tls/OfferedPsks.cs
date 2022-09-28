#if !BESTHTTP_DISABLE_ALTERNATE_SSL && (!UNITY_WEBGL || UNITY_EDITOR)
#pragma warning disable
namespace BestHTTP.SecureProtocol.Org.BouncyCastle.Tls
{
    using System;
    using System.Collections;
    using System.IO;
    using BestHTTP.SecureProtocol.Org.BouncyCastle.Tls.Crypto;
    using BestHTTP.SecureProtocol.Org.BouncyCastle.Utilities;

    public sealed class OfferedPsks
    {
        internal class BindersConfig
        {
            internal readonly TlsPsk[]    m_psks;
            internal readonly short[]     m_pskKeyExchangeModes;
            internal readonly TlsSecret[] m_earlySecrets;
            internal          int         m_bindersSize;

            internal BindersConfig(TlsPsk[] psks, short[] pskKeyExchangeModes, TlsSecret[] earlySecrets,
                int bindersSize)
            {
                this.m_psks                = psks;
                this.m_pskKeyExchangeModes = pskKeyExchangeModes;
                this.m_earlySecrets        = earlySecrets;
                this.m_bindersSize         = bindersSize;
            }
        }

        internal class SelectedConfig
        {
            internal readonly int       m_index;
            internal readonly TlsPsk    m_psk;
            internal readonly short[]   m_pskKeyExchangeModes;
            internal readonly TlsSecret m_earlySecret;

            internal SelectedConfig(int index, TlsPsk psk, short[] pskKeyExchangeModes, TlsSecret earlySecret)
            {
                this.m_index               = index;
                this.m_psk                 = psk;
                this.m_pskKeyExchangeModes = pskKeyExchangeModes;
                this.m_earlySecret         = earlySecret;
            }
        }

        public OfferedPsks(IList identities)
            : this(identities, null, -1)
        {
        }

        private OfferedPsks(IList identities, IList binders, int bindersSize)
        {
            if (null == identities || identities.Count < 1)
                throw new ArgumentException("cannot be null or empty", "identities");
            if (null != binders && identities.Count != binders.Count)
                throw new ArgumentException("must be the same length as 'identities' (or null)", "binders");
            if (null != binders != bindersSize >= 0)
                throw new ArgumentException("must be >= 0 iff 'binders' are present", "bindersSize");

            this.Identities  = identities;
            this.Binders     = binders;
            this.BindersSize = bindersSize;
        }

        public IList Binders { get; }

        public int BindersSize { get; }

        public IList Identities { get; }

        public int GetIndexOfIdentity(PskIdentity pskIdentity)
        {
            for (int i = 0, count = this.Identities.Count; i < count; ++i)
                if (pskIdentity.Equals(this.Identities[i]))
                    return i;
            return -1;
        }

        /// <exception cref="IOException" />
        public void Encode(Stream output)
        {
            // identities
            {
                var lengthOfIdentitiesList                                               = 0;
                foreach (PskIdentity identity in this.Identities) lengthOfIdentitiesList += identity.GetEncodedLength();

                TlsUtilities.CheckUint16(lengthOfIdentitiesList);
                TlsUtilities.WriteUint16(lengthOfIdentitiesList, output);

                foreach (PskIdentity identity in this.Identities) identity.Encode(output);
            }

            // binders
            if (null != this.Binders)
            {
                var lengthOfBindersList                                     = 0;
                foreach (byte[] binder in this.Binders) lengthOfBindersList += 1 + binder.Length;

                TlsUtilities.CheckUint16(lengthOfBindersList);
                TlsUtilities.WriteUint16(lengthOfBindersList, output);

                foreach (byte[] binder in this.Binders) TlsUtilities.WriteOpaque8(binder, output);
            }
        }

        /// <exception cref="IOException" />
        internal static void EncodeBinders(Stream output, TlsCrypto crypto, TlsHandshakeHash handshakeHash,
            BindersConfig bindersConfig)
        {
            var psks                        = bindersConfig.m_psks;
            var earlySecrets                = bindersConfig.m_earlySecrets;
            var expectedLengthOfBindersList = bindersConfig.m_bindersSize - 2;

            TlsUtilities.CheckUint16(expectedLengthOfBindersList);
            TlsUtilities.WriteUint16(expectedLengthOfBindersList, output);

            var lengthOfBindersList = 0;
            for (var i = 0; i < psks.Length; ++i)
            {
                var psk         = psks[i];
                var earlySecret = earlySecrets[i];

                // TODO[tls13-psk] Handle resumption PSKs
                var isExternalPsk          = true;
                var pskCryptoHashAlgorithm = TlsCryptoUtilities.GetHashForPrf(psk.PrfAlgorithm);

                // TODO[tls13-psk] Cache the transcript hashes per algorithm to avoid duplicates for multiple PSKs
                var hash = crypto.CreateHash(pskCryptoHashAlgorithm);
                handshakeHash.CopyBufferTo(new TlsHashSink(hash));
                var transcriptHash = hash.CalculateHash();

                var binder = TlsUtilities.CalculatePskBinder(crypto, isExternalPsk, pskCryptoHashAlgorithm,
                    earlySecret, transcriptHash);

                lengthOfBindersList += 1 + binder.Length;
                TlsUtilities.WriteOpaque8(binder, output);
            }

            if (expectedLengthOfBindersList != lengthOfBindersList)
                throw new TlsFatalAlert(AlertDescription.internal_error);
        }

        /// <exception cref="IOException" />
        internal static int GetBindersSize(TlsPsk[] psks)
        {
            var lengthOfBindersList = 0;
            for (var i = 0; i < psks.Length; ++i)
            {
                var psk = psks[i];

                var prfAlgorithm           = psk.PrfAlgorithm;
                var prfCryptoHashAlgorithm = TlsCryptoUtilities.GetHashForPrf(prfAlgorithm);

                lengthOfBindersList += 1 + TlsCryptoUtilities.GetHashOutputSize(prfCryptoHashAlgorithm);
            }

            TlsUtilities.CheckUint16(lengthOfBindersList);
            return 2 + lengthOfBindersList;
        }

        /// <exception cref="IOException" />
        public static OfferedPsks Parse(Stream input)
        {
            var identities = Platform.CreateArrayList();
            {
                var totalLengthIdentities = TlsUtilities.ReadUint16(input);
                if (totalLengthIdentities < 7)
                    throw new TlsFatalAlert(AlertDescription.decode_error);

                var identitiesData = TlsUtilities.ReadFully(totalLengthIdentities, input);
                var buf            = new MemoryStream(identitiesData, false);
                do
                {
                    var identity = PskIdentity.Parse(buf);
                    identities.Add(identity);
                } while (buf.Position < buf.Length);
            }

            var binders            = Platform.CreateArrayList();
            var totalLengthBinders = TlsUtilities.ReadUint16(input);
            {
                if (totalLengthBinders < 33)
                    throw new TlsFatalAlert(AlertDescription.decode_error);

                var bindersData = TlsUtilities.ReadFully(totalLengthBinders, input);
                var buf         = new MemoryStream(bindersData, false);
                do
                {
                    var binder = TlsUtilities.ReadOpaque8(buf, 32);
                    binders.Add(binder);
                } while (buf.Position < buf.Length);
            }

            return new OfferedPsks(identities, binders, 2 + totalLengthBinders);
        }
    }
}
#pragma warning restore
#endif