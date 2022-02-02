namespace GameFoundation.Scripts.BlueprintFlow.BlueprintControlFlow
{
    using UnityEngine;

    /// <summary>
    /// Contains all the constants, the configuration of Blueprint control flow
    /// </summary>
    public class BlueprintConfig
    {
        public readonly string CurrentBlueprintVersion = Application.version;

        private readonly string persistentDataPath = Application.persistentDataPath;
        
        private         string BlueprintZipFilepathFormat => $"{this.persistentDataPath}/{this.BlueprintZipFilename}";
        
        public readonly string BlueprintZipFilename = "Blueprints_v{0}.zip";
        
        public virtual string BlueprintZipFilepath => string.Format(this.BlueprintZipFilepathFormat, this.CurrentBlueprintVersion);
        
        public const string ResourceBlueprintPath = "BlueprintData/";
        
        public const string BlueprintFileType     = ".csv";
    }
}