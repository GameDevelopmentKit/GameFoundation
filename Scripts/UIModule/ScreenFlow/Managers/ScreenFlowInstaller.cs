#if GDK_ZENJECT
namespace caojweldjflwendl.Scripts.UIModule.ScreenFlow.Managers
{
    using caojweldjflwendl.Scripts.UIModule.ScreenFlow.Signals;
    using caojweldjflwendl.Scripts.UIModule.Utilities.UIStuff;
    using caojweldjflwendl.Signals;
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