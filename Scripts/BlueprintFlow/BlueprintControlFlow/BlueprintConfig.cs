namespace GameFoundation.Scripts.BlueprintFlow.BlueprintControlFlow
{
    using UnityEngine;

    /// <summary>
    /// Contains all the constants, the configuration of Blueprint control flow
    /// </summary>
    public static class BlueprintConfig
    {
        public static string CurrentBlueprintVersion = "v1.0";

        public static readonly string PersistentDataPath         = Application.persistentDataPath;
        public static readonly string BlueprintZipFilepathFormat = $"{PersistentDataPath}/{BlueprintZipFilename}";

        public const string BlueprintZipFilename = "Blueprints_{0}.zip";
        public static string BlueprintZipFilepath
#if TEST_BLUEPRINT
            => "Assets/Resources/BlueprintData/Blueprints_v1.0.zip";
#else
            => string.Format(BlueprintZipFilepathFormat, CurrentBlueprintVersion);
#endif

        public const string ResourceBlueprintPath = "BlueprintData/";
        public const string BlueprintFileType     = ".csv";
        public const string BlueprintVersionKey   = "bp_version";
    }
}