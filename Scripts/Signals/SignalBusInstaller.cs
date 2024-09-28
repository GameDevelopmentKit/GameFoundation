#if GDK_ZENJECT
namespace GameFoundation.Signals
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