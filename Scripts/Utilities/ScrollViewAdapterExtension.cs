namespace GameFoundation.Scripts.Utilities
{
    using System.Collections.Generic;
    using Cysharp.Threading.Tasks;
    using GameFoundation.Scripts.Adapter;
    using GameFoundation.Scripts.MVP;
    using Zenject;

    public static class ScrollViewAdapterExtension
    {
        //List
        public static async void InitItemAdapter(this BasicListAdapter adapter, int count)
        {
            await UniTask.WaitUntil(() => adapter.IsInitialized);
            adapter.ResetItems(count);
        }

        //Grid
        public static async void InitItemAdapter(this BasicGridAdapter adapter, int count)
        {
            await UniTask.WaitUntil(() => adapter.IsInitialized);
            adapter.ResetItems(count);
        }

        //Bind data for item with List Model
        public static void BindDataItemOnAdapter<TModel, TView, TPresenter>(this IUIItemPresenter itemPresenter, MyListItemViewsHolder v,
            DiContainer diContainer, List<TModel> models, ref List<TPresenter> listPresenter) where TView : TViewMono where TPresenter : BaseUIItemPresenter<TView, TModel>
        {
            var index      = v.ItemIndex;
            var model      = models[index];
            var viewObject = v.root.GetComponentInChildren<TView>(true);
            if (listPresenter.Count == index)
            {
                var p = diContainer.Instantiate<TPresenter>();
                p.SetView(viewObject);
                p.BindData(model);
                listPresenter.Add(p);
            }
            else
            {
                listPresenter[index].SetView(viewObject);
                listPresenter[index].BindData(model);
            }
        }

        public static void BindDataItemOnAdapter<TModel, TView, TPresenter>(this IUIItemPresenter itemPresenter, MyGridItemViewsHolder v,
            DiContainer diContainer, List<TModel> models, ref List<TPresenter> listPresenter) where TView : TViewMono where TPresenter : BaseUIItemPresenter<TView, TModel>
        {
            var index      = v.ItemIndex;
            var model      = models[index];
            var viewObject = v.root.GetComponentInChildren<TView>(true);
            if (listPresenter.Count == index)
            {
                var p = diContainer.Instantiate<TPresenter>();
                p.SetView(viewObject);
                p.BindData(model);
                listPresenter.Add(p);
            }
            else
            {
                listPresenter[index].SetView(viewObject);
                listPresenter[index].BindData(model);
            }
        }


        //Bind data for item with single model
        public static void BindDataItemOnAdapter<TModel, TView, TPresenter>(this BasicListAdapter baseAdapter, MyListItemViewsHolder v,
            DiContainer diContainer, TModel model, ref List<TPresenter> listPresenter) where TView : TViewMono where TPresenter : BaseUIItemPresenter<TView, TModel>
        {
            var index      = v.ItemIndex;
            var viewObject = v.root.GetComponentInChildren<TView>(true);
            if (listPresenter.Count == index)
            {
                var p = diContainer.Instantiate<TPresenter>();
                p.SetView(viewObject);
                p.BindData(model);
                listPresenter.Add(p);
            }
            else
            {
                listPresenter[index].SetView(viewObject);
                listPresenter[index].BindData(model);
            }
        }

        //grid
        public static void BindDataItemOnAdapter<TModel, TView, TPresenter>(this BasicGridAdapter baseAdapter, MyGridItemViewsHolder v,
            DiContainer diContainer, TModel model, ref List<TPresenter> listPresenter) where TView : TViewMono where TPresenter : BaseUIItemPresenter<TView, TModel>
        {
            var index      = v.ItemIndex;
            var viewObject = v.root.GetComponentInChildren<TView>(true);
            if (listPresenter.Count == index)
            {
                var p = diContainer.Instantiate<TPresenter>();
                p.SetView(viewObject);
                p.BindData(model);
                listPresenter.Add(p);
            }
            else
            {
                listPresenter[index].SetView(viewObject);
                listPresenter[index].BindData(model);
            }
        }
    }
}