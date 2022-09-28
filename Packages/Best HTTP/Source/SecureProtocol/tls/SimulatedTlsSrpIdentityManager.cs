#if !BESTHTTP_DISABLE_ALTERNATE_SSL && (!UNITY_WEBGL || UNITY_EDITOR)
#pragma warning disable
namespace BestHTTP.SecureProtocol.Org.BouncyCastle.Tls
{
    using System.IO;
    using BestHTTP.SecureProtocol.Org.BouncyCastle.Tls.Crypto;
    using BestHTTP.SecureProtocol.Org.BouncyCastle.Utilities;

    /// <summary>
    ///     An implementation of <see cref="TlsSrpIdentityManager" /> that simulates the existence of "unknown"
    ///     identities to obscure the fact that there is no verifier for them.
    /// </summary>
    public class SimulatedTlsSrpIdentityManager
        : TlsSrpIdentityManager
    {
        private static readonly byte[] PrefixPassword = Strings.ToByteArray("password");
        private static readonly byte[] PrefixSalt     = Strings.ToByteArray("salt");

        /// <summary>
        ///     Create a <see cref="SimulatedTlsSrpIdentityManager" /> that implements the algorithm from RFC 5054
        ///     2.5.1.3.
        /// </summary>
        /// <param name="crypto">
        ///     <see cref="TlsCrypto" />
        /// </param>
        /// <param name="group">the <see cref="Srp6Group" /> defining the group that SRP is operating in.</param>
        /// <param name="seedKey">the secret "seed key" referred to in RFC 5054 2.5.1.3.</param>
        /// <returns>an instance of <see cref="SimulatedTlsSrpIdentityManager" />.</returns>
        /// <exception cref="IOException" />
        public static SimulatedTlsSrpIdentityManager GetRfc5054Default(TlsCrypto crypto, Srp6Group group, byte[] seedKey)
        {
            TlsMac mac = crypto.CreateHmac(MacAlgorithm.hmac_sha1);

            mac.SetKey(seedKey, 0, seedKey.Length);

            var srpConfig = new TlsSrpConfig();

            srpConfig.SetExplicitNG(new[] { group.N, group.G });

            return new SimulatedTlsSrpIdentityManager(group, crypto.CreateSrp6VerifierGenerator(srpConfig), mac);
        }

        protected readonly Srp6Group                m_group;
        protected readonly TlsSrp6VerifierGenerator m_verifierGenerator;
        protected readonly TlsMac                   m_mac;

        public SimulatedTlsSrpIdentityManager(Srp6Group group, TlsSrp6VerifierGenerator verifierGenerator, TlsMac mac)
        {
            this.m_group             = group;
            this.m_verifierGenerator = verifierGenerator;
            this.m_mac               = mac;
        }

        public virtual TlsSrpLoginParameters GetLoginParameters(byte[] identity)
        {
            this.m_mac.Update(PrefixSalt, 0, PrefixSalt.Length);
            this.m_mac.Update(identity, 0, identity.Length);

            var salt = this.m_mac.CalculateMac();

            this.m_mac.Update(PrefixPassword, 0, PrefixPassword.Length);
            this.m_mac.Update(identity, 0, identity.Length);

            var password = this.m_mac.CalculateMac();

            var verifier = this.m_verifierGenerator.GenerateVerifier(salt, identity, password);

            var srpConfig = new TlsSrpConfig();
            srpConfig.SetExplicitNG(new[] { this.m_group.N, this.m_group.G });

            return new TlsSrpLoginParameters(identity, srpConfig, verifier, salt);
        }
    }
}
#pragma warning restore
#endif