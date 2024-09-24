namespace BlueprintFlow.BlueprintControlFlow
{
    using System;
    using Models;
    using UnityEngine;

    /// <summary>
    /// Contains all the constants, the configuration of Blueprint control flow
    /// </summary>
    [Serializable]
    public class BlueprintConfig : ScriptableObject, IGameConfig
    {
        public string currentBlueprintVersion = "0.0.1";
        public bool   isResourceMode          = true;
        public string fetchBlueprintUri       = "https://dqp03g2hg3.execute-api.ap-southeast-1.amazonaws.com/api/v1/projects/vampire-survivor-development/blueprints/{0}/info";
        public string resourceBlueprintPath   = "BlueprintData/";
        public string blueprintFileType       = ".csv";

        private string persistentDataPath;

        public         string FetchBlueprintUri     => string.Format(this.fetchBlueprintUri, this.currentBlueprintVersion);
        public         bool   IsResourceMode        => this.isResourceMode;
        public virtual string BlueprintZipFilepath  => $"{persistentDataPath}/Blueprints_v{this.currentBlueprintVersion}.zip";
        public         string ResourceBlueprintPath => this.resourceBlueprintPath;
        public         string BlueprintFileType     => this.blueprintFileType;

        private void OnEnable() { this.persistentDataPath = Application.persistentDataPath; }
    }
}