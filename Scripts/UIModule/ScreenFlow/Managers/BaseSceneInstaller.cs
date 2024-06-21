namespace GameFoundation.Scripts.UIModule.ScreenFlow.Managers
{
    using System;
    using UnityEngine;
    using UnityEngine.SceneManagement;
    using Zenject;
#if UNITY_EDITOR
    using UnityEditor;
    using UnityEditor.SceneManagement;
#endif

    /// <summary>
    /// Every Mono Scene Installer will be inherited this class
    /// </summary>
    public class BaseSceneInstaller : MonoInstaller
    {
        /// <summary>
        /// Instance of Root UI Canvas on Scene
        /// </summary>
        [SerializeField] protected RootUICanvas rootUICanvas;

        [Inject] private IScreenManager screenManager;

        public override void InstallBindings()
        {
            //todo this should be setup automatically
            if (this.rootUICanvas == null) return;
            this.screenManager.RootUICanvas       = this.rootUICanvas;
            this.screenManager.CurrentRootScreen  = this.rootUICanvas.RootUIShowTransform;
            this.screenManager.CurrentHiddenRoot  = this.rootUICanvas.RootUIClosedTransform;
            this.screenManager.CurrentOverlayRoot = this.rootUICanvas.RootUIOverlayTransform;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            var activeScene  = SceneManager.GetActiveScene();
            var sceneContext = this.GetComponent<SceneContext>();

            if (!sceneContext.AutoInjectInHierarchy)
            {
                Debug.LogWarning($"{activeScene.name}: SceneContext AutoInjectInHierarchy should false");
            }
        }
#endif
    }
}