namespace BlueprintFlow.APIHandler
{
    using System;
    using BlueprintFlow.BlueprintControlFlow;
    using Cysharp.Threading.Tasks;
    using UnityEngine.Scripting;
    #if !GDK_NETWORK_ENABLE
    using System.Net;
    using GameFoundation.Scripts.Utilities.LogService;

    #else
    using Network.WebService;
    #endif

    /// <summary>
    /// Class uses for downloading the blueprint zip file from <see cref="BlueprintConfig.FetchBlueprintUri"/> and put it in the <see cref="BlueprintConfig.BlueprintZipFilepath"/>
    /// </summary>
    public class BlueprintDownloader
    {
        #if !GDK_NETWORK_ENABLE
        private readonly ILogService logService;

        [Preserve]
        public BlueprintDownloader(ILogService logService)
        {
            this.logService = logService;
        }

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
        #else
        private readonly IHttpService httpService;

        [Preserve]
        public BlueprintDownloader(IHttpService httpService)
        {
            this.httpService = httpService;
        }

        public UniTask DownloadBlueprintAsync(string blueprintDownloadUrl, string filePath, Action<long, long> onDownloadProgress)
        {
            return this.httpService.Download(blueprintDownloadUrl, filePath, onDownloadProgress.Invoke);
        }
        #endif
    }
}