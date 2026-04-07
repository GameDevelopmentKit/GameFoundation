namespace RequirementSystem.Manager
{
    using Zenject;

    public class RequirementSystemInstaller : Installer<RequirementSystemInstaller>
    {
        public override void InstallBindings()
        {
            this.Container.BindInterfacesTo<RequirementService>().AsSingle();
        }
    }
}