namespace GameFoundation.Scripts.UIModule.Adapter
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Com.TheFallenGames.OSA.Core;
    using Com.TheFallenGames.OSA.DataHelpers;
    using Cysharp.Threading.Tasks;
    using GameFoundation.Scripts.UIModule.MVP;
    using GameFoundation.Scripts.UIModule.Utilities;
    using UnityEngine;
    using Zenject;

    // There are 2 important callbacks you need to implement, apart from Start(): CreateViewsHolder() and UpdateViewsHolder()
    // See explanations below
    public class MultiplePrefabsListAdapter<TModel, TView, TPresenter> : OSA<MultiplePrefabsParams, BaseItemViewsHolder>
        where TModel : MultiplePrefabsModel
        where TView : MonoBehaviour, IUIView
        where TPresenter : BaseUIItemPresenter<TView, TModel>, IDisposable
    {
        // Helper that stores data and notifies the adapter when items count changes
        // Can be iterated and can also have its elements accessed by the [] operator
        private CanvasGroup               canvasGroup;
        private SimpleDataHelper<TModel>  models;
        private List<BaseItemViewsHolder> viewsHolders;
        private List<TPresenter>          presenters;
        private HashSet<TView>            calledOnViewReadySet = new();

        private DiContainer diContainer;

        #region OSA implementation

        protected override void Start()
        {
            this.models = new SimpleDataHelper<TModel>(this);

            // Calling this initializes internal data and prepares the adapter to handle item count changes
            base.Start();

            // Retrieve the models from your data source and set the items count
            /*
            RetrieveDataAndUpdate(500);
            */
        }

        // This is called initially, as many times as needed to fill the viewport,
        // and anytime the viewport's size grows, thus allowing more items to be displayed
        // Here you create the "ViewsHolder" instance whose views will be re-used
        // *For the method's full description check the base implementation
        protected override BaseItemViewsHolder CreateViewsHolder(int itemIndex)
        {
            var vh = new BaseItemViewsHolder();
            vh.Init(this.Parameters.ItemPrefabs[this.models[itemIndex].PrefabIndex], this.Parameters.Content, itemIndex);
            return vh;
        }

        // This is called anytime a previously invisible item become visible, or after it's created,
        // or when anything that requires a refresh happens
        // Here you bind the data from the model to the item's views
        // *For the method's full description check the base implementation
        protected override void UpdateViewsHolder(BaseItemViewsHolder viewHolder)
        {
            var index = viewHolder.ItemIndex;
            if (this.models.Count <= index || index < 0) return;

            for (var i = this.presenters.Count; i <= index; ++i)
            {
                this.presenters.Add(this.diContainer.Instantiate(this.models[i].PresenterType) as TPresenter);
            }

            var model     = this.models[index];
            var tView      = viewHolder.root.GetComponentInChildren<TView>(true);
            var presenter = this.presenters[index];

            presenter.SetView(tView);
            if (!this.calledOnViewReadySet.Contains(tView))
            {
                presenter.OnViewReady();
                this.calledOnViewReadySet.Add(tView);
            }
            presenter.Dispose();
            presenter.BindData(model);

            if (this.Parameters.PrefabControlsDefaultItemSize && !this.viewsHolders.Contains(viewHolder))
            {
                this.RequestChangeItemSizeAndUpdateLayout(index, this.Parameters.ItemSizes[model.PrefabIndex]);
                this.viewsHolders.Add(viewHolder);
            }
        }

        protected override bool IsRecyclable(BaseItemViewsHolder vh, int itemIndex, double _)
        {
            return this.models[vh.ItemIndex].PresenterType == this.models[itemIndex].PresenterType;
            return vh.ItemIndex == itemIndex;
        }

        #endregion

        // These are common data manipulation methods
        // The list containing the models is managed by you. The adapter only manages the items' sizes and the count
        // The adapter needs to be notified of any change that occurs in the data list. Methods for each
        // case are provided: Refresh, ResetItems, InsertItems, RemoveItems

        public async UniTask InitItemAdapter(List<TModel> models, DiContainer diContainer)
        {
            this.diContainer  = diContainer;
            this.models       = new SimpleDataHelper<TModel>(this);
            this.viewsHolders = new List<BaseItemViewsHolder>();
            this.presenters   = new List<TPresenter>();

            await UniTask.WaitUntil(() => this.IsInitialized);
            this.ResetItems(0);
            this.models.InsertItems(0, models);
        }
    }

    [Serializable]
    public class MultiplePrefabsParams : BaseParams
    {
        public List<RectTransform> ItemPrefabs;
        public bool                PrefabControlsDefaultItemSize = true;
        public List<float>         ItemSizes { get; set; }

        public override void InitIfNeeded(IOSA iAdapter)
        {
            base.InitIfNeeded(iAdapter);
            this.ItemSizes = new List<float>();
            foreach (var itemPrefab in this.ItemPrefabs)
            {
                this.AssertValidWidthHeight(itemPrefab);
                this.ItemSizes.Add(itemPrefab.rect.height);
            }
        }
    }

    public abstract class MultiplePrefabsModel
    {
        public int  PrefabIndex   { get; set; }
        public Type PresenterType { get; set; }
    }
}