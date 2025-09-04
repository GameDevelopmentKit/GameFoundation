namespace caojweldjflwendl.Scripts.UIModule.ScreenFlow
{
    using caojweldjflwendl.Scripts.UIModule.ScreenFlow.BaseScreen.View;
    using caojweldjflwendl.Scripts.UIModule.ScreenFlow.Managers;

    public static class ScreenHelper
    {
        public static string GetScreenId<TView>() where TView : IScreenView
        {
            return $"{SceneDirector.CurrentSceneName}/{typeof(TView).Name}";
        }
    }
}