namespace DataManager.Blueprint.BlueprintSource
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Cysharp.Threading.Tasks;
    using DataManager.Blueprint.BlueprintController;
    using GameFoundation.Scripts.AssetLibrary;
    using UnityEngine;

    public class AddressableBlueprintLoader : IBlueprintLoader
    {
        private readonly IGameAssets         gameAsset;
        private readonly BlueprintConfig     blueprintConfig;
        public           BlueprintSourceType BlueprintSource { get; } = BlueprintSourceType.Addressable;

        public AddressableBlueprintLoader(IGameAssets gameAsset, BlueprintConfig blueprintConfig)
        {
            this.gameAsset       = gameAsset;
            this.blueprintConfig = blueprintConfig;
        }

        public async UniTask<(Dictionary<string, string> dataPathToRawBlueprint, HashSet<string> failedDataPathList)> LoadAllBlueprint(HashSet<string> dataPathList)
        {
            var result             = new Dictionary<string, string>();
            var failedDataPathList = new HashSet<string>();

            await UniTask.WhenAll(Enumerable.Select(dataPathList, dataPath => this.LoadBlueprintTask(dataPath, result, failedDataPathList)));
            return (result, failedDataPathList);
        }
        private async UniTask LoadBlueprintTask(string dataPath, Dictionary<string, string> result, HashSet<string> failedDataPathList)
        {
            try
            {
                object key       = $"{this.blueprintConfig.AddressableBlueprintPath}{dataPath}{this.blueprintConfig.BlueprintFileType}";
                var    textAsset = await this.gameAsset.LoadAssetAsync<TextAsset>(key);
                result.Add(dataPath, textAsset.text);
                this.gameAsset.ReleaseAsset(key);
            }
            catch (Exception e)
            {
                Debug.LogError($"[AddressableBlueprintLoader] Failed to load blueprint data from {dataPath}");
                Debug.LogException(e);
                failedDataPathList.Add(dataPath);
            }
        }
    }
}