namespace GameFoundation.Scripts.UIModule.ScreenFlow.Managers
{
    using System.Collections.Generic;
    using System.Linq;
    using Cysharp.Threading.Tasks;
    using GameFoundation.Scripts.AssetLibrary;
    using GameFoundation.Scripts.UIModule.ScreenFlow.Signals;
    using UnityEngine;
    using UnityEngine.AddressableAssets;
    using UnityEngine.ResourceManagement.ResourceProviders;
    using UnityEngine.SceneManagement;
    using Zenject;

    /// <summary>Load, unload scenes are wrapped here </summary>
    public class SceneDirector
    {
        private readonly   ISignalBus  signalBus;
        protected readonly IGameAssets GameAssets;
        public static      string      CurrentSceneName;

        public SceneDirector(ISignalBus signalBus, IGameAssets gameAssets)
        {
            this.signalBus  = signalBus;
            this.GameAssets = gameAssets;
        }

        public virtual async UniTask PreloadAssetBundleKey(string key) { await Addressables.DownloadDependenciesAsync(key).ToUniTask(); }

        public virtual async UniTask<SceneInstance> LoadSingleSceneAsyncByAddictiveMode(string sceneName, bool isSingle = true, List<string> keptAssets = null)
        {
            this.signalBus.Fire(new StartLoadingNewSceneSignal
            {
                CurrentScreenName = new List<string>() { CurrentSceneName },
                TargetScreenName  = new List<string>() { sceneName },
                ActiveScreenName  = sceneName
            });

            var oldScene     = SceneManager.GetActiveScene();
            var oldSceneName = oldScene.name;

            if (!isSingle)
            {
                var loadHandle = Addressables.LoadSceneAsync(
                    sceneName,
                    LoadSceneMode.Additive,
                    false
                );

                await loadHandle.Task;

                await UniTask.DelayFrame(1);

                await loadHandle.Result.ActivateAsync();

                SceneManager.SetActiveScene(loadHandle.Result.Scene);

                if (oldScene.IsValid())
                {
                    var unloadOp = SceneManager.UnloadSceneAsync(oldScene);

                    while (unloadOp is { isDone: false })
                        await UniTask.DelayFrame(1);
                }

                CurrentSceneName = sceneName;

                this.signalBus.Fire(new FinishLoadingNewSceneSignal
                {
                    CurrentScreenName = new List<string>() { CurrentSceneName },
                    TargetScreenName  = new List<string>() { sceneName },
                    ActiveScreenName  = sceneName
                });

                this.UnloadSceneAsync(oldScene.name, keptAssets).Forget();

                return loadHandle.Result;
            }

            var screenInstance = await Addressables.LoadSceneAsync(sceneName);

            CurrentSceneName = sceneName;

            this.signalBus.Fire(new FinishLoadingNewSceneSignal
            {
                CurrentScreenName = new List<string>() { CurrentSceneName },
                TargetScreenName  = new List<string>() { sceneName },
                ActiveScreenName  = sceneName
            });

            this.UnloadSceneAsync(oldSceneName, keptAssets).Forget();

            return screenInstance;
        }

        public virtual async UniTask<SceneInstance> ReloadCurrentScene(List<string> listNotClear = null)
        {
            var sceneName = CurrentSceneName;

            this.signalBus.Fire(new StartLoadingNewSceneSignal
            {
                CurrentScreenName = new List<string>() { CurrentSceneName },
                TargetScreenName  = new List<string>() { sceneName },
                ActiveScreenName  = sceneName
            });

            await this.UnloadSceneAsync(sceneName, listNotClear);
            CurrentSceneName = sceneName;
            var screenInstance = await this.GameAssets.LoadSceneAsync(sceneName);

            this.signalBus.Fire(new FinishLoadingNewSceneSignal
            {
                CurrentScreenName = new List<string>() { sceneName },
                TargetScreenName  = new List<string>() { sceneName },
                ActiveScreenName  = sceneName
            });

            return screenInstance;
        }

        public virtual async UniTask<SceneInstance> SoftReloadCurrentScene(List<string> listNotClear = null)
        {
            var oldScene = SceneManager.GetActiveScene();

            this.signalBus.Fire(new StartLoadingNewSceneSignal
            {
                CurrentScreenName = new List<string>() { CurrentSceneName },
                TargetScreenName  = new List<string>() { CurrentSceneName },
                ActiveScreenName  = CurrentSceneName
            });

            await this.UnloadSceneAsync(oldScene.name, listNotClear);
            var result = await Addressables.LoadSceneAsync(CurrentSceneName, LoadSceneMode.Additive);
            SceneManager.SetActiveScene(result.Scene);

            if (oldScene.IsValid())
            {
                var unloadOp = SceneManager.UnloadSceneAsync(oldScene);

                while (unloadOp is { isDone: false })
                    await UniTask.DelayFrame(1);
            }

            this.signalBus.Fire(new FinishLoadingNewSceneSignal
            {
                CurrentScreenName = new List<string>() { CurrentSceneName },
                TargetScreenName  = new List<string>() { CurrentSceneName },
                ActiveScreenName  = CurrentSceneName
            });

            return result;
        }

