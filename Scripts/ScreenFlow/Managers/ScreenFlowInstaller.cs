namespace Mech.Core.ScreenFlow.Managers
{
    using Mech.Core.ScreenFlow.Signals;
    using Zenject;

    public class ScreenFlowInstaller: Installer<ScreenFlowInstaller>
    {
        public override void InstallBindings()
        {
            this.Container.Bind<SceneDirector>().AsSingle().NonLazy();
            this.Container.BindInterfacesAndSelfTo<ScreenManager>().FromNewComponentOnNewGameObject().AsSingle().NonLazy();
            this.Container.DeclareSignal<StartLoadingNewSceneSignal>();
            this.Container.DeclareSignal<FinishLoadingNewSceneSignal>();
            this.Container.DeclareSignal<ScreenHideSignal>();
            this.Container.DeclareSignal<ScreenShowSignal>();
            this.Container.DeclareSignal<ManualInitScreenSignal>();
            this.Container.DeclareSignal<ScreenSelfDestroyedSignal>();
            this.Container.DeclareSignal<PopupShowedSignal>();
            this.Container.DeclareSignal<PopupHiddenSignal>();
            this.Container.DeclareSignal<PopupBlurBgShowedSignal>();
        }
    }
}