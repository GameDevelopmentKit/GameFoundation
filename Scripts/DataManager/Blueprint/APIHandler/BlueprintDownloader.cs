namespace DataManager.Blueprint.APIHandler
{
    using System;
    using Cysharp.Threading.Tasks;
    using GameFoundation.Scripts.Utilities.LogService;
    using Zenject;

    /// <summary>
    /// Class uses for downloading the blueprint zip file from <see cref="BlueprintConfig.BlueprintS3Link"/> and put it in the <see cref="BlueprintConfig.BlueprintZipFilepath"/>
    /// </summary>
    public class BlueprintDownloader
    {
        [Inject] private ILogService     logService;
        
#if !GDK_NETWORK_ENABLE
        public UniTask DownloadBlueprintAsync(string blueprintDownloadUrl, string filePath, Action<long,long> onDownloadProgress)
        {
            try
            {
                using var client = new System.Net.WebClient();
                var       uri    = new Uri(blueprintDownloadUrl);
                var       task   = client.DownloadFileTaskAsync(uri, filePath);
                client.DownloadProgressChanged += (sender, args) => onDownloadProgress.Invoke(args.BytesReceived, args.TotalBytesToReceive);
                return task.AsUniTask();
            }
            catch (Exception e)
            {
                this.logService.Exception(e);
                throw;
            }
        }
        
#else
        [Inject] private Network.WebService.IHttpService httpService;
        public UniTask DownloadBlueprintAsync(string blueprintDownloadUrl, string filePath, Action<long,long> onDownloadProgress)
        {
            return this.httpService.Download(blueprintDownloadUrl, filePath, onDownloadProgress.Invoke);
        }
        
#endif
    }
}