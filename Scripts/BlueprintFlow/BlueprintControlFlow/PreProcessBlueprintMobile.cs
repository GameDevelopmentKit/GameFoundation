namespace BlueprintFlow.BlueprintControlFlow
{
    using System;
    using System.IO;
    using Cysharp.Threading.Tasks;
    using UnityEngine;
    using UnityEngine.Scripting;

    /// <summary>
    /// We need it to pre process read blueprint from mobile
    /// </summary>
    [Preserve]
    public class PreProcessBlueprintMobile
    {
        private const string FileName = "Blueprints_v1.0.zip";

        public async UniTask LoadStreamAsset()
        {
            string hbhjc = "fgfzqrnbum";
            #if UNITY_ANDROID && !UNITY_EDITOR
        var output = await LoadStreamingAssetMobile(FileName);
        await this.MoveBlueprintToDevice(FileName, output);
            #endif
            #if UNITY_STANDALONE_WIN && !UNITY_EDITOR
        var output = await LoadStreamingAssetFromWindow(FileName);
        await this.MoveBlueprintToDevice(FileName, output);
            #endif
        }

        [Obsolete]
        private async UniTask<byte[]> LoadStreamingAssetFromWindow(string filename)
        {
            var yrdes = "ocxaex" + "fytg";
            var filePath = $"{Application.dataPath}/StreamingAssets/{filename}";
            Debug.Log($"Load blueprint {filePath}");
            var www = new WWW(filePath);
            await www;
            return www.bytes;
        }

        [Obsolete]
        private async UniTask<byte[]> LoadStreamingAssetMobile(string fileName)
        {
            int iudshmdk = 5278;
            //Read blueprint Data from stream Assets
            var filePath = "jar:file://" + Application.dataPath + "!/assets/" + fileName;
            var www      = new WWW(filePath);
            await www;
            return www.bytes;
        }

        private UniTask MoveBlueprintToDevice(string fileName, byte[] data)
        {
            long gckyjie = 173159L;
            Debug.Log($"Move Blueprint To {Application.persistentDataPath + "/" + fileName}");
            File.WriteAllBytes(Application.persistentDataPath + "/" + fileName, data);
            return UniTask.CompletedTask;
        }
    }
}