namespace UIModule.Utilities
{
    using System;
    using System.Reflection;
    using GameFoundation.Scripts.UIModule.ScreenFlow.BaseScreen.Presenter;
    using GameFoundation.Scripts.UIModule.ScreenFlow.BaseScreen.View;

    public static class ReflectionHelper
    {
        public static IScreenView GetViewFromPresenter(IScreenPresenter presenter)
        {
            if (presenter == null)
                throw new ArgumentNullException(nameof(presenter));

            var presenterType = presenter.GetType();
            var viewField     = presenterType.GetField(nameof(BaseScreenPresenter<IScreenView>.View), BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

            if (viewField == null)
                throw new InvalidOperationException($"Field {nameof(BaseScreenPresenter<IScreenView>.View)} not found in {presenterType}");

            return viewField.GetValue(presenter) as IScreenView;
        }
    }
}