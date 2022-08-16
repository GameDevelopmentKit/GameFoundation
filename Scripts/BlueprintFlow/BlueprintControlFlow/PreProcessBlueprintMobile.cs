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
        var output = await LoadStreamingAsset(FileName);
        await this.MoveBlueprintToDevice(FileName, output);
    }

    [Obsolete]
    private async UniTask<byte[]> LoadStreamingAsset(string fileName)
    {
        //Read blueprint Data from stream Assets
        var filePath = "jar:file://" + Application.dataPath + "!/assets/" + fileName;
        var www      = new WWW(filePath);
        await www;
        return www.bytes;
    }

    private UniTask MoveBlueprintToDevice(string fileName, byte[] data)
    {
        File.WriteAllBytes(Application.persistentDataPath + "/" + fileName, data);
        return UniTask.CompletedTask;
    }
}