namespace UIModule.ScreenFlow
{
    using GameFoundation.Scripts.UIModule.ScreenFlow.BaseScreen.View;
    using GameFoundation.Scripts.UIModule.ScreenFlow.Managers;

    public static class ScreenHelper
    {
        public static string GetScreenId<TView>() where TView : IScreenView => $"{SceneDirector.CurrentSceneName}/{typeof(TView).Name}";
    }
}