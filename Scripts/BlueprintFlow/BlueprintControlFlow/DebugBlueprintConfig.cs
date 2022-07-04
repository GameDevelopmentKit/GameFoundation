namespace GameFoundation.Scripts.BlueprintFlow.BlueprintControlFlow
{
    using UnityEngine;
    public class DebugBlueprintConfig : BlueprintConfig
    {
#if UNITY_EDITOR
        public override string BlueprintZipFilepath => "Assets/Resources/BlueprintData/Blueprints_v1.0.zip";
#endif
#if UNITY_ANDROID &&!UNITY_EDITOR
 static string persistentDataPath = Application.persistentDataPath;
        public override string BlueprintZipFilepath => $"{persistentDataPath}/Blueprints_v1.0.zip";
#endif
    }
}