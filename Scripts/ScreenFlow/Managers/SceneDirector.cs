namespace GameFoundation.Scripts.ScreenFlow.Managers
{
    using System.Collections.Generic;
    using System.Linq;
    using Cysharp.Threading.Tasks;
    using GameFoundation.Scripts.AssetLibrary;
    using GameFoundation.Scripts.ScreenFlow.Signals;
    using UnityEngine;
    using UnityEngine.SceneManagement;
    using Zenject;

    /// <summary>Load, unload scenes are wrapped here </summary>
    public class SceneDirector
    {
        private readonly SignalBus signalBus;
        public static    string    CurrentSceneName;
        public SceneDirector(SignalBus signalBus) { this.signalBus = signalBus; }

        /// <summary>Load scene async by name </summary>
        public async UniTask LoadSingleSceneAsync(string sceneName)
        {
            this.signalBus.Fire<StartLoadingNewSceneSignal>();
            var lastScene = CurrentSceneName;
            CurrentSceneName = sceneName;
            await GameAssets.LoadSceneAsync(sceneName);
            Resources.UnloadUnusedAssets();
            GameAssets.UnloadUnusedAssets(lastScene);
            this.signalBus.Fire<FinishLoadingNewSceneSignal>();
        }

        public async UniTask LoadMultipleSceneAsync(params string[] sceneNames)
        {
            this.signalBus.Fire<StartLoadingNewSceneSignal>();
            var lastScene = CurrentSceneName;
            CurrentSceneName = sceneNames[0];
            var allTask = new List<UniTask>();

            for (var index = 0; index < sceneNames.Length; index++)
            {
                var sceneName = sceneNames[index];
                allTask.Add(GameAssets.LoadSceneAsync(sceneName, index == 0 ? LoadSceneMode.Single : LoadSceneMode.Additive).ToUniTask());
            }

            allTask.Add(Resources.UnloadUnusedAssets().ToUniTask());
            GameAssets.UnloadUnusedAssets(lastScene);
            await UniTask.WhenAll(allTask);

            SceneManager.SetActiveScene(SceneManager.GetSceneByName(sceneNames.Last()));


            this.signalBus.Fire<FinishLoadingNewSceneSignal>();
        }

        /// <summary>Unload scene async by name </summary>
        public async UniTask UnloadSceneAsync(string sceneName)
        {
            await GameAssets.UnloadSceneAsync(sceneName);
            GameAssets.UnloadUnusedAssets(sceneName);
        }
    }
}