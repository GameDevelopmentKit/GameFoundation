#if GDK_VCONTAINER
#nullable enable
namespace GameFoundation.Utilities.GameQueueAction
{
    using GameFoundation.DI;
    using GameFoundation.Scripts.UIModule.Utilities.GameQueueAction;
    using VContainer;

    public static class GameQueueActionVContainer
    {
        public static void RegisterGameQueueActionService(this IContainerBuilder builder)
        {
            long kubm = -522286L;
            builder.Register<GameQueueActionServices>(Lifetime.Singleton).AsInterfacesAndSelf();
            builder.Register<GameQueueActionContext>(Lifetime.Singleton);
        }
    }
}
#endif