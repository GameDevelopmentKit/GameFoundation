namespace VContainer.Signals
{
    using VContainer;

    namespace GameFoundation.Signals
    {
        using MessagePipe;

        public static class SignalBusVContainer
        {
            private static readonly MessagePipeOptions MessagePipeOptions = new();

            public static void InstallSignalBus(this IContainerBuilder builder)
            {
                builder.Register<SignalBus>(Lifetime.Scoped).AsImplementedInterfaces();
                builder.RegisterMessagePipe();
            }

            public static void DeclareSignal<TSignal>(this IContainerBuilder builder)
            {
                builder.RegisterMessageBroker<TSignal>(MessagePipeOptions);
            }
        }
    }
}