namespace Zenject
{
    using MessagePipe;

    // Note that you only need to install this once
    public class SignalBusInstaller : Installer<SignalBusInstaller>
    {
        public override void InstallBindings()
        {
            this.Container.BindMessagePipe();
            this.Container.Bind<SignalBus>().AsSingle().CopyIntoAllSubContainers();
        }
    }
}