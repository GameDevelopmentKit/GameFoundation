namespace DataManager.Blueprint.BlueprintSource
{
    using System.Collections.Generic;
    using Cysharp.Threading.Tasks;
    using DataManager.Blueprint.BlueprintController;

    public interface IBlueprintLoader
    {
        public BlueprintSourceType BlueprintSource { get; }

        public UniTask<(Dictionary<string, string> dataPathToRawBlueprint, HashSet<string> failedDataPathList)> LoadAllBlueprint(HashSet<string> dataPathList);
    }
}