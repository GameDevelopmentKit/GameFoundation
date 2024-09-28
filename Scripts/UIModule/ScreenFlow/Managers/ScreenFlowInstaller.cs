#if GDK_ZENJECT
namespace GameFoundation.Scripts.UIModule.ScreenFlow.Managers
{
    using GameFoundation.Scripts.UIModule.ScreenFlow.Signals;
    using GameFoundation.Scripts.UIModule.Utilities.UIStuff;
    using GameFoundation.Signals;
    using Zenject;

    public class ScreenFlowInstaller : Installer<ScreenFlowInstaller>
    {
        public override void InstallBindings()
        {
            this.Container.Bind<SceneDirector>().AsSingle();
            this.Container.BindInterfacesAndSelfTo<ScreenManager>().AsSingle();
            this.Container.DeclareSignal<StartLoadingNewSceneSignal>();
            this.Container.DeclareSignal<FinishLoadingNewSceneSignal>();
            this.Container.DeclareSignal<ScreenCloseSignal>();
            this.Container.DeclareSignal<ScreenShowSignal>();
            this.Container.DeclareSignal<ScreenHideSignal>();
            this.Container.DeclareSignal<ManualInitScreenSignal>();
            this.Container.DeclareSignal<ScreenSelfDestroyedSignal>();
            this.Container.DeclareSignal<PopupShowedSignal>();
            this.Container.DeclareSignal<PopupHiddenSignal>();
            this.Container.DeclareSignal<PopupBlurBgShowedSignal>();

            this.Container.Bind<AutoCooldownTimer>().AsTransient();
        }
    }
}
#endif