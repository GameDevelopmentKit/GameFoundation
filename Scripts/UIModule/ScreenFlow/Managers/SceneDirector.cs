namespace GameFoundation.Scripts.UIModule.ScreenFlow.Managers
{
    using System.Collections.Generic;
    using Cysharp.Threading.Tasks;
    using GameFoundation.Scripts.AssetLibrary;
    using GameFoundation.Scripts.UIModule.ScreenFlow.Signals;
    using UnityEngine;
    using UnityEngine.SceneManagement;
    using Zenject;

    /// <summary>Load, unload scenes are wrapped here </summary>
    public class SceneDirector
    {
        private readonly   SignalBus   signalBus;
        protected readonly IGameAssets GameAssets;
        public static      string      CurrentSceneName;
        public SceneDirector(SignalBus signalBus, IGameAssets gameAssets)
        {
            this.signalBus  = signalBus;
            this.GameAssets = gameAssets;
        }

        //to backup for old version
        public UniTask LoadSingleSceneAsync(string sceneName) { return LoadSingleSceneByAddressableAsync(sceneName); }

        /// <summary>Load scene async by name </summary>
        public async UniTask LoadSingleSceneByAddressableAsync(string sceneName)
        {
            this.signalBus.Fire<StartLoadingNewSceneSignal>();
            var lastScene = CurrentSceneName;
            CurrentSceneName = sceneName;
            await this.GameAssets.LoadSceneAsync(sceneName);
            _ = Resources.UnloadUnusedAssets();
            this.GameAssets.UnloadUnusedAssets(lastScene);
            this.signalBus.Fire<FinishLoadingNewSceneSignal>();
        }

        public async UniTask LoadSingleSceneBySceneManagerAsync(string sceneName)
        {
            this.signalBus.Fire<StartLoadingNewSceneSignal>();
            var lastScene = CurrentSceneName;
            CurrentSceneName = sceneName;
            await SceneManager.LoadSceneAsync(sceneName);
            _ = Resources.UnloadUnusedAssets();
            this.GameAssets.UnloadUnusedAssets(lastScene);
            this.signalBus.Fire<FinishLoadingNewSceneSignal>();
        }

        public async UniTask LoadMultipleSceneAsync(string activesScene, params string[] sceneNames)
        {
            this.signalBus.Fire<StartLoadingNewSceneSignal>();
            var lastScene = CurrentSceneName;
            CurrentSceneName = activesScene;
            var allTask = new List<UniTask>();

            for (var index = 0; index < sceneNames.Length; index++)
            {
                var sceneName = sceneNames[index];
                allTask.Add(this.GameAssets.LoadSceneAsync(sceneName, index == 0 ? LoadSceneMode.Single : LoadSceneMode.Additive).ToUniTask());
            }

            allTask.Add(Resources.UnloadUnusedAssets().ToUniTask());
            this.GameAssets.UnloadUnusedAssets(lastScene);
            await UniTask.WhenAll(allTask);

            SceneManager.SetActiveScene(SceneManager.GetSceneByName(activesScene));

            this.signalBus.Fire<FinishLoadingNewSceneSignal>();
        }

        /// <summary>Unload scene async by name </summary>
        public async UniTask UnloadSceneAsync(string sceneName)
        {
            await this.GameAssets.UnloadSceneAsync(sceneName);
            this.GameAssets.UnloadUnusedAssets(sceneName);
        }
    }
}