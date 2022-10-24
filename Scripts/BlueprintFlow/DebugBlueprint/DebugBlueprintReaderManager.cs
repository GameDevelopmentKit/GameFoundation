namespace BlueprintFlow.DebugBlueprint
{
    using BlueprintFlow.BlueprintControlFlow;
    using BlueprintFlow.Downloader;
    using GameFoundation.Scripts.Models;
    using GameFoundation.Scripts.Utilities;
    using GameFoundation.Scripts.Utilities.LogService;
    using Zenject;

    public class DebugBlueprintReaderManager : BlueprintReaderManager
    {

        protected override bool IsLoadLocalBlueprint(string url, string hash) => true;
        public DebugBlueprintReaderManager(SignalBus signalBus, ILogService logService, DiContainer diContainer, GameFoundationLocalData localData, HandleLocalDataServices handleLocalDataServices, BlueprintConfig blueprintConfig, BlueprintDownloader blueprintDownloader) : base(signalBus, logService, diContainer, localData, handleLocalDataServices, blueprintConfig, blueprintDownloader)
        {
        }
    }
}