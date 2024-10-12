namespace GameFoundation.Scripts.UIModule.Utilities
{
    using System.Collections.Generic;
    using Cysharp.Threading.Tasks;
    using GameFoundation.DI;
    using GameFoundation.Scripts.UIModule.MVP;
    using GameFoundation.Scripts.UIModule.ScreenFlow.BaseScreen.Presenter;
    using GameFoundation.Scripts.UIModule.ScreenFlow.BaseScreen.View;
    using GameFoundation.Scripts.UIModule.ScreenFlow.Signals;
    using GameFoundation.Signals;
    using UnityEngine;
    using UnityEngine.UI;
    #if GDK_ZENJECT
    using Zenject;
    #endif
    #if GDK_VCONTAINER
    using VContainer;
    #endif

    public static class ExtensionMethod
    {
        //Remove all Button Listener On View
        public static void OnRemoveButtonListener(this MonoBehaviour view)
        {
            var buttons = view.GetComponentsInChildren<Button>();

            foreach (var b in buttons) b.onClick.RemoveAllListeners();
        }

        //check Object trigger With other object
        public static bool CheckObjectOnBound(this BaseView view, Bounds bounds, Bounds g)
        {
            return bounds.Intersects(g);
        }

        public static void InstantiateUIPresenter<TPresenter, TView, TModel>(this IDependencyContainer container, ref TPresenter presenter, TView view, TModel model)
            where TPresenter : IUIItemPresenter<TView, TModel> where TView : IUIView
        {
            if (presenter == null)
            {
                presenter = container.Instantiate<TPresenter>();
                presenter.SetView(view);
            }
            presenter.BindData(model);
        }

        public static async UniTask<TPresenter> InstantiateUIPresenter<TPresenter, TModel>(this IDependencyContainer container, Transform parentView, TModel model)
            where TPresenter : IUIItemPresenter<IUIView, TModel>
        {
            var presenter = container.Instantiate<TPresenter>();
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

        #if GDK_ZENJECT
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
                    ScreenPresenter = presenter,
                    IncludingBindData = autoBindData,
                });
            }).NonLazy();
        }
        #endif

        #if GDK_VCONTAINER
        public static void InitScreenManually<T>(this IContainerBuilder builder, bool autoBindData = false) where T : IScreenPresenter
        {
            builder.RegisterBuildCallback(container => container.Resolve<SignalBus>().Fire(new ManualInitScreenSignal
            {
                ScreenPresenter   = container.Instantiate<T>(),
                IncludingBindData = autoBindData,
            }));
        }
        #endif
    }
}