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
        [SerializeField] private string currentBlueprintVersion = "0.0.1";
        [SerializeField] private bool   isResourceMode          = true;
        [SerializeField] private string fetchBlueprintUri       = "https://dqp03g2hg3.execute-api.ap-southeast-1.amazonaws.com/api/v1/projects/vampire-survivor-development/blueprints/{0}/info";
        [SerializeField] private string resourceBlueprintPath   = "BlueprintData/";
        [SerializeField] private string blueprintFileType       = ".csv";

        private string persistentDataPath;

        public         string FetchBlueprintUri     => string.Format(this.fetchBlueprintUri, this.currentBlueprintVersion);
        public         bool   IsResourceMode        => this.isResourceMode;
        public virtual string BlueprintZipFilepath  => $"{this.persistentDataPath}/Blueprints_v{this.currentBlueprintVersion}.zip";
        public         string ResourceBlueprintPath => this.resourceBlueprintPath;
        public         string BlueprintFileType     => this.blueprintFileType;

        private void OnEnable()
        {
            this.persistentDataPath = Application.persistentDataPath;
        }
    }
}