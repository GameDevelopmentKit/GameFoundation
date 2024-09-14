#if GDK_VCONTAINER
#nullable enable
namespace GameFoundation.Signals
{
    using GameFoundation.DI;
    using MessagePipe;
    using VContainer;

    public static class SignalBusVContainer
    {
        private static readonly MessagePipeOptions MessagePipeOptions = new();

        public static void RegisterSignalBus(this IContainerBuilder builder)
        {
            builder.Register<SignalBus>(Lifetime.Scoped).AsInterfacesAndSelf();
            builder.RegisterMessagePipe();
        }

        public static void DeclareSignal<TSignal>(this IContainerBuilder builder)
        {
            builder.RegisterMessageBroker<TSignal>(MessagePipeOptions);
        }
    }
}
#endif