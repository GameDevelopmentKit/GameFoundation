#if GDK_VCONTAINER
#nullable enable
namespace caojweldjflwendl.Utilities.GameQueueAction
{
    using caojweldjflwendl.DI;
    using caojweldjflwendl.Scripts.UIModule.Utilities.GameQueueAction;
    using VContainer;

    public static class GameQueueActionVContainer
    {
        public static void RegisterGameQueueActionService(this IContainerBuilder builder)
        {
            builder.Register<GameQueueActionServices>(Lifetime.Singleton).AsInterfacesAndSelf();
            builder.Register<GameQueueActionContext>(Lifetime.Singleton);
        }
    }
}
#endif