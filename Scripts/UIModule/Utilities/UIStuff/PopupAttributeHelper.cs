namespace UIModule.Utilities.UIStuff
{
    using System.Linq;
    using GameFoundation.Scripts.UIModule.ScreenFlow.BaseScreen.Presenter;
    using GameFoundation.Scripts.UIModule.ScreenFlow.Signals;
    using Zenject;

    public class PopupAttributeHelper : IInitializable
    {
        #region inject

        private readonly SignalBus signalBus;

        public PopupAttributeHelper(SignalBus signalBus) { this.signalBus = signalBus; }

        #endregion

        public void Initialize() { this.signalBus.Subscribe<PopupShowedSignal>(this.OnPopupShowedHandler); }

        private void OnPopupShowedHandler(PopupShowedSignal obj)
        {
            var presenter  = obj.ScreenPresenter;
            var view       = ReflectionHelper.GetViewFromPresenter(presenter);
            var gameObject = view.RectTransform.gameObject;

            if (presenter.GetType().GetCustomAttributes(typeof(PopupInfoAttribute), true).FirstOrDefault() is PopupInfoAttribute popupInfoAttribute)
            {
                if (popupInfoAttribute.IsCloseWhenTapOutside)
                {
                    var invisibleBackground = gameObject.AddComponent<InvisibleBackground>();
                    invisibleBackground.SetupInvisibleBg(gameObject.transform.GetChild(0).transform, () =>
                    {
                        view.Close();
                    });
                }
            }
        }
    }
}