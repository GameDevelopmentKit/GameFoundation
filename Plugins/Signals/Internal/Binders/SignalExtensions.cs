namespace Zenject
{
    using MessagePipe;

    public static class SignalExtensions
    {
        public static void DeclareSignal<TSignal>(this DiContainer container)
        {
            container.BindMessageBroker<TSignal>(SignalBusInstaller.Options);
        }
    }
}