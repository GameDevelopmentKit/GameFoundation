namespace DataManager.Blueprint.BlueprintController
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Cysharp.Threading.Tasks;
    using DataManager.Blueprint.BlueprintReader;
    using DataManager.Blueprint.BlueprintSource;
    using DataManager.Blueprint.Signals;
    using GameFoundation.Scripts.Utilities.Extension;
    using GameFoundation.Scripts.Utilities.LogService;
    using Zenject;

    /// <summary>
    ///  The main manager for reading blueprints pipeline/>.
    /// </summary>
    public class BlueprintReaderManager
    {
        #region zeject

        private readonly SignalBus                                         signalBus;
        private readonly ILogService                                       logService;
        private readonly BlueprintConfig                                   blueprintConfig;
        private readonly DiContainer                                       diContainer;
        private          List<IGenericBlueprintReader>                     blueprintReaders;
        private readonly Dictionary<BlueprintSourceType, IBlueprintLoader> blueprintLoaders;

        #endregion

        private readonly ReadBlueprintProgressSignal readBlueprintProgressSignal = new();

        public BlueprintReaderManager(SignalBus signalBus, ILogService logService, BlueprintConfig blueprintConfig,
            List<IBlueprintLoader> blueprintLoaders, DiContainer diContainer)
        {
            this.signalBus        = signalBus;
            this.logService       = logService;
            this.blueprintConfig  = blueprintConfig;
            this.diContainer      = diContainer;
            this.blueprintLoaders = blueprintLoaders.ToDictionary(loader => loader.BlueprintSource);
        }

        public virtual async UniTask LoadBlueprint()
        {
            var stopWatchLoadBlueprintFlow = System.Diagnostics.Stopwatch.StartNew();
            this.logService.Log("[BlueprintReader] Start loading blueprint version " + this.blueprintConfig.CurrentBlueprintVersion);

            // Load all blueprints raw data from sources
            var stopWatchLoadBlueprintFromSource = System.Diagnostics.Stopwatch.StartNew();

            var listRawBlueprints = await this.LoadAllBlueprintFromSources();

            stopWatchLoadBlueprintFromSource.Stop();
            this.logService.Log("[BlueprintReader] Load All Blueprint From Source, " + stopWatchLoadBlueprintFromSource.Elapsed.TotalSeconds + "s");

            if (listRawBlueprints.Count == 0)
            {
                //Show warning popup
                return;
            }

            //read all blueprints to instances
            try
            {
                var stopWatchReadAllBlueprint = System.Diagnostics.Stopwatch.StartNew();
                await this.ReadAllBlueprint(listRawBlueprints);
                stopWatchReadAllBlueprint.Stop();
                this.logService.Log("[BlueprintReader] Read All Blueprint, " + stopWatchReadAllBlueprint.Elapsed.TotalSeconds + "s");
            }
            catch (Exception e)
            {
                this.logService.Exception(e);
            }

            stopWatchLoadBlueprintFlow.Stop();
            this.logService.Log("[BlueprintReader] Finish loading, " + stopWatchLoadBlueprintFlow.Elapsed.TotalSeconds + "s");
            this.signalBus.Fire<LoadBlueprintDataSucceedSignal>();
        }


        private async UniTask<Dictionary<string, string>> LoadAllBlueprintFromSources()
        {
            var sourceTypeToListDataPath = new Dictionary<BlueprintSourceType, HashSet<string>>();
            var allDerivedTypes          = ReflectionUtils.GetAllDerivedTypes<IGenericBlueprintReader>();
            this.blueprintReaders = allDerivedTypes.Select(type => (IGenericBlueprintReader)this.diContainer.Resolve(type)).ToList();
            foreach (var blueprintReader in this.blueprintReaders)
            {
                var bpAttribute = blueprintReader.GetCustomAttribute<BlueprintReaderAttribute>();
                if (bpAttribute != null)
                {
                    if (bpAttribute.BlueprintScope == BlueprintScope.Server) continue;

                    var sourceType = bpAttribute.CustomSource != BlueprintSourceType.None ? bpAttribute.CustomSource : this.blueprintConfig.Source;

                    if (!sourceTypeToListDataPath.TryGetValue(sourceType, out var listDataPath))
                    {
                        listDataPath = new HashSet<string>();
                        sourceTypeToListDataPath.Add(sourceType, listDataPath);
                    }

                    listDataPath.Add(bpAttribute.DataPath);
                }
                else
                {
                    this.logService.Warning($"[BlueprintReader] Class {blueprintReader} does not have BlueprintReaderAttribute yet");
                }
            }


            var listRawBlueprints  = new Dictionary<string, string>();
            var failedDataPathList = new HashSet<string>();

            // Load all blueprints from all sources and await for all of them
            var listLoadTask = new List<UniTask<(Dictionary<string, string> dataPathToRawBlueprint, HashSet<string> failedDataPathList)>>();
            foreach (var (sourceType, dataPathList) in sourceTypeToListDataPath)
            {
                if (this.TryGetLoader(sourceType, out var loader))
                {
                    listLoadTask.Add(loader.LoadAllBlueprint(dataPathList));
                }
            }

            var results = await UniTask.WhenAll(listLoadTask);
            foreach (var result in results)
            {
                if (result.dataPathToRawBlueprint != null)
                {
                    foreach (var (dataPath, rawBlueprint) in result.dataPathToRawBlueprint)
                    {
                        listRawBlueprints.Add(dataPath, rawBlueprint);
                    }
                }

                if (result.failedDataPathList != null)
                {
                    failedDataPathList.AddRange(result.failedDataPathList);
                }
            }

            // If there are still some data path that failed to load, try to load them from fallback source
            if (failedDataPathList.Count > 0)
            {
                if (this.TryGetLoader(this.blueprintConfig.SourceFallback, out var fallbackLoader))
                {
                    var fallbackResult = await fallbackLoader.LoadAllBlueprint(failedDataPathList);


                    if (fallbackResult.dataPathToRawBlueprint != null)
                    {
                        foreach (var (dataPath, rawBlueprint) in fallbackResult.dataPathToRawBlueprint)
                        {
                            listRawBlueprints.Add(dataPath, rawBlueprint);
                        }
                    }

                    if (fallbackResult.failedDataPathList.Count > 0)
                    {
                        this.logService.Warning($"[BlueprintReader] Failed to load blueprints from {string.Join(",", fallbackResult.failedDataPathList)}");
                    }
                }
            }

            return listRawBlueprints;
        }


        private bool TryGetLoader(BlueprintSourceType type, out IBlueprintLoader loader)
        {
            if (this.blueprintLoaders.TryGetValue(type, out loader))
            {
                return true;
            }

            this.logService.Warning($"[BlueprintReader] Can not find loader for {type}");
            return false;
        }

        private UniTask ReadAllBlueprint(Dictionary<string, string> listRawBlueprints)
        {
            var listReadTask = new List<UniTask>();
            this.readBlueprintProgressSignal.MaxBlueprint    = this.blueprintReaders.Count;
            this.readBlueprintProgressSignal.CurrentProgress = 0;
            this.signalBus.Fire(this.readBlueprintProgressSignal); // Inform that we just start reading blueprint
            foreach (var blueprintReader in this.blueprintReaders)
            {
                var bpAttribute = blueprintReader.GetCustomAttribute<BlueprintReaderAttribute>();
                if (bpAttribute == null) continue;
                if (listRawBlueprints.TryGetValue(bpAttribute.DataPath, out var rawCsv))
                {
                    listReadTask.Add(this.ReadBlueprint(blueprintReader, rawCsv));
                }
            }

            return UniTask.WhenAll(listReadTask);
        }

        private async UniTask ReadBlueprint(IGenericBlueprintReader blueprintReader, string rawCsv)
        {
            // Deserialize the raw blueprint to the blueprint reader instance
            if (!string.IsNullOrEmpty(rawCsv))
            {
                try
                {
                    await blueprintReader.DeserializeFromCsv(rawCsv);
                    lock (this.readBlueprintProgressSignal)
                    {
                        this.readBlueprintProgressSignal.CurrentProgress++;
                        this.signalBus.Fire(this.readBlueprintProgressSignal);
                    }
                }
                catch (Exception e)
                {
                    this.logService.Error($"[BlueprintReader] Error while reading blueprint {blueprintReader.GetType().Name}");
                    this.logService.Exception(e);
                }
            }
            else
                this.logService.Warning($"[BlueprintReader] Can not find raw csv for {blueprintReader.GetType().Name}");
        }
    }
}