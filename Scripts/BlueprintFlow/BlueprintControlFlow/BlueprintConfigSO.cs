namespace BlueprintFlow.BlueprintControlFlow
{
    using System;
    using Models;
    using Sirenix.OdinInspector;
    using UnityEngine;

    /// <summary>
    /// Contains all the constants, the configuration of Blueprint control flow
    /// </summary>
    public class BlueprintConfigSO : SerializedScriptableObject, IGameConfig
    {
        public BlueprintConfig blueprintConfig = new();

        private void OnEnable() { this.blueprintConfig.PersistentDataPath = Application.persistentDataPath; }

        public BlueprintConfig CloneType() => this.blueprintConfig.CloneType();
    }

    [Serializable]
    public class BlueprintConfig
    {
        public string currentBlueprintVersion = "0.0.1";
        public bool   isResourceMode          = true;
        public string fetchBlueprintUri       = "https://dqp03g2hg3.execute-api.ap-southeast-1.amazonaws.com/api/v1/projects/vampire-survivor-development/blueprints/{0}/info";
        public string resourceBlueprintPath   = "BlueprintData/";
        public string blueprintFileType       = ".csv";

        public string PersistentDataPath { get; set; }

        public         string FetchBlueprintUri     => string.Format(this.fetchBlueprintUri, this.currentBlueprintVersion);
        public         bool   IsResourceMode        => this.isResourceMode;
        public virtual string BlueprintZipFilepath  => $"{this.PersistentDataPath}/Blueprints_v{this.currentBlueprintVersion}.zip";
        public         string ResourceBlueprintPath => this.resourceBlueprintPath;
        public         string BlueprintFileType     => this.blueprintFileType;

        public BlueprintConfig CloneType() => new ()
        {
            currentBlueprintVersion = this.currentBlueprintVersion,
            isResourceMode          = this.isResourceMode,
            fetchBlueprintUri       = this.fetchBlueprintUri,
            resourceBlueprintPath   = this.resourceBlueprintPath,
            blueprintFileType       = this.blueprintFileType,
            PersistentDataPath      = this.PersistentDataPath,
        };
    }
}