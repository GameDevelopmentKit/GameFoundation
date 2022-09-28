namespace GameFoundation.Scripts.BlueprintFlow.Downloader
{
    using System;
    using System.Net;
    using System.Threading.Tasks;
    using GameFoundation.Scripts.BlueprintFlow.BlueprintControlFlow;
    using GameFoundation.Scripts.Utilities.LogService;
    using Zenject;

    /// <summary>
    /// Class uses for downloading the blueprint zip file from <see cref="BlueprintConfig.BlueprintS3Link"/> and put it in the <see cref="BlueprintConfig.BlueprintZipFilepath"/>
    /// </summary>
    public class BlueprintDownloader
    {
        [Inject] private ILogService     logService;
        [Inject] private BlueprintConfig blueprintConfig;
        
        public Task DownloadBlueprintAsync(string blueprintDownloadUrl)
        {
            try
            {
                using var client = new WebClient();
                var       uri    = new Uri(blueprintDownloadUrl);
                var       task   = client.DownloadFileTaskAsync(uri, this.blueprintConfig.BlueprintZipFilepath);
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