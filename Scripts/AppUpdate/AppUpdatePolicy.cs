namespace GameFoundation.Scripts.AppUpdate
{
    using GameFoundation.Scripts.Utilities.ApplicationServices;

    public enum AppUpdateDecisionType
    {
        None,
        Optional,
        Force,
    }

    public class AppUpdateDecision
    {
        public AppUpdateDecisionType   Type;
        public AppUpdateConfig         Config;
        public AppUpdatePlatformConfig PlatformConfig;
        public ApplicationBuildInfo    BuildInfo;
        public string                  StoreUrl;
        public string                  Reason;

        public bool IsForceUpdate => this.Type == AppUpdateDecisionType.Force;
        public bool IsOptionalUpdate => this.Type == AppUpdateDecisionType.Optional;

        public static AppUpdateDecision None(AppUpdateConfig config, ApplicationBuildInfo buildInfo, string reason)
        {
            return new AppUpdateDecision
            {
                Type      = AppUpdateDecisionType.None,
                Config    = config,
                BuildInfo = buildInfo,
                Reason    = reason,
            };
        }
    }

    public class AppUpdatePolicy
    {
        public AppUpdateDecision Evaluate(AppUpdateConfig config, ApplicationBuildInfo buildInfo)
        {
            if (config == null)
            {
                return AppUpdateDecision.None(config, buildInfo, "Config unavailable.");
            }

            if (buildInfo == null)
            {
                return AppUpdateDecision.None(config, null, "Build info unavailable.");
            }

            var platformConfig = config.GetPlatformConfig(buildInfo.Platform);
            if (platformConfig == null)
            {
                return AppUpdateDecision.None(config, buildInfo, "Platform config unavailable.");
            }

            var isBelowMinimum = VersionComparer.IsBelow(buildInfo.Version, platformConfig.MinVersion) ||
                                 VersionComparer.IsBelow(buildInfo.BuildNumber, platformConfig.MinBuild);

            if (isBelowMinimum)
            {
                return this.CreateDecision(AppUpdateDecisionType.Force, config, platformConfig, buildInfo, "Below minimum supported version.");
            }

            var isBelowLatest = VersionComparer.IsBelow(buildInfo.Version, platformConfig.LatestVersion) ||
                                VersionComparer.IsBelow(buildInfo.BuildNumber, platformConfig.LatestBuild);

            if (isBelowLatest)
            {
                return this.CreateDecision(AppUpdateDecisionType.Optional, config, platformConfig, buildInfo, "Below latest recommended version.");
            }

            return AppUpdateDecision.None(config, buildInfo, "Already up to date.");
        }

        private AppUpdateDecision CreateDecision(
            AppUpdateDecisionType type,
            AppUpdateConfig config,
            AppUpdatePlatformConfig platformConfig,
            ApplicationBuildInfo buildInfo,
            string reason)
        {
            return new AppUpdateDecision
            {
                Type           = type,
                Config         = config,
                PlatformConfig = platformConfig,
                BuildInfo      = buildInfo,
                StoreUrl       = this.ResolveStoreUrl(platformConfig.StoreUrl, buildInfo),
                Reason         = reason,
            };
        }

        private string ResolveStoreUrl(string configuredStoreUrl, ApplicationBuildInfo buildInfo)
        {
            if (!string.IsNullOrWhiteSpace(configuredStoreUrl))
            {
                return configuredStoreUrl;
            }

            if (buildInfo.Platform == ApplicationBuildPlatform.Android && !string.IsNullOrWhiteSpace(buildInfo.PackageIdentifier))
            {
                return $"market://details?id={buildInfo.PackageIdentifier}";
            }

            return string.Empty;
        }
    }
}
