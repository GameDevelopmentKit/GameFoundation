namespace DataManager.Blueprint.BlueprintSource
{
    using System.Collections.Generic;
    using Cysharp.Threading.Tasks;
    using DataManager.Blueprint.BlueprintController;
    using DataManager.Blueprint.Signals;
    using GameFoundation.Scripts.Utilities.LogService;
    using UnityEngine;
    using Zenject;

    public class ResourceBlueprintLoader : IBlueprintLoader
    {
        private readonly SignalBus           signalBus;
        private readonly BlueprintConfig     blueprintConfig;
        private readonly ILogService         logService;
        public           BlueprintSourceType BlueprintSource { get; } = BlueprintSourceType.Resource;

        public ResourceBlueprintLoader(SignalBus signalBus, BlueprintConfig blueprintConfig, ILogService logService)
        {
            this.signalBus       = signalBus;
            this.blueprintConfig = blueprintConfig;
            this.logService      = logService;
        }

        public UniTask<(Dictionary<string, string> dataPathToRawBlueprint, HashSet<string> failedDataPathList)> LoadAllBlueprint(HashSet<string> dataPathList)
        {
            var result             = new Dictionary<string, string>();
            var failedDataPathList = new HashSet<string>();

            foreach (var dataPath in dataPathList)
            {
                var textAsset = Resources.Load<TextAsset>(this.blueprintConfig.ResourceBlueprintPath + dataPath);
                if (textAsset == null)
                {
                    this.logService.Error($"[ResourceBlueprintLoader] Failed to load blueprint data from {dataPath}");
                    failedDataPathList.Add(dataPath);
                    continue;
                }
            
                result.Add(dataPath, textAsset.text);
            }

            this.signalBus.Fire(new LoadBlueprintDataProgressSignal { Percent = 1f });

            return UniTask.FromResult((result, failedDataPathList));
        }
    }
}