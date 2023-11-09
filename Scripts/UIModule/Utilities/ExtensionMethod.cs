namespace GameFoundation.Scripts.UIModule.Utilities
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using GameFoundation.Scripts.UIModule.MVP;
    using GameFoundation.Scripts.UIModule.ScreenFlow.BaseScreen.Presenter;
    using GameFoundation.Scripts.UIModule.ScreenFlow.BaseScreen.View;
    using GameFoundation.Scripts.UIModule.ScreenFlow.Signals;
    using GameFoundation.Scripts.UIModule.Utilities.UIStuff;
    using GameFoundation.Scripts.Utilities.Extension;
    using TMPro;
    using UnityEngine;
    using UnityEngine.UI;
    using Zenject;

    public static class ExtensionMethod
    {
        //Remove all Button Listener On View
        public static void OnRemoveButtonListener(this MonoBehaviour view)
        {
            var buttons = view.GetComponentsInChildren<Button>();

            foreach (var b in buttons)
            {
                b.onClick.RemoveAllListeners();
            }
        }

        //check Object trigger With other object
        public static bool CheckObjectOnBound(this BaseView view, Bounds bounds, Bounds g) { return bounds.Intersects(g); }

        public static void InstantiateUIPresenter<TPresenter, TView, TModel>(this IInstantiator instantiator, ref TPresenter presenter, TView view, TModel model)
            where TPresenter : IUIItemPresenter<TView, TModel>
        {
            if (presenter == null)
            {
                presenter = instantiator.Instantiate<TPresenter>();
                presenter.SetView(view);
            }

            presenter.BindData(model);
        }

        public static async Task<TPresenter> InstantiateUIPresenter<TPresenter, TModel>(this IInstantiator instantiator, Transform parentView, TModel model)
            where TPresenter : IUIItemPresenter<IUIView, TModel>
        {
            var presenter = instantiator.Instantiate<TPresenter>();
            await presenter.SetView(parentView);
            presenter.BindData(model);

            return presenter;
        }

        //FillChild Width with parent Width
        public static void FillChildWidthWithParentWidth(this IUIPresenter presenter, RectTransform childRect, RectTransform parentRect)
        {
            var v = childRect.sizeDelta;
            v.x                 = parentRect.rect.width;
            childRect.sizeDelta = v;
        }

        public static async void Add<TPresenter, TModel>(this List<TPresenter> listPresenter, TPresenter presenter, Transform parentView, TModel model)
            where TPresenter : IUIItemPresenter<IUIView, TModel>
        {
            await presenter.SetView(parentView);
            presenter.BindData(model);
            listPresenter.Add(presenter);
        }

        //mechText extension
        public static void SetTextLocalization(this TextMeshProUGUI t, string key, Color color = default)
        {
            var mechTextMeshPro = t.GetComponent<MechTextMeshPro>();

            if (mechTextMeshPro == null)
            {
                Debug.Log($"{t.gameObject.name} have no MechTextPro");

                return;
            }

            mechTextMeshPro.SetTextWithLocalization(key, color);
        }

        public static void SetTextLocalization(this TMP_InputField t, string key)
        {
            var mechTextMeshPro = t.textComponent.GetComponent<MechTextMeshPro>();

            if (mechTextMeshPro == null)
            {
                return;
            }

            mechTextMeshPro.SetTextWithLocalization(key);
        }

        /// <summary>
        /// Utils use to initialize a screen presenter manually, and the view is already initialized on the scene
        /// </summary>
        /// <param name="container"></param>
        /// <param name="autoBindData"></param>
        /// <typeparam name="T"> Type of screen presenter</typeparam>
        public static void InitScreenManually<T>(this DiContainer container, bool autoBindData = false) where T : IScreenPresenter
        {
            container.Bind<T>().AsSingle().OnInstantiated<T>((context, presenter) =>
                {
                    context.Container.Resolve<SignalBus>().Fire(new ManualInitScreenSignal()
                    {
                        ScreenPresenter   = presenter,
                        IncludingBindData = autoBindData
                    });
                })
                .NonLazy();
        }
    }
}