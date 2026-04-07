namespace SoundManager
{
    using GameFoundation.Scripts.Utilities;
    using Zenject;

    public class SoundInstaller: Installer<SoundInstaller>
    {
        public override void InstallBindings()
        {
            this.Container.BindInterfacesAndSelfTo<SoundEffectManager>().AsCached().NonLazy();
            this.Container.BindInterfacesAndSelfTo<MusicPlaylistManager>().AsCached().NonLazy();
            
            //note: AudioManager is singleton, rebind an inherited AudioManager will cause 2 instance and just one subcribes compositeDisposable 
            //do not rebind IAudioManager to another AudioManager, if do rebind, keep the inherited AudioManager and comment this
            this.Container.BindInterfacesTo<AudioManager>().AsSingle().NonLazy();
        }
    }
}