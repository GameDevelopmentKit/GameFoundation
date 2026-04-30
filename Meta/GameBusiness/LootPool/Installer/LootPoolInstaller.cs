namespace GameBusiness.LootPool.Installer
{
    using GameBusiness.LootPool.PayoutService;
    using GameBusiness.LootPool.Service;
    using Zenject;

    public class LootPoolInstaller : Installer<LootPoolInstaller>
    {
        public override void InstallBindings()
        {
            this.Container.Bind<ILootPoolRandomProvider>().To<UnityLootPoolRandomProvider>().AsSingle();
            this.Container.Bind<LootPoolService>().AsSingle();
        }
    }
}
