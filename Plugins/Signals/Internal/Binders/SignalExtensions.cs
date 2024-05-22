namespace Zenject
{
    using MessagePipe;

    public static class SignalExtensions
    {
        public static void DeclareSignal<TSignal>(this DiContainer container)
        {
            //Currently we only support project context signals
            ProjectContext.Instance.Container.BindMessageBroker<TSignal>(SignalBusInstaller.Options);
            //container.BindMessageBroker<TSignal>(SignalBusInstaller.Options);
        }
    }
}