namespace GameFoundation.Scripts.BlueprintFlow.BlueprintControlFlow
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.IO.Compression;
    using Cysharp.Threading.Tasks;
    using GameFoundation.Scripts.BlueprintFlow.Signals;
    using GameFoundation.Scripts.GameManager;
    using GameFoundation.Scripts.Network.WebService;
    using GameFoundation.Scripts.Utilities.Extension;
    using GameFoundation.Scripts.Utilities.LogService;
    using MechSharingCode.Blueprints.BlueprintReader;
    using UnityEngine;
    using Zenject;

    /// <summary>
    ///  The main manager for reading blueprints pipeline, trigger by <see cref="LoadBlueprintDataSignal"/>.
    /// </summary>
    public class BlueprintReaderManager : IInitializable, IDisposable
    {
        private readonly SignalBus                  signalBus;
        private readonly ILogService                logService;
        private readonly DiContainer                diContainer;
        private readonly GameFoundationLocalData    localData;
        private readonly HandleLocalDataServices    handleLocalDataServices;
        private readonly IHttpService               httpService;
        private          Dictionary<string, string> listRawBlueprints;

        public BlueprintReaderManager(SignalBus signalBus, ILogService logService, DiContainer diContainer, GameFoundationLocalData localData, HandleLocalDataServices handleLocalDataServices,
            IHttpService httpService)
        {
            this.signalBus               = signalBus;
            this.logService              = logService;
            this.diContainer             = diContainer;
            this.localData               = localData;
            this.handleLocalDataServices = handleLocalDataServices;
            this.httpService             = httpService;
        }

        public void Initialize() { this.signalBus.Subscribe<LoadBlueprintDataSignal>(this.OnLoadBlueprint); }
        public void Dispose()    { this.signalBus.Unsubscribe<LoadBlueprintDataSignal>(this.OnLoadBlueprint); }

        private async void OnLoadBlueprint(LoadBlueprintDataSignal signal)
        {
            if (!this.IsLoadLocalBlueprint(signal))
            {
                //Download new blueprints version from remote
                var progressSignal = new LoadBlueprintDataProgressSignal();
                await this.httpService.Download(signal.Url, string.Format(BlueprintConfig.BlueprintZipFilename, BlueprintConfig.CurrentBlueprintVersion), (downloaded, length) =>
                {
                    progressSignal.percent = downloaded / (float)length * 100f;
                    this.signalBus.Fire(progressSignal);
                });

                this.localData.BlueprintModel.BlueprintDownloadUrl = signal.Url;
                this.handleLocalDataServices.Save(this.localData,true);
            }

            // Unzip file to memory
            this.listRawBlueprints = await UniTask.RunOnThreadPool(this.UnzipBlueprint);

            //Load all blueprints to instances
            await this.ReadAllBlueprint();
            this.logService.Log("[BlueprintReader] All blueprint are loaded");

            this.signalBus.Fire<LoadBlueprintDataSuccessedSignal>();
        }

        private bool IsLoadLocalBlueprint(LoadBlueprintDataSignal blueprintInfo)
        {
#if TEST_BLUEPRINT
            return true;
#else
            return this.localData.BlueprintModel.BlueprintDownloadUrl == blueprintInfo.Url && MD5Utils.GetMD5HashFromFile(BlueprintConfig.BlueprintZipFilepath) == blueprintInfo.Hash &&
                   File.Exists(BlueprintConfig.BlueprintZipFilepath);
#endif
        }

        private async UniTask<Dictionary<string, string>> UnzipBlueprint()
        {
            var result = new Dictionary<string, string>();
            using (var archive = ZipFile.OpenRead(BlueprintConfig.BlueprintZipFilepath))
            {
                foreach (var entry in archive.Entries)
                {
                    if (!entry.FullName.EndsWith(BlueprintConfig.BlueprintFileType, StringComparison.OrdinalIgnoreCase)) continue;
                    using var streamReader = new StreamReader(entry.Open());
                    result.Add(entry.Name, await streamReader.ReadToEndAsync());
                }
            }

            return result;
        }

        private UniTask ReadAllBlueprint()
        {
            if (!File.Exists(BlueprintConfig.BlueprintZipFilepath))
            {
                this.logService.Error($"[BlueprintReader] {BlueprintConfig.BlueprintZipFilepath} is not exists!!!");
                return UniTask.CompletedTask;
            }

            var listReadTask = new List<UniTask>();
            foreach (var blueprintType in ReflectionUtils.GetAllDerivedTypes<IGenericBlueprint>())
            {
                var blueprintInstance = (IGenericBlueprint)this.diContainer.Resolve(blueprintType);
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

        private async UniTask OpenReadBlueprint(IGenericBlueprint blueprintReader)
        {
            var bpAttribute = blueprintReader.GetCustomAttribute<BlueprintReaderAttribute>();
            if (bpAttribute != null)
            {
                if (bpAttribute.BlueprintScope == BlueprintScope.Server) return;

                // Try to load a raw blueprint file from local or resource folder
                string rawCsv;
                if (!bpAttribute.IsLoadFromResource)
                {
                    if (!this.listRawBlueprints.TryGetValue(bpAttribute.DataPath + BlueprintConfig.BlueprintFileType, out rawCsv))
                    {
                        this.logService.Warning($"[BlueprintReader] Blueprint {bpAttribute.DataPath} is not exists at the local folder, try to load from resource folder");
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
                        return ((TextAsset)await Resources.LoadAsync<TextAsset>(BlueprintConfig.ResourceBlueprintPath + bpAttribute.DataPath)).text;
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
                    await blueprintReader.DeserializeFromCsv(rawCsv);
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