namespace VContainer.Context
{
    using System.Collections.Generic;
    using UnityEngine;
    using VContainer.Unity;

    public class ProjectContext : LifetimeScope
    {
        [SerializeField] private List<MonoInstaller> monoInstallers;

        protected override void Configure(IContainerBuilder builder)
        {
            foreach (var installer in this.monoInstallers)
            {
                installer.Install(builder);
            }
        }
    }
}