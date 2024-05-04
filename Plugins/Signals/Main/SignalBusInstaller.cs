namespace Zenject
{
    using MessagePipe;

    // Note that you only need to install this once
    public class SignalBusInstaller : Installer<SignalBusInstaller>
    {
        public static MessagePipeOptions Options;
        
        public override void InstallBindings()
        {
            this.Container.BindInterfacesAndSelfTo<SignalBus>().AsSingle().CopyIntoAllSubContainers();
            this.Configure(this.Container);
        }
        
        void Configure(DiContainer builder)
        {
            Options = builder.BindMessagePipe(ops =>
            {
                ops.InstanceLifetime = InstanceLifetime.Scoped;
            });
            GlobalMessagePipe.SetProvider(builder.AsServiceProvider());
        }
    }
}
