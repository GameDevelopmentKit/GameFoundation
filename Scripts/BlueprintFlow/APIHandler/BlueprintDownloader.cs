namespace BlueprintFlow.APIHandler
{
    using System;
    using System.Net;
    using BlueprintFlow.BlueprintControlFlow;
    using Cysharp.Threading.Tasks;
    using GameFoundation.Scripts.Utilities.LogService;
    using Zenject;

    /// <summary>
    /// Class uses for downloading the blueprint zip file from <see cref="BlueprintConfig.BlueprintS3Link"/> and put it in the <see cref="BlueprintConfig.BlueprintZipFilepath"/>
    /// </summary>
    public class BlueprintDownloader
    {
        [Inject] private ILogService logService;

        public UniTask DownloadBlueprintAsync(string blueprintDownloadUrl, string filePath, Action<long, long> onDownloadProgress)
        {
            try
            {
                using var client = new WebClient();
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
    }
}