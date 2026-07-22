namespace GameFoundation.Scripts.AppUpdate.Tests.Editor
{
    using GameFoundation.Scripts.Utilities;
    using GameFoundation.Scripts.Utilities.ApplicationServices;
    using NUnit.Framework;

    [TestFixture]
    public class AppUpdatePolicyTests
    {
        [Test]
        public void Evaluate_ReturnsForce_WhenCurrentVersionBelowMinimum()
        {
            var decision = new AppUpdatePolicy().Evaluate(
                new AppUpdateConfig
                {
                    Android = new AppUpdatePlatformConfig
                    {
                        MinVersion = "1.2.0",
                        LatestVersion = "1.3.0",
                        StoreUrl = "market://details?id=test"
                    }
                },
                BuildInfo("1.1.9", "100"));

            Assert.AreEqual(AppUpdateDecisionType.Force, decision.Type);
            Assert.AreEqual("market://details?id=test", decision.StoreUrl);
        }

        [Test]
        public void Evaluate_ReturnsOptional_WhenCurrentVersionBelowLatestOnly()
        {
            var decision = new AppUpdatePolicy().Evaluate(
                new AppUpdateConfig
                {
                    Android = new AppUpdatePlatformConfig
                    {
                        MinVersion = "1.2.0",
                        LatestVersion = "1.3.0",
                        StoreUrl = "market://details?id=test"
                    }
                },
                BuildInfo("1.2.5", "125"));

            Assert.AreEqual(AppUpdateDecisionType.Optional, decision.Type);
        }

        [Test]
        public void Evaluate_ReturnsNone_WhenConfigUnavailable()
        {
            var decision = new AppUpdatePolicy().Evaluate(
                null,
                BuildInfo("1.0.0", "100"));

            Assert.AreEqual(AppUpdateDecisionType.None, decision.Type);
        }

        [Test]
        public void Evaluate_ReturnsNone_WhenPlatformConfigMissing()
        {
            var decision = new AppUpdatePolicy().Evaluate(
                new AppUpdateConfig(),
                BuildInfo("1.0.0", "100"));

            Assert.AreEqual(AppUpdateDecisionType.None, decision.Type);
        }

        [Test]
        public void Evaluate_ReturnsNone_WhenBuildInfoUnavailable()
        {
            var decision = new AppUpdatePolicy().Evaluate(
                new AppUpdateConfig { Android = new AppUpdatePlatformConfig { MinVersion = "1.0.0" } },
                null);

            Assert.AreEqual(AppUpdateDecisionType.None, decision.Type);
        }

        [Test]
        public void Evaluate_ReturnsNone_WhenNoThresholdConfigured()
        {
            var decision = new AppUpdatePolicy().Evaluate(
                new AppUpdateConfig { Android = new AppUpdatePlatformConfig() },
                BuildInfo("1.0.0", "100"));

            Assert.AreEqual(AppUpdateDecisionType.None, decision.Type);
        }

        [Test]
        public void Compare_HandlesNumericBuildNumbers()
        {
            Assert.Less(VersionComparer.Compare("99", "100"), 0);
            Assert.Greater(VersionComparer.Compare("101", "100"), 0);
            Assert.AreEqual(0, VersionComparer.Compare("100", "100"));
        }

        private static ApplicationBuildInfo BuildInfo(string version, string buildNumber)
        {
            return new ApplicationBuildInfo
            {
                Platform = ApplicationBuildPlatform.Android,
                Version = version,
                BuildNumber = buildNumber,
                PackageIdentifier = "test"
            };
        }
    }
}
