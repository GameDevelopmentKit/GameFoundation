namespace DataManager.Blueprint.BlueprintController
{
    using DataManager.Blueprint.APIHandler;
    using DataManager.Blueprint.BlueprintReader;
    using DataManager.Blueprint.BlueprintSource;
    using DataManager.Blueprint.Signals;
    using GameConfigs;
    using GameFoundation.Scripts.Utilities.Extension;
    using Zenject;

    /// <summary>
    /// Binding all services of the blueprint control flow at here
    /// </summary>
    public class BlueprintServicesInstaller : Installer<BlueprintServicesInstaller>
    {
        public override void InstallBindings()
        {
            //BindBlueprint reader for mobile
            this.Container.Bind<PreProcessBlueprintMobile>().AsCached().NonLazy();
            this.Container.Bind<FetchBlueprintInfo>().AsCached();
            this.Container.Bind<BlueprintDownloader>().AsCached();
            this.Container.Bind<BlueprintReaderManager>().AsCached();
            this.Container.Bind<BlueprintConfig>().FromResolveGetter<GDKConfig>(config => config.GetGameConfig<BlueprintConfig>()).AsCached();

            this.Container.BindAllDerivedTypes<IGenericBlueprintReader>();
            this.Container.BindInterfacesAndSelfToAllTypeDriveFrom<IBlueprintLoader>();

            this.Container.DeclareSignal<LoadBlueprintDataSucceedSignal>();
            this.Container.DeclareSignal<LoadBlueprintDataProgressSignal>();
            this.Container.DeclareSignal<ReadBlueprintProgressSignal>();
        }
    }
}