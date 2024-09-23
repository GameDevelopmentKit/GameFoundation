namespace GameFoundation.Scripts.UIModule.ScreenFlow.Managers
{
    using UnityEngine;
    using VContainer;
    using VContainer.Context;

    public class BaseSceneInstaller : MonoInstaller
    {
        [SerializeField] protected RootUICanvas rootUICanvas;

        public override void Install(IContainerBuilder builder)
        {
            //todo this should be setup automatically
            if (this.rootUICanvas == null) return;
            builder.RegisterBuildCallback(container =>
                                          {
                                              var screenManager = container.Resolve<IScreenManager>();
                                              screenManager.RootUICanvas       = this.rootUICanvas;
                                              screenManager.CurrentRootScreen  = this.rootUICanvas.RootUIShowTransform;
                                              screenManager.CurrentHiddenRoot  = this.rootUICanvas.RootUIClosedTransform;
                                              screenManager.CurrentOverlayRoot = this.rootUICanvas.RootUIOverlayTransform;
                                          });
        }
    }
}