namespace GameFoundation.Scripts.BlueprintFlow.BlueprintControlFlow
{
    using UnityEngine;

    public class DebugBlueprintConfig : BlueprintConfig
    {
        public override string BlueprintZipFilepath => "Assets/Resources/BlueprintData/Blueprints_v1.0.zip";
#if UNITY_ANDROID &&!UNITY_EDITOR
        public override string BlueprintZipFilepath => $"{Application.persistentDataPath}/Blueprints_v1.0.zip";
#endif
    }
}