namespace Mech.Network.HttpRequest
{
    using GameFoundation.Scripts.BlueprintFlow.Signals;
    using GameFoundation.Scripts.Network.WebService;
    using GameFoundation.Scripts.Utilities.LogService;
    using MechSharingCode.WebService.Blueprint;
    using Zenject;

    /// <summary>
    /// Get blueprint download link from server
    /// </summary>
    public class BlueprintDownloadRequest : BaseHttpRequest<BlueprintResponse>
    {

        private readonly SignalBus               signalBus;

        public BlueprintDownloadRequest(ILogService logger, SignalBus signalBus) : base(logger)
        {
            this.signalBus               = signalBus;
        }

        public override void Process(BlueprintResponse responseData)
        {
            this.Logger.Log($"Blueprint download link: {responseData.Url}");
            this.signalBus.Fire(new LoadBlueprintDataSignal { Url = responseData.Url, Hash = responseData.Hash});
        }
    }
}
