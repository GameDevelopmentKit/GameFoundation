#if GDK_ZENJECT
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
        public override void InstallBindings()
        {
        }

        #if UNITY_EDITOR
        private void OnValidate()
        {
            var activeScene = SceneManager.GetActiveScene();
            var sceneContext = this.GetComponent<SceneContext>();
            if (!sceneContext.AutoInjectInHierarchy)
            {
                sceneContext.AutoInjectInHierarchy = true;
                Debug.LogException(new Exception($"{activeScene.name}: SceneContext AutoInjectInHierarchy should be true, commit this scene please!!!"));
                EditorApplication.delayCall += MarkSceneDirtyOnce;

                void MarkSceneDirtyOnce()
                {
                    // Remove the delegate after executing to prevent multiple calls
                    EditorApplication.delayCall -= MarkSceneDirtyOnce;

                    // Check if the current scene is already marked as dirty to avoid unnecessary operations
                    if (!activeScene.isDirty)
                    {
                        EditorSceneManager.MarkSceneDirty(
                            activeScene
                        );
                        EditorSceneManager.SaveScene(activeScene, activeScene.path);
                    }
                }

                EditorApplication.ExitPlaymode();
            }
        }
        #endif
    }
}
#endif