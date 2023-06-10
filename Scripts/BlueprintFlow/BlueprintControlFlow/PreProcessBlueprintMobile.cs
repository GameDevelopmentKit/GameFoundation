namespace BlueprintFlow.BlueprintControlFlow
{
    using System;
    using System.IO;
    using Cysharp.Threading.Tasks;
    using UnityEngine;

    /// <summary>
    /// We need it to pre process read blueprint from mobile
    /// </summary>
    public class PreProcessBlueprintMobile
    {
        private const string FileName = "Blueprints_v1.0.zip";

        public async UniTask LoadStreamAsset()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
        var output = await LoadStreamingAssetMobile(FileName);
        await this.MoveBlueprintToDevice(FileName, output);
#endif
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
        var output = await LoadStreamingAssetFromWindow(FileName);
        await this.MoveBlueprintToDevice(FileName, output);
#endif
        }

        private UniTask MoveBlueprintToDevice(string fileName, byte[] data)
        {
            Debug.Log($"Move Blueprint To {Application.persistentDataPath + "/" + fileName}");
            File.WriteAllBytes(Application.persistentDataPath + "/" + fileName, data);
            return UniTask.CompletedTask;
        }
    }
}