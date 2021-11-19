namespace GameFoundation.Scripts.ScreenFlow.Managers
{
    using Zenject;

    public class ARSceneDirector : SceneDirector
    {
        
        public ARSceneDirector(SignalBus signalBus) : base(signalBus)
        {
        }

        #region Shortcut

        public async void LoadLoginScene() => await this.LoadSingleSceneAsync(SceneName.Login);
        public async void LoadARScene()    => await this.LoadSingleSceneAsync(SceneName.AR);

        #endregion
    }
}
