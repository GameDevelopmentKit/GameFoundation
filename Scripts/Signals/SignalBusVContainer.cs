#if GDK_VCONTAINER
#nullable enable
namespace GameFoundation.Signals
{
    using MessagePipe;
    using VContainer;

    public static class SignalBusVContainer
    {
        private static readonly MessagePipeOptions MessagePipeOptions = new();

        public static void RegisterSignalBus(this IContainerBuilder builder)
        {
            builder.Register<SignalBus>(Lifetime.Scoped).AsSelf().AsImplementedInterfaces();
            builder.RegisterMessagePipe();
        }

        public static void DeclareSignal<TSignal>(this IContainerBuilder builder)
        {
            builder.RegisterMessageBroker<TSignal>(MessagePipeOptions);
        }
    }
}
#endif