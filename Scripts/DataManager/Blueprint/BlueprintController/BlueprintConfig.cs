namespace DataManager.Blueprint.BlueprintController
{
    using System;
    using GameConfigs;
    using UnityEngine;

    /// <summary>
    /// Contains all the constants, the configuration of Blueprint control flow
    /// </summary>
    [Serializable]
    public class BlueprintConfig : ScriptableObject, IGameConfig
    {
        [SerializeField] private string currentBlueprintVersion = "1.0.0";
        [SerializeField] private string blueprintFileType       = ".csv";

        [Space] [Header("Blueprint Load Mode")] [SerializeField]
        private BlueprintSourceType source = BlueprintSourceType.Resource;

        [SerializeField] private BlueprintSourceType sourceFallback = BlueprintSourceType.Resource;

        [SerializeField] private string fetchBlueprintUri = "https://dqp03g2hg3.execute-api.ap-southeast-1.amazonaws.com/api/v1/projects/vampire-survivor-development/blueprints/{0}/info";

        [SerializeField] private string resourceBlueprintPath = "BlueprintData/";

        [SerializeField] private string addressableBlueprintPath = "v{0}/";

        private string persistentDataPath;

        public         string              FetchBlueprintUri        => string.Format(this.fetchBlueprintUri, this.currentBlueprintVersion);
        public         BlueprintSourceType Source                   => this.source;
        public         BlueprintSourceType SourceFallback           => this.sourceFallback;
        public virtual string              BlueprintZipFilepath     => $"{this.persistentDataPath}/Blueprints_v{this.currentBlueprintVersion}.zip";
        public         string              ResourceBlueprintPath    => this.resourceBlueprintPath;
        public         string              BlueprintFileType        => this.blueprintFileType;
        public         string              AddressableBlueprintPath => string.Format(this.addressableBlueprintPath, this.currentBlueprintVersion);
        public         string              CurrentBlueprintVersion  => this.currentBlueprintVersion;

        private void OnEnable() { this.persistentDataPath = Application.persistentDataPath; }

        public void SetCurrentBlueprintVersion(string version) { this.currentBlueprintVersion = version; }
    }

    public enum BlueprintSourceType
    {
        None,
        Resource,
        Addressable,
        Cloud
    }
}