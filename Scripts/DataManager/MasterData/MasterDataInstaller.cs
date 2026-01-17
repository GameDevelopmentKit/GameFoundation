namespace DataManager.MasterData
{
    using Zenject;
    public class MasterDataInstaller: Installer<MasterDataInstaller>
    {

        public override void InstallBindings()
        {
            this.Container.DeclareSignal<MasterDataReadySignal>();
            this.Container.DeclareSignal<MasterDataRegisterSignal>();
            
            this.Container.BindInterfacesAndSelfTo<MasterDataManager>().AsSingle().NonLazy();
        }
    }
}
