#if GDK_ZENJECT
namespace GameFoundation.Signals
{
    using MessagePipe;
    using UnityEngine.Scripting;
    using Zenject;

    [Preserve]
    public class SignalBusInstaller : Installer<SignalBusInstaller>
    {
        public override void InstallBindings()
        {
            this.Container.BindMessagePipe();
            this.Container.BindInterfacesAndSelfTo<SignalBus>().AsSingle().CopyIntoAllSubContainers();
        }
    }
}
#endif