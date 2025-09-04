#if GDK_ZENJECT
namespace caojweldjflwendl.Signals
{
    using MessagePipe;
    using Zenject;

    public class SignalBusInstaller : Installer<SignalBusInstaller>
    {
        public override void InstallBindings()
        {
            this.Container.BindMessagePipe();
            this.Container.BindInterfacesAndSelfTo<SignalBus>().AsSingle().CopyIntoAllSubContainers();
        }
    }
}
#endif