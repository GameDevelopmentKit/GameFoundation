#if GDK_ZENJECT
namespace GameFoundation.Scripts.Utilities.ApplicationServices
{
    using GameFoundation.Scripts.Utilities.UserData;
    using GameFoundation.Signals;
    using Zenject;

    public class ApplicationServiceInstaller : Installer<ApplicationServiceInstaller>
    {
        public override void InstallBindings()
        {
            this.Container.Bind<MinimizeAppService>()
                .FromNewComponentOnNewGameObject()
                .AsSingle()
                .OnInstantiated<MinimizeAppService>((ctx, svc) => svc.Construct(ctx.Container.Resolve<SignalBus>(), ctx.Container.Resolve<IHandleUserDataServices>()))
                .NonLazy();
            this.Container.DeclareSignal<ApplicationPauseSignal>();
            this.Container.DeclareSignal<ApplicationQuitSignal>();
            this.Container.DeclareSignal<UpdateTimeAfterFocusSignal>();
        }
    }
}
#endif