namespace GameFoundation.Scripts.BlueprintFlow.BlueprintControlFlow
{
    using UnityEngine;

    public class DebugBlueprintConfig : BlueprintConfig
    {
        private static string dataPath = Application.persistentDataPath;
#if UNITY_EDITOR
        public override string BlueprintZipFilepath => "Assets/Resources/BlueprintData/Blueprints_v1.0.zip";
#endif
#if UNITY_ANDROID && !UNITY_EDITOR
        public override string BlueprintZipFilepath => $"{dataPath}/Blueprints_v1.0.zip";
#endif
    }
}