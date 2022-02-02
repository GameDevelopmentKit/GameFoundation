namespace Mech.Network.HttpRequest
{
    using GameFoundation.Scripts.BlueprintFlow.BlueprintControlFlow;
    using GameFoundation.Scripts.BlueprintFlow.Signals;
    using GameFoundation.Scripts.Network.WebService;
    using GameFoundation.Scripts.Utilities.LogService;
    using MechSharingCode.WebService.Blueprint;
    using Zenject;

    /// <summary>
    /// Get blueprint download link from server
    /// </summary>
    public class BlueprintDownloadRequest : BaseHttpRequest<GetBlueprintResponse>
    {
        #region zenject

        private readonly BlueprintReaderManager blueprintReaderManager;

        #endregion
        
        public BlueprintDownloadRequest(ILogService logger, BlueprintReaderManager blueprintReaderManager) : base(logger)
        {
            this.blueprintReaderManager = blueprintReaderManager;
        }

        public override void Process(GetBlueprintResponse responseData)
        {
            this.Logger.Log($"Blueprint download link: {responseData.Url}");
            this.blueprintReaderManager.LoadBlueprint(responseData.Url, responseData.Hash);
        }
    }
}
