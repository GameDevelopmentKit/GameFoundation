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
    using VContainer;
    using VContainer.Internal;

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
    }
}