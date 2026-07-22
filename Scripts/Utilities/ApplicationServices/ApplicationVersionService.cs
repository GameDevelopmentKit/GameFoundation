namespace GameFoundation.Scripts.Utilities.ApplicationServices
{
    public class ApplicationVersionService
    {
        private readonly IApplicationBuildInfoProvider buildInfoProvider;

        private string environment        = string.Empty;
        private string blueprintVersion   = string.Empty;
        private string assetBundleVersion = string.Empty;

        public string Environment        => this.environment;
        public string BlueprintVersion   => this.blueprintVersion;
        public string AssetBundleVersion => this.assetBundleVersion;

        public ApplicationVersionService(IApplicationBuildInfoProvider buildInfoProvider)
        {
            this.buildInfoProvider = buildInfoProvider;
        }

        public void SetAssetBundleVersion(string version)
        {
            this.assetBundleVersion = version;
        }
        
        public void SetBlueprintVersion(string version)
        {
            this.blueprintVersion = version;
        }

        public void SetEnvironment(string environment)
        {
            this.environment = environment;
        }
        
        public void SetGameInfo(string environment, string blueprintVersion, string assetBundleVersion)
        {
            this.environment        = environment ?? string.Empty;
            this.blueprintVersion   = blueprintVersion ?? string.Empty;
            this.assetBundleVersion = assetBundleVersion ?? string.Empty;
        }

        public string GetFullVersionText()
        {
            var buildInfo         = this.buildInfoProvider.GetBuildInfo() ?? new ApplicationBuildInfo();
            var version           = string.IsNullOrWhiteSpace(buildInfo.Version) ? "Unknown" : buildInfo.Version;
            var versionText       = string.IsNullOrWhiteSpace(buildInfo.BuildNumber) ? version : $"{version} ({buildInfo.BuildNumber})";
            var environmentSuffix = this.GetEnvironmentSuffix();
            var blueprintSuffix   = string.IsNullOrWhiteSpace(this.blueprintVersion) ? string.Empty : $" (bp v{this.blueprintVersion})";

            return $"Version {versionText}{environmentSuffix}{blueprintSuffix}";
        }

        private string GetEnvironmentSuffix()
        {
            if (string.IsNullOrWhiteSpace(this.environment))
            {
                return string.Empty;
            }

            var normalizedEnvironment = this.environment.Trim();
            var shortEnvironment = normalizedEnvironment.Length >= 3
                ? normalizedEnvironment.Substring(0, 3).ToLower()
                : normalizedEnvironment.ToLower();

            return $"-{shortEnvironment}";
        }
    }
}
