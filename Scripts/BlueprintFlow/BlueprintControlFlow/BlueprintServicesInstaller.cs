namespace Mech.Core.BlueprintFlow.BlueprintControlFlow
{
    using Mech.Utils;
    using MechSharingCode.Blueprints.BlueprintControlFlow;
    using MechSharingCode.Blueprints.BlueprintReader;
    using MechSharingCode.Blueprints.Signals;
    using Zenject;

    /// <summary>
    /// Binding all services of the blueprint control flow at here
    /// </summary>
    public class BlueprintServicesInstaller : Installer<BlueprintServicesInstaller>
    {
        public override void InstallBindings()
        {
            this.Container.Bind<BlueprintDownloader>().WhenInjectedInto<BlueprintReaderManager>();
            this.Container.BindInterfacesAndSelfTo<BlueprintReaderManager>().AsSingle().NonLazy();

            this.Container.BindAllTypeDriveFrom<IGenericBlueprint>();

            this.Container.DeclareSignal<LoadBlueprintDataSignal>();
            this.Container.DeclareSignal<LoadBlueprintDataSuccessedSignal>();
            this.Container.DeclareSignal<LoadBlueprintDataProgressSignal>();
        }
    }
}