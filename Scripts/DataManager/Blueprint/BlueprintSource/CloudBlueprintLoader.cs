namespace DataManager.Blueprint.BlueprintSource
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.IO.Compression;
    using System.Threading.Tasks;
    using Cysharp.Threading.Tasks;
    using DataManager.Blueprint.APIHandler;
    using DataManager.Blueprint.BlueprintController;
    using DataManager.Blueprint.Signals;
    using DataManager.LocalData;
    using GameFoundation.Scripts.Utilities.Extension;
    using GameFoundation.Scripts.Utilities.LogService;
    using Zenject;

    public class CloudBlueprintLoader : IBlueprintLoader
    {
        public BlueprintSourceType BlueprintSource { get; } = BlueprintSourceType.Cloud;


        private readonly SignalBus                signalBus;
        private readonly IHandleLocalDataServices handleLocalDataServices;
        private readonly BlueprintConfig          blueprintConfig;
        private readonly FetchBlueprintInfo       fetchBlueprintInfo;
        private readonly BlueprintDownloader      blueprintDownloader;
        private readonly ILogService              logService;


        public CloudBlueprintLoader(SignalBus signalBus, IHandleLocalDataServices handleLocalDataServices, BlueprintConfig blueprintConfig,
            FetchBlueprintInfo fetchBlueprintInfo, BlueprintDownloader blueprintDownloader, ILogService logService)
        {
            this.signalBus               = signalBus;
            this.handleLocalDataServices = handleLocalDataServices;
            this.blueprintConfig         = blueprintConfig;
            this.fetchBlueprintInfo      = fetchBlueprintInfo;
            this.blueprintDownloader     = blueprintDownloader;
            this.logService              = logService;
        }


        public async UniTask<(Dictionary<string, string> dataPathToRawBlueprint, HashSet<string> failedDataPathList)> LoadAllBlueprint(HashSet<string> dataPathList)
        {
            var newBlueprintInfo = await this.fetchBlueprintInfo.GetBlueprintInfo(this.blueprintConfig.FetchBlueprintUri);
            if (!await this.IsCachedBlueprintUpToDate(newBlueprintInfo.Url, newBlueprintInfo.Hash))
            {
                await this.DownloadBlueprint(newBlueprintInfo.Url);
            }

            //Is blueprint zip file exists in storage
            if (File.Exists(this.blueprintConfig.BlueprintZipFilepath))
            {
                // Save blueprint info to local
                this.handleLocalDataServices.Save(newBlueprintInfo, true);

                // Unzip file to memory

#if !UNITY_WEBGL
                return await UniTask.RunOnThreadPool(() => this.UnzipBlueprint(dataPathList));
#else
                return await UniTask.Create(() => this.UnzipBlueprint(dataPathList));
#endif
            }

            this.logService.Warning($"[BlueprintReader] {this.blueprintConfig.BlueprintZipFilepath} is not exists!!!");
            return (null, dataPathList);
        }
        private async UniTask<(Dictionary<string, string> dataPathToRawBlueprint, HashSet<string> failedDataPathList)> UnzipBlueprint(HashSet<string> dataPathList)
        {
            var       blueprintData = new Dictionary<string, string>();
            var       listTask      = new List<Task>();
            using var archive       = ZipFile.OpenRead(this.blueprintConfig.BlueprintZipFilepath);
            foreach (var entry in archive.Entries)
            {
                var nameWithoutExtension = Path.GetFileNameWithoutExtension(entry.Name);
                if (!dataPathList.Contains(nameWithoutExtension) || !entry.FullName.EndsWith(this.blueprintConfig.BlueprintFileType, StringComparison.OrdinalIgnoreCase))
                    continue;
                using var streamReader = new StreamReader(entry.Open());
                listTask.Add(streamReader.ReadToEndAsync().ContinueWith(task =>
                {
                    blueprintData.Add(nameWithoutExtension, task.Result);
                    dataPathList.Remove(nameWithoutExtension);
                }));
               
            }

            await Task.WhenAll(listTask);

            return (blueprintData, dataPathList);
        }


        protected virtual async UniTask<bool> IsCachedBlueprintUpToDate(string url, string hash) =>
            (await this.handleLocalDataServices.Load<BlueprintInfoData>()).Url == url &&
            MD5Utils.GetMD5HashFromFile(this.blueprintConfig.BlueprintZipFilepath) == hash;


        //Download new blueprints version from remote
        private async UniTask DownloadBlueprint(string blueprintDownloadLink)
        {
            var progressSignal = new LoadBlueprintDataProgressSignal { Percent = 0f };
            this.signalBus.Fire(progressSignal); //Inform that we just starting dowloading blueprint
            await this.blueprintDownloader.DownloadBlueprintAsync(blueprintDownloadLink, this.blueprintConfig.BlueprintZipFilepath, (downloaded, length) =>
            {
                progressSignal.Percent = downloaded / (float)length * 100f;
                this.signalBus.Fire(progressSignal);
            });
        }
    }
}