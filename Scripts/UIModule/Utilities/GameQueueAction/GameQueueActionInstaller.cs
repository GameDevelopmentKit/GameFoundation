namespace GameFoundation.Scripts.UIModule.Utilities.GameQueueAction
{
    using Zenject;

    public class GameQueueActionInstaller : Installer<GameQueueActionInstaller>
    {
        public override void InstallBindings()
        {
            this.Container.Bind<GameQueueActionServices>().AsCached();
            this.Container.Bind<GameQueueActionContext>().AsCached();
        }
    }
}