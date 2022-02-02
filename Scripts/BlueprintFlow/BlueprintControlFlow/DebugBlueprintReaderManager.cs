using GameFoundation.Scripts.GameManager;
using GameFoundation.Scripts.Network.WebService;
using GameFoundation.Scripts.Utilities.LogService;
using Zenject;

namespace GameFoundation.Scripts.BlueprintFlow.BlueprintControlFlow
{
    public class DebugBlueprintReaderManager : BlueprintReaderManager
    {
        public DebugBlueprintReaderManager(SignalBus signalBus, ILogService logService, DiContainer diContainer, GameFoundationLocalData localData, HandleLocalDataServices handleLocalDataServices, IHttpService httpService, BlueprintConfig blueprintConfig) : base(signalBus, logService, diContainer, localData, handleLocalDataServices, httpService, blueprintConfig)
        {
        }

        protected override bool IsLoadLocalBlueprint(string url, string hash) => true;
    }
}