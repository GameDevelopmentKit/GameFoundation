namespace BlueprintFlow.BlueprintControlFlow
{
    using BlueprintFlow.APIHandler;
    using BlueprintFlow.BlueprintReader;
    using BlueprintFlow.Signals;
    using GameFoundation.Scripts.Utilities.Extension;
    using Models;
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
            this.Container.Bind<FetchBlueprintInfo>().WhenInjectedInto<BlueprintReaderManager>();
            this.Container.Bind<BlueprintDownloader>().WhenInjectedInto<BlueprintReaderManager>();
            this.Container.Bind<BlueprintReaderManager>().AsCached();
            this.Container.Bind<BlueprintConfig>().FromResolveGetter<GDKConfig>(config => config.GetGameConfig<BlueprintConfig>()).AsCached();

            this.Container.BindAllTypeDriveFrom<IGenericBlueprintReader>();

            this.Container.DeclareSignal<LoadBlueprintDataSucceedSignal>();
            this.Container.DeclareSignal<LoadBlueprintDataProgressSignal>();
            this.Container.DeclareSignal<ReadBlueprintProgressSignal>();
        }
    }
}