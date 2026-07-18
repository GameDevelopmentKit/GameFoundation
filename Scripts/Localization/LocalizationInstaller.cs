namespace Localization
{
    using Localization.Blueprint;
    using Localization.Signals;
    using Localization.UnityLocalization;
    using Zenject;

    public class LocalizationInstaller : Installer<LocalizationInstaller>
    {
        public override void InstallBindings()
        {
            this.Container.DeclareSignal<LocaleChangedSignal>();
            this.Container.BindInterfacesAndSelfTo<UnityLocalizationServices>().AsSingle().NonLazy();
            this.Container.Bind<LocalizationDataOnline>().AsSingle().NonLazy();
        }
    }
}
