namespace GameFoundation.Scripts.BlueprintFlow.DebugBlueprint
{
    using GameFoundation.Scripts.BlueprintFlow.BlueprintControlFlow;
    using GameFoundation.Scripts.Models;
    using GameFoundation.Scripts.Network.WebService;
    using GameFoundation.Scripts.Utilities;
    using GameFoundation.Scripts.Utilities.LogService;
    using Zenject;

    public class DebugBlueprintReaderManager : BlueprintReaderManager
    {
        public DebugBlueprintReaderManager(SignalBus signalBus, ILogService logService, DiContainer diContainer, GameFoundationLocalData localData, HandleLocalDataServices handleLocalDataServices, IHttpService httpService, BlueprintConfig blueprintConfig) : base(signalBus, logService, diContainer, localData, handleLocalDataServices, httpService, blueprintConfig)
        {
        }

        protected override bool IsLoadLocalBlueprint(string url, string hash) => true;
    }
}