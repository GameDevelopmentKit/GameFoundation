#if GDK_VCONTAINER
#nullable enable
namespace GameFoundation.Utilities.ApplicationServices
{
    using GameFoundation.Scripts.Utilities.ApplicationServices;
    using GameFoundation.Scripts.Utilities.UserData;
    using GameFoundation.Signals;
    using UnityEngine;
    using VContainer;
    using VContainer.Unity;

    public static class ApplicationServicesVContainer
    {
        public static void RegisterApplicationServices(this IContainerBuilder builder, Transform rootTransform)
        {
            builder.RegisterComponentOnNewGameObject<MinimizeAppService>(Lifetime.Singleton).UnderTransform(rootTransform);
            builder.RegisterBuildCallback(container => container.Resolve<MinimizeAppService>().Construct(container.Resolve<SignalBus>(), container.Resolve<IHandleUserDataServices>()));

            builder.DeclareSignal<ApplicationPauseSignal>();
            builder.DeclareSignal<ApplicationQuitSignal>();
            builder.DeclareSignal<UpdateTimeAfterFocusSignal>();
        }
    }
}
#endif