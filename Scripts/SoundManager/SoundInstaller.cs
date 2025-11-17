namespace SoundManager
{
    using Zenject;

    public class SoundInstaller: Installer<SoundInstaller>
    {
        public override void InstallBindings()
        {
            this.Container.Bind<SoundEffectManager>().AsCached().NonLazy();
            this.Container.Bind<MusicPlaylistManager>().AsCached().NonLazy();
        }
    }
}