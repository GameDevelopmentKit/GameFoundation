namespace GameFoundation.Scripts.BlueprintFlow.BlueprintControlFlow
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.IO.Compression;
    using System.Linq;
    using Cysharp.Threading.Tasks;
    using GameFoundation.Scripts.BlueprintFlow.BlueprintReader;
    using GameFoundation.Scripts.BlueprintFlow.Signals;
    using GameFoundation.Scripts.GameManager;
    using GameFoundation.Scripts.Network.WebService;
    using GameFoundation.Scripts.Utilities.Extension;
    using GameFoundation.Scripts.Utilities.LogService;
    using UnityEngine;
    using Zenject;

    /// <summary>
    ///  The main manager for reading blueprints pipeline/>.
    /// </summary>
    public class BlueprintReaderManager
    {
        private ReadBlueprintProgressSignal readBlueprintProgressSignal = new();
        public BlueprintReaderManager(SignalBus signalBus, ILogService logService, DiContainer diContainer,
            GameFoundationLocalData localData, HandleLocalDataServices handleLocalDataServices,
            IHttpService httpService, BlueprintConfig blueprintConfig)
        {
            this.signalBus               = signalBus;
            this.logService              = logService;
            this.diContainer             = diContainer;
            this.localData               = localData;
            this.handleLocalDataServices = handleLocalDataServices;
            this.httpService             = httpService;
            this.blueprintConfig         = blueprintConfig;
        }

        public virtual async void LoadBlueprint(string url, string hash = "test")
        {
            if (!this.IsLoadLocalBlueprint(url, hash))
            {
                //Download new blueprints version from remote
                var progressSignal = new LoadBlueprintDataProgressSignal();
                await this.httpService.Download(url,
                    string.Format(this.blueprintConfig.BlueprintZipFilename,
                        this.blueprintConfig.CurrentBlueprintVersion), (downloaded, length) =>
                    {
                        progressSignal.percent = downloaded / (float)length * 100f;
                        this.signalBus.Fire(progressSignal);
                    });

                this.localData.BlueprintModel.BlueprintDownloadUrl = url;
                this.handleLocalDataServices.Save(this.localData, true);
            }

            // Unzip file to memory
            this.listRawBlueprints = await UniTask.RunOnThreadPool(this.UnzipBlueprint);

            //Load all blueprints to instances
            try
            {
                await this.ReadAllBlueprint();
            }
            catch (FieldDontExistInBlueprint e)
            {
                this.logService.Error(e.Message);
            }

            this.logService.Log("[BlueprintReader] All blueprint are loaded");

            this.signalBus.Fire<LoadBlueprintDataSuccessedSignal>();
        }

        protected virtual bool IsLoadLocalBlueprint(string url, string hash) =>
            this.localData.BlueprintModel.BlueprintDownloadUrl == url &&
            MD5Utils.GetMD5HashFromFile(this.blueprintConfig.BlueprintZipFilepath) == hash &&
            File.Exists(this.blueprintConfig.BlueprintZipFilepath);

        protected virtual async UniTask<Dictionary<string, string>> UnzipBlueprint()
        {
            var result = new Dictionary<string, string>();
            if (!File.Exists(this.blueprintConfig.BlueprintZipFilepath))
            {
                return result;
            }

            using (var archive = ZipFile.OpenRead(this.blueprintConfig.BlueprintZipFilepath))
            {
                foreach (var entry in archive.Entries)
                {
                    if (!entry.FullName.EndsWith(BlueprintConfig.BlueprintFileType, StringComparison.OrdinalIgnoreCase))
                        continue;
                    using var streamReader = new StreamReader(entry.Open());
                    result.Add(entry.Name, await streamReader.ReadToEndAsync());
                }
            }

            return result;
        }

        private UniTask ReadAllBlueprint()
        {
            if (!File.Exists(this.blueprintConfig.BlueprintZipFilepath))
            {
                this.logService.Error(
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
                    listReadTask.Add(UniTask.RunOnThreadPool(() => this.OpenReadBlueprint(blueprintInstance)));
                }
                else
                {
                    this.logService.Log($"Can not resolve blueprint {blueprintType.Name}");
                }
            }

            return UniTask.WhenAll(listReadTask);
        }

        private async UniTask OpenReadBlueprint(IGenericBlueprintReader blueprintReader)
        {
            var bpAttribute = blueprintReader.GetCustomAttribute<BlueprintReaderAttribute>();
            if (bpAttribute != null)
            {
                if (bpAttribute.BlueprintScope == BlueprintScope.Server) return;

                // Try to load a raw blueprint file from local or resource folder
                string rawCsv;
                if (!bpAttribute.IsLoadFromResource)
                {
                    if (!this.listRawBlueprints.TryGetValue(bpAttribute.DataPath + BlueprintConfig.BlueprintFileType,
                        out rawCsv))
                    {
                        this.logService.Warning(
                            $"[BlueprintReader] Blueprint {bpAttribute.DataPath} is not exists at the local folder, try to load from resource folder");
                        rawCsv = await LoadRawCsvFromResourceFolder();
                    }
                }
                else
                {
                    rawCsv = await LoadRawCsvFromResourceFolder();
                }

                async UniTask<string> LoadRawCsvFromResourceFolder()
                {
                    await UniTask.SwitchToMainThread();
                    try
                    {
                        return ((TextAsset)await Resources.LoadAsync<TextAsset>(BlueprintConfig.ResourceBlueprintPath +
                                                                                bpAttribute.DataPath)).text;
                    }
                    catch (Exception e)
                    {
                        this.logService.Error($"Load {bpAttribute.DataPath} blueprint error!!!");
                        this.logService.Exception(e);
                        return null;
                    }
                }

                // Deserialize the raw blueprint to the blueprint reader instance
                if (!string.IsNullOrEmpty(rawCsv))
                {
                    await blueprintReader.DeserializeFromCsv(rawCsv);
                    this.readBlueprintProgressSignal.CurrentProgress++;
                    this.signalBus.Fire(this.readBlueprintProgressSignal);
                }
                else
                    this.logService.Warning(
                        $"[BlueprintReader] Unable to load {bpAttribute.DataPath} from {(bpAttribute.IsLoadFromResource ? "resource folder" : "local folder")}!!!");
            }
            else
            {
                this.logService.Warning(
                    $"[BlueprintReader] Class {blueprintReader} does not have BlueprintReaderAttribute yet");
            }
        }

        #region zeject

        private readonly SignalBus                  signalBus;
        private readonly ILogService                logService;
        private readonly DiContainer                diContainer;
        private readonly GameFoundationLocalData    localData;
        private readonly HandleLocalDataServices    handleLocalDataServices;
        private readonly IHttpService               httpService;
        private          Dictionary<string, string> listRawBlueprints = new();
        private readonly BlueprintConfig            blueprintConfig;

        #endregion
    }
}