        //to backup for old version
        public virtual UniTask<SceneInstance> LoadSingleSceneAsync(string sceneName, LoadSceneMode loadMode = LoadSceneMode.Single, bool activeOnLoad = true)
        {
            return this.LoadSingleSceneByAddressableAsync(sceneName, loadMode, activeOnLoad);
        }

        /// <summary>
        /// For Preload scene and wait it
        /// </summary>
        /// <param name="sceneName"></param>
        /// <returns></returns>
        public virtual async UniTask<SceneInstance> LoadSceneInstanceByAddressAble(string sceneName, LoadSceneMode mode = LoadSceneMode.Single)
        {
            this.signalBus.Fire(new StartLoadingNewSceneSignal
            {
                CurrentScreenName = new List<string>() { CurrentSceneName },
                TargetScreenName  = new List<string>() { sceneName },
                ActiveScreenName  = sceneName
            });

            var screenInstance = await this.GameAssets.LoadSceneAsync(sceneName, mode, false);

            return screenInstance;
        }

        /// <summary>
        /// Active an scene instance
        /// </summary>
        /// <param name="sceneName"></param>
        /// <param name="sceneInstance"></param>
        public virtual async UniTask ActiveSceneInstance(string sceneName, SceneInstance sceneInstance)
        {
            var lastScene = CurrentSceneName;
            CurrentSceneName = sceneName;
            await this.UnloadSceneAsync(lastScene);
            _ = Resources.UnloadUnusedAssets();
            await sceneInstance.ActivateAsync();

            this.signalBus.Fire(new FinishLoadingNewSceneSignal
            {
                CurrentScreenName = new List<string>() { lastScene },
                TargetScreenName  = new List<string>() { sceneName },
                ActiveScreenName  = sceneName
            });
        }

        /// <summary>Load scene async by name </summary>
        public virtual async UniTask<SceneInstance> LoadSingleSceneByAddressableAsync(string sceneName, LoadSceneMode loadMode = LoadSceneMode.Single, bool activeOnLoad = true)
        {
            var screenInstance = await this.LoadSceneInstanceByAddressAble(sceneName, loadMode);

            var lastScene = CurrentSceneName;
            CurrentSceneName = sceneName;
            await this.UnloadSceneAsync(lastScene);
            _ = Resources.UnloadUnusedAssets();

            if (activeOnLoad)
            {
                await screenInstance.ActivateAsync();
            }

            await UniTask.DelayFrame(1);

            this.signalBus.Fire(new FinishLoadingNewSceneSignal
            {
                CurrentScreenName = new List<string>() { lastScene },
                TargetScreenName  = new List<string>() { sceneName },
                ActiveScreenName  = sceneName
            });

            return screenInstance;
        }

        public virtual async UniTask LoadSingleSceneBySceneManagerAsync(string sceneName)
        {
            this.signalBus.Fire(new StartLoadingNewSceneSignal
            {
                CurrentScreenName = new List<string>() { CurrentSceneName },
                TargetScreenName  = new List<string>() { sceneName },
                ActiveScreenName  = sceneName
            });

            var lastScene = CurrentSceneName;
            CurrentSceneName = sceneName;
            await SceneManager.LoadSceneAsync(sceneName);
            await this.GameAssets.UnloadUnusedAssets(lastScene);
            await Resources.UnloadUnusedAssets();

            this.signalBus.Fire(new FinishLoadingNewSceneSignal
            {
                CurrentScreenName = new List<string>() { lastScene },
                TargetScreenName  = new List<string>() { sceneName },
                ActiveScreenName  = sceneName
            });
        }

        public virtual async UniTask LoadMultipleSceneAsync(string activesScene, params string[] sceneNames)
        {
            this.signalBus.Fire(new StartLoadingNewSceneSignal
            {
                CurrentScreenName = new List<string>() { CurrentSceneName },
                TargetScreenName  = sceneNames.ToList(),
                ActiveScreenName  = activesScene
            });

            var lastScene = CurrentSceneName;
            CurrentSceneName = activesScene;
            var allTask = new List<UniTask>();

            for (var index = 0; index < sceneNames.Length; index++)
            {
                var sceneName = sceneNames[index];
                allTask.Add(this.GameAssets.LoadSceneAsync(sceneName, index == 0 ? LoadSceneMode.Single : LoadSceneMode.Additive).ToUniTask());
            }

            allTask.Add(Resources.UnloadUnusedAssets().ToUniTask());
            await this.GameAssets.UnloadUnusedAssets(lastScene);
            await UniTask.WhenAll(allTask);

            SceneManager.SetActiveScene(SceneManager.GetSceneByName(activesScene));

            this.signalBus.Fire(new FinishLoadingNewSceneSignal
            {
                CurrentScreenName = new List<string>() { lastScene },
                TargetScreenName  = sceneNames.ToList(),
                ActiveScreenName  = activesScene
            });
        }

        /// <summary>Unload scene async by name </summary>
        public virtual async UniTask UnloadSceneAsync(string sceneName, List<string> listNotClear = null)
        {
            this.GameAssets.UnloadSceneAsync(sceneName);
            await this.GameAssets.UnloadUnusedAssets(sceneName, listNotClear);
        }
    }
}