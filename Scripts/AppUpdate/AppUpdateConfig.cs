namespace GameFoundation.Scripts.AppUpdate
{
    using System;
    using GameFoundation.Scripts.Utilities.ApplicationServices;

    [Serializable]
    public class AppUpdateConfig
    {
        public int SoftPromptCooldownHours = 24;

        public AppUpdatePlatformConfig Android;
        public AppUpdatePlatformConfig Ios;
        public AppUpdatePlatformConfig WebGL;
        public AppUpdatePlatformConfig Editor;

        public AppUpdatePlatformConfig GetPlatformConfig(ApplicationBuildPlatform platform)
        {
            return platform switch
            {
                ApplicationBuildPlatform.Android => this.Android,
                ApplicationBuildPlatform.Ios     => this.Ios,
                ApplicationBuildPlatform.WebGL   => this.WebGL,
                ApplicationBuildPlatform.Editor  => this.Editor,
                _                                => null,
            };
        }
    }

    [Serializable]
    public class AppUpdatePlatformConfig
    {
        public string MinVersion;
        public string MinBuild;
        public string LatestVersion;
        public string LatestBuild;
        public string StoreUrl;
    }
}
