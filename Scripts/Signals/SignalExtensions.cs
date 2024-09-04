#if GDK_ZENJECT
namespace Zenject
{
    using MessagePipe;

    public static class SignalExtensions
    {
        private static readonly MessagePipeOptions Options = new();

        public static void DeclareSignal<TSignal>(this DiContainer container)
        {
            container.BindMessageBroker<TSignal>(Options);
        }
    }
}
#endif