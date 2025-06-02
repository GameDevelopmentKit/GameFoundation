namespace BlueprintFlow.APIHandler
{
    using System;
    using BlueprintFlow.BlueprintControlFlow;
    using Cysharp.Threading.Tasks;
    using UnityEngine.Scripting;
    #if !GDK_NETWORK_ENABLE
    using System.Net;
    #else
    using Network.WebService;
    #endif

    /// <summary>
    /// Class uses for downloading the blueprint zip file from <see cref="BlueprintConfig.FetchBlueprintUri"/> and put it in the <see cref="BlueprintConfig.BlueprintZipFilepath"/>
    /// </summary>
    public class BlueprintDownloader
    {
        #if !GDK_NETWORK_ENABLE
        [Preserve]
        public BlueprintDownloader()
        {
        }

        public UniTask DownloadBlueprintAsync(string blueprintDownloadUrl, string filePath, Action<long, long> onDownloadProgress)
        {
            using var client = new WebClient();
            var       uri    = new Uri(blueprintDownloadUrl);
            var       task   = client.DownloadFileTaskAsync(uri, filePath);
            client.DownloadProgressChanged += (sender, args) => onDownloadProgress.Invoke(args.BytesReceived, args.TotalBytesToReceive);
            return task.AsUniTask();
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