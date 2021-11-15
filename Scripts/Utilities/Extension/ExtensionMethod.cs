namespace Mech.Utils
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Cysharp.Threading.Tasks;
    using Mech.Core.AssetLibrary;
    using Mech.Core.MVP;
    using Mech.Core.ScreenFlow.BaseScreen.View;
    using UnityEngine;
    using UnityEngine.UI;
    using Zenject;

    //<summary>
    //Manager all extension method
    //</summary>
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

        //Create view
        public static async Task<T> CreateView<T>(this IUIPresenter iScreen, Transform parent) where T : IUIView
        {
            var viewObject = Object.Instantiate(await GameAssets.LoadAssetAsync<GameObject>(typeof(T).Name), parent).GetComponent<T>();
            return viewObject;
        }

        public static void InstantiateUIPresenter<TPresenter, TView, TModel>(this IInstantiator instantiator, ref TPresenter presenter, TView view, TModel model)
            where TPresenter : IUIItemPresenter<TView, TModel> where TView : IUIView
        {
            if (presenter == null)
            {
                presenter = instantiator.Instantiate<TPresenter>();
                presenter.SetView(view);
            }

            presenter.BindData(model);
        }

        //FillChild Width with parent Width
        public static void FillChildWidthWithParentWidth(this IUIPresenter presenter, RectTransform childRect, RectTransform parentRect)
        {
            var v = childRect.sizeDelta;
            v.x                 = parentRect.rect.width;
            childRect.sizeDelta = v;
        }


        public static async void Add<TPresenter, TModel>(this List<TPresenter> listPresenter, TPresenter presenter, Transform parentView, TModel model) where TPresenter : IUIItemPresenter<IUIView, TModel> 
        {
            await presenter.SetView(parentView);
            presenter.BindData(model);
            listPresenter.Add(presenter);
        }
    }
}