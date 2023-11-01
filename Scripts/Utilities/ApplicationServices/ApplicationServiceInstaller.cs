﻿namespace GameFoundation.Scripts.Utilities.ApplicationServices
{
    using Zenject;

    public class ApplicationServiceInstaller : Installer<ApplicationServiceInstaller>
    {
        public override void InstallBindings()
        {
            this.Container.Bind<MinimizeAppService>().FromNewComponentOnNewGameObject().AsSingle().NonLazy();
            this.Container.DeclareSignal<ApplicationPauseSignal>();
            this.Container.DeclareSignal<ApplicationQuitSignal>();
            this.Container.DeclareSignal<UpdateTimeAfterFocusSignal>();
        }
    }
}