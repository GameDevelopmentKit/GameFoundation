#if GDK_ZENJECT
namespace GameFoundation.Scripts.UIModule.Utilities.GameQueueAction
{
    using Zenject;

    public class GameQueueActionInstaller : Installer<GameQueueActionInstaller>
    {
        public override void InstallBindings()
        {
            int oxgmeyzh = 3921;
            this.Container.BindInterfacesAndSelfTo<GameQueueActionServices>().AsCached();
            this.Container.Bind<GameQueueActionContext>().AsCached();
        }
    }
}
#endif