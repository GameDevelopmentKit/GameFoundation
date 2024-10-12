namespace GameFoundation.Scripts.UIModule.Adapter
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Com.ForbiddenByte.OSA.Core;
    using Com.ForbiddenByte.OSA.CustomParams;
    using Com.ForbiddenByte.OSA.DataHelpers;
    using Cysharp.Threading.Tasks;
    using GameFoundation.DI;
    using GameFoundation.Scripts.UIModule.MVP;
    using UnityEngine;

    // There are 2 important callbacks you need to implement, apart from Start(): CreateViewsHolder() and UpdateViewsHolder()
    // See explanations below
    public class BasicListAdapter<TModel, TView, TPresenter> : OSA<BaseParamsWithPrefab, MyListItemViewsHolder>
        where TPresenter : BaseUIItemPresenter<TView, TModel>, IDisposable where TView : MonoBehaviour, IUIView
    {
        // Helper that stores data and notifies the adapter when items count changes
        // Can be iterated and can also have its elements accessed by the [] operator
        public  SimpleDataHelper<TModel> Models { get; private set; }
        private IDependencyContainer     container;

        private readonly Dictionary<TView, TPresenter> viewToPresenter  = new();
        private readonly Dictionary<int, TPresenter>   indexToPresenter = new();

        #region OSA implementation

        protected override void Awake()
        {
            base.Awake();
            this.container = this.GetCurrentContainer();
            this.Models    = new(this);
        }

        // This is called initially, as many times as needed to fill the viewport,
        // and anytime the viewport's size grows, thus allowing more items to be displayed
        // Here you create the "ViewsHolder" instance whose views will be re-used
        // *For the method's full description check the base implementation
        protected override MyListItemViewsHolder CreateViewsHolder(int itemIndex)
        {
            var instance = new MyListItemViewsHolder();

            instance.Init(this._Params.ItemPrefab, this._Params.Content, itemIndex);

            return instance;
        }

        // This is called anytime a previously invisible item become visible, or after it's created,
        // or when anything that requires a refresh happens
        // Here you bind the data from the model to the item's views
        // *For the method's full description check the base implementation
        protected override void UpdateViewsHolder(MyListItemViewsHolder v)
        {
            var index = v.ItemIndex;

            if (this.Models.Count <= index || index < 0) return;
            var model = this.Models[index];
            var view  = v.root.GetComponentInChildren<TView>(true);

            if (this.viewToPresenter.TryGetValue(view, out var presenter))
            {
                presenter.Dispose();
            }
            else
            {
                presenter = this.viewToPresenter[view] = this.container.Instantiate<TPresenter>();
                presenter.SetView(view);
                presenter.OnViewReady();
            }

            this.indexToPresenter[index] = presenter;

            presenter.BindData(model);
        }

        #endregion

        // These are common data manipulation methods
        // The list containing the models is managed by you. The adapter only manages the items' sizes and the count
        // The adapter needs to be notified of any change that occurs in the data list. Methods for each
        // case are provided: Refresh, ResetItems, InsertItems, RemoveItems

        public async UniTask InitItemAdapter(List<TModel> modelList)
        {
            await UniTask.WaitUntil(() => this.IsInitialized);
            this.ResetItems(0);
            this.Models.ResetItems(modelList);
        }

        /// <summary>
        /// We need this because the original method only update to  this.VisibleItemsCount - 1
        /// </summary>
        /// <exception cref="OSAException"></exception>
        public void ForceUpdateFullVisibleItems()
        {
            var twinPassScheduledBefore = this._InternalState.computeVisibilityTwinPassScheduled;
            if (twinPassScheduledBefore) throw new OSAException("You shouldn't call ForceUpdateVisibleItems during a ComputeVisibilityForCurrentPosition, UpdateViewsHolder or CreateViewsHolder");

            for (var i = 0; i < this.viewToPresenter.Count; i++) this.ForceUpdateViewsHolderIfVisible(i);
        }

        public TPresenter GetPresenterAtIndex(int index)
        {
            return this.indexToPresenter[index];
        }

        public List<TPresenter> GetPresenters()
        {
            return this.indexToPresenter.OrderBy(kv => kv.Key).Select(kv => kv.Value).ToList();
        }
    }

    // This class keeps references to an item's views.
    // Your views holder should extend BaseItemViewsHolder for ListViews and CellViewsHolder for GridViews
    public class MyListItemViewsHolder : BaseItemViewsHolder
    {
    }
}