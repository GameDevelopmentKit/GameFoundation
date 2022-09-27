namespace GameFoundation.Scripts.BlueprintFlow.BlueprintControlFlow
{
    using GameFoundation.Scripts.BlueprintFlow.BlueprintReader;
    using GameFoundation.Scripts.BlueprintFlow.Signals;
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
            this.Container.Bind<BlueprintDownloader>().WhenInjectedInto<BlueprintReaderManager>();
            this.Container.Bind<BlueprintReaderManager>().AsCached();
            this.Container.Bind<BlueprintConfig>().AsCached();

            this.Container.BindAllTypeDriveFrom<IGenericBlueprintReader>();

            this.Container.DeclareSignal<LoadBlueprintDataSuccessedSignal>();
            this.Container.DeclareSignal<LoadBlueprintDataProgressSignal>();
            this.Container.DeclareSignal<ReadBlueprintProgressSignal>();
        }
    }
}