namespace Utilities.SoundServices
{
    using Zenject;

    public class SoundManagerInstaller : Installer<MasterAaaSoundMasterModel, SoundManagerInstaller>
    {
        private readonly MasterAaaSoundMasterModel masterAaaSoundMasterModel;

        public SoundManagerInstaller(MasterAaaSoundMasterModel masterAaaSoundMasterModel) { this.masterAaaSoundMasterModel = masterAaaSoundMasterModel; }

        public override void InstallBindings()
        {
            this.Container.Bind<MasterAaaSoundMasterModel>().FromInstance(this.masterAaaSoundMasterModel).WhenInjectedInto<MasterAAASoundWrapper>();
            this.Container.BindInterfacesAndSelfTo<MasterAAASoundWrapper>().AsCached().NonLazy();
        }
    }
}