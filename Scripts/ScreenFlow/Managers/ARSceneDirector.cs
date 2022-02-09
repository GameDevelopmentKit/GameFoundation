namespace GameFoundation.Scripts.ScreenFlow.Managers
{
    using Zenject;

    public static class SceneName
    {
        public const string Loading = "LoadingScene";
        public const string Main    = "MainScene";
        public const string Login   = "LoginScene";
        public const string AR      = "MechaAR";
    }

    public class ARSceneDirector : SceneDirector
    {
        public ARSceneDirector(SignalBus signalBus) : base(signalBus) { }

        #region Shortcut

        public async void LoadLoadingScene() => await this.LoadSingleSceneAsync(SceneName.Loading);
        public async void LoadMainScene()    => await this.LoadSingleSceneAsync(SceneName.Main);
        public async void LoadLoginScene()   => await this.LoadSingleSceneAsync(SceneName.Login);
        public async void LoadARScene()      => await this.LoadSingleSceneAsync(SceneName.AR);

        #endregion
    }
}