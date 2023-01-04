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
        public bool   isLoadFromResource;
        public string fetchBlueprintUri     = "https://dqp03g2hg3.execute-api.ap-southeast-1.amazonaws.com/api/v1/projects/vampire-survivor-development/blueprints/{0}/info";
        public string resourceBlueprintPath = "BlueprintData/";
        public string blueprintFileType     = ".csv";

        public virtual string BlueprintZipFilepath => $"{Application.persistentDataPath}/Blueprints_v{this.currentBlueprintVersion}.zip";
    }
}