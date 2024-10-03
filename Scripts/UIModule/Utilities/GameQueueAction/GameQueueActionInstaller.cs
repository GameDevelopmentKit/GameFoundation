#if GDK_ZENJECT
namespace GameFoundation.Scripts.UIModule.Utilities.GameQueueAction
{
    using UnityEngine.Scripting;
    using Zenject;

    [Preserve]
    public class GameQueueActionInstaller : Installer<GameQueueActionInstaller>
    {
        public override void InstallBindings()
        {
            this.Container.BindInterfacesAndSelfTo<GameQueueActionServices>().AsCached();
            this.Container.Bind<GameQueueActionContext>().AsCached();
        }
    }
}
#endif