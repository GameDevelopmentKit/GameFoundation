namespace MechSharingCode.Blueprints.BlueprintControlFlow
{
    using System;
    using System.Net;
    using System.Threading.Tasks;
    using Mech.Core.BlueprintFlow.BlueprintControlFlow;
    using Mech.Services;
    using Zenject;

    /// <summary>
    /// Class uses for downloading the blueprint zip file from <see cref="BlueprintConfig.BlueprintS3Link"/> and put it in the <see cref="BlueprintConfig.BlueprintZipFilepath"/>
    /// </summary>
    public class BlueprintDownloader
    {
        [Inject] private ILogService logService;
        public Task DownloadBlueprintAsync(string blueprintDownloadUrl)
        {
            try
            {
                using var client = new WebClient();
                var       uri    = new Uri(blueprintDownloadUrl);
                var       task   = client.DownloadFileTaskAsync(uri, BlueprintConfig.BlueprintZipFilepath);
                return task;
            }
            catch (Exception e)
            {
                this.logService.Exception(e);
                throw;
            }
        }
    }
}