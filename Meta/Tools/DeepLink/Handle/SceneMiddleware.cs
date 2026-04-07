namespace DeepLink.Handle
{
    using Cysharp.Threading.Tasks;
    using GameFoundation.Scripts.UIModule.ScreenFlow.Managers;

    public class SceneMiddleware : ActionHandler<string>
    {
        private readonly SceneDirector sceneDirector;
        public override  string        Type => "scene";

        public SceneMiddleware(SceneDirector sceneDirector) { this.sceneDirector = sceneDirector; }

        protected override async UniTask ProcessInternal(string sceneName)
        {
            if (SceneDirector.CurrentSceneName != sceneName)
                await this.sceneDirector.LoadSingleSceneBySceneManagerAsync(sceneName);
        }
    }
}