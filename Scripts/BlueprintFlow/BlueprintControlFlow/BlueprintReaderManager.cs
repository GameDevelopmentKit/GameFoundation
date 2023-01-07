namespace BlueprintFlow.BlueprintControlFlow
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.IO.Compression;
    using System.Linq;
    using System.Threading.Tasks;
    using BlueprintFlow.APIHandler;
    using BlueprintFlow.BlueprintReader;
    using BlueprintFlow.Signals;
    using Cysharp.Threading.Tasks;
    using GameFoundation.Scripts.Utilities;
    using GameFoundation.Scripts.Utilities.Extension;
    using GameFoundation.Scripts.Utilities.LogService;
    using UnityEngine;
    using Zenject;

    /// <summary>
    ///  The main manager for reading blueprints pipeline/>.
    /// </summary>
    public class BlueprintReaderManager
    {
        #region zeject

        private readonly SignalBus               signalBus;
        private readonly ILogService             logService;
        private readonly DiContainer             diContainer;
        private readonly HandleLocalDataServices handleLocalDataServices;
        private readonly BlueprintConfig         blueprintConfig;
        private readonly FetchBlueprintInfo      fetchBlueprintInfo;
        private readonly BlueprintDownloader     blueprintDownloader;

        #endregion

        private readonly ReadBlueprintProgressSignal readBlueprintProgressSignal = new();

        public BlueprintReaderManager(SignalBus signalBus, ILogService logService, DiContainer diContainer, HandleLocalDataServices handleLocalDataServices, BlueprintConfig blueprintConfig,
            FetchBlueprintInfo fetchBlueprintInfo, BlueprintDownloader blueprintDownloader)
        {
            this.signalBus               = signalBus;
            this.logService              = logService;
            this.diContainer             = diContainer;
            this.handleLocalDataServices = handleLocalDataServices;
            this.blueprintConfig         = blueprintConfig;
            this.fetchBlueprintInfo      = fetchBlueprintInfo;
            this.blueprintDownloader     = blueprintDownloader;
        }

        public virtual async UniTask LoadBlueprint()
        {
            this.logService.Log("[BlueprintReader] Start loading");
            Dictionary<string, string> listRawBlueprints = null;
            if (this.blueprintConfig.IsResourceMode)
            {
                listRawBlueprints = new Dictionary<string, string>();
            }
            else
            {
                var newBlueprintInfo = await this.fetchBlueprintInfo.GetBlueprintInfo(this.blueprintConfig.FetchBlueprintUri);
                if (!this.IsCachedBlueprintUpToDate(newBlueprintInfo.Url, newBlueprintInfo.Hash))
                {
                    await this.DownloadBlueprint(newBlueprintInfo.Url);
                }

                //Is blueprint zip file exists in storage
                if (File.Exists(this.blueprintConfig.BlueprintZipFilepath))
                {
                    // Save blueprint info to local
                    this.handleLocalDataServices.Save(newBlueprintInfo, true);

                    // Unzip file to memory
                    listRawBlueprints = await UniTask.RunOnThreadPool(this.UnzipBlueprint);
                }
            }

            if (listRawBlueprints == null)
            {
                //Show warning popup
                return;
            }

            //Load all blueprints to instances
            try
            {
                await this.ReadAllBlueprint(listRawBlueprints);
            }
            catch (Exception e)
            {
                this.logService.Exception(e);
            }

            this.logService.Log("[BlueprintReader] All blueprint are loaded");

            this.signalBus.Fire<LoadBlueprintDataSuccessedSignal>();
        }

        protected virtual bool IsCachedBlueprintUpToDate(string url, string hash) =>
            this.handleLocalDataServices.Load<BlueprintInfoData>().Url == url &&
            MD5Utils.GetMD5HashFromFile(this.blueprintConfig.BlueprintZipFilepath) == hash;


        //Download new blueprints version from remote
        private async UniTask DownloadBlueprint(string blueprintDownloadLink)
        {
            var progressSignal = new LoadBlueprintDataProgressSignal();
            await this.blueprintDownloader.DownloadBlueprintAsync(blueprintDownloadLink, this.blueprintConfig.BlueprintZipFilepath, (downloaded, length) =>
            {
                progressSignal.percent = downloaded / (float)length * 100f;
                this.signalBus.Fire(progressSignal);
            });
        }

        protected virtual async UniTask<Dictionary<string, string>> UnzipBlueprint()
        {
            var result = new Dictionary<string, string>();
            if (!File.Exists(this.blueprintConfig.BlueprintZipFilepath))
            {
                return result;
            }

            using var archive = ZipFile.OpenRead(this.blueprintConfig.BlueprintZipFilepath);
            foreach (var entry in archive.Entries)
            {
                if (!entry.FullName.EndsWith(this.blueprintConfig.BlueprintFileType, StringComparison.OrdinalIgnoreCase))
                    continue;
                using var streamReader   = new StreamReader(entry.Open());
                var       readToEndAsync = await streamReader.ReadToEndAsync();
                result.Add(entry.Name, readToEndAsync);
            }

            return result;
        }

        private UniTask ReadAllBlueprint(Dictionary<string, string> listRawBlueprints)
        {
            if (!File.Exists(this.blueprintConfig.BlueprintZipFilepath))
            {
                this.logService.Warning(
                    $"[BlueprintReader] {this.blueprintConfig.BlueprintZipFilepath} is not exists!!!, Continue load from resource");
            }

            var listReadTask    = new List<UniTask>();
            var allDerivedTypes = ReflectionUtils.GetAllDerivedTypes<IGenericBlueprintReader>();
            this.readBlueprintProgressSignal.MaxBlueprint = allDerivedTypes.Count();
            foreach (var blueprintType in allDerivedTypes)
            {
                var blueprintInstance = (IGenericBlueprintReader)this.diContainer.Resolve(blueprintType);
                if (blueprintInstance != null)
                {
                    listReadTask.Add(UniTask.RunOnThreadPool(() => this.OpenReadBlueprint(blueprintInstance, listRawBlueprints)));
                }
                else
                {
                    this.logService.Log($"Can not resolve blueprint {blueprintType.Name}");
                }
            }

            return UniTask.WhenAll(listReadTask);
        }

        private async UniTask OpenReadBlueprint(IGenericBlueprintReader blueprintReader, Dictionary<string, string> listRawBlueprints)
        {
            var bpAttribute = blueprintReader.GetCustomAttribute<BlueprintReaderAttribute>();
            if (bpAttribute != null)
            {
                if (bpAttribute.BlueprintScope == BlueprintScope.Server) return;

                // Try to load a raw blueprint file from local or resource folder
                string rawCsv;
                if (this.blueprintConfig.IsResourceMode || bpAttribute.IsLoadFromResource)
                {
                    rawCsv = await LoadRawCsvFromResourceFolder();
                }
                else
                {
                    if (!listRawBlueprints.TryGetValue(bpAttribute.DataPath + this.blueprintConfig.BlueprintFileType, out rawCsv))
                    {
                        this.logService.Warning($"[BlueprintReader] Blueprint {bpAttribute.DataPath} is not exists at the local folder, try to load from resource folder");
                        rawCsv = await LoadRawCsvFromResourceFolder();
                    }
                }

                async UniTask<string> LoadRawCsvFromResourceFolder()
                {
                    await UniTask.SwitchToMainThread();
                    var result = string.Empty;
                    try
                    {
                        result = ((TextAsset)await Resources.LoadAsync<TextAsset>(this.blueprintConfig.ResourceBlueprintPath + bpAttribute.DataPath)).text;
                    }
                    catch (Exception e)
                    {
                        this.logService.Error($"Load {bpAttribute.DataPath} blueprint error!!!");
                        this.logService.Exception(e);
                    }

                    await UniTask.SwitchToThreadPool();
                    return result;
                }

                // Deserialize the raw blueprint to the blueprint reader instance
                if (!string.IsNullOrEmpty(rawCsv))
                {
                    await blueprintReader.DeserializeFromCsv(rawCsv);
                    this.readBlueprintProgressSignal.CurrentProgress++;
                    try
                    {
                        this.signalBus.Fire(this.readBlueprintProgressSignal);
                    }
                    catch (Exception e)
                    {
                        logService.Exception(e);
                    }
                }
                else
                    this.logService.Warning($"[BlueprintReader] Unable to load {bpAttribute.DataPath} from {(bpAttribute.IsLoadFromResource ? "resource folder" : "local folder")}!!!");
            }
            else
            {
                this.logService.Warning($"[BlueprintReader] Class {blueprintReader} does not have BlueprintReaderAttribute yet");
            }
        }
    }
}