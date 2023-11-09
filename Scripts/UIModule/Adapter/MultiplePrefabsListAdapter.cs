namespace GameFoundation.Scripts.UIModule.Adapter
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Com.TheFallenGames.OSA.Core;
    using Com.TheFallenGames.OSA.DataHelpers;
    using Cysharp.Threading.Tasks;
    using GameFoundation.Scripts.UIModule.MVP;
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
        private CanvasGroup              canvasGroup;
        private SimpleDataHelper<TModel> models;
        private List<TPresenter>         presenters;

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
            vh.Init(this.Parameters.ItemPrefabs[this.models[itemIndex].PrefabName], this.Parameters.Content, itemIndex);
            return vh;
        }

        // This is called anytime a previously invisible item become visible, or after it's created,
        // or when anything that requires a refresh happens
        // Here you bind the data from the model to the item's views
        // *For the method's full description check the base implementation
        protected override void UpdateViewsHolder(BaseItemViewsHolder vh)
        {
            var index = vh.ItemIndex;

            if (this.models.Count <= index || index < 0) return;

            var model = this.models[index];
            var view  = vh.root.GetComponentInChildren<TView>(true);

            if (this.presenters.Count <= index)
            {
                var presenter = this.diContainer.Instantiate(this.models[index].PresenterType) as TPresenter;
                presenter.SetView(view);
                presenter.BindData(model);
                this.presenters.Add(presenter);
            }
            else
            {
                this.presenters[index].SetView(view);
                this.presenters[index].Dispose();
                this.presenters[index].BindData(model);
            }
        }

        protected override bool IsRecyclable(BaseItemViewsHolder vh, int itemIndex, double _)
        {
            return this.models[vh.ItemIndex].PresenterType == this.models[itemIndex].PresenterType;
        }

        #endregion

        // These are common data manipulation methods
        // The list containing the models is managed by you. The adapter only manages the items' sizes and the count
        // The adapter needs to be notified of any change that occurs in the data list. Methods for each
        // case are provided: Refresh, ResetItems, InsertItems, RemoveItems

        public async UniTask InitItemAdapter(List<TModel> models, DiContainer diContainer)
        {
            this.diContainer = diContainer;
            this.models      = new SimpleDataHelper<TModel>(this);
            
            if (this.presenters != null)
            {
                foreach (var baseUIItemPresenter in this.presenters)
                {
                    baseUIItemPresenter.Dispose();
                } 
            }
            this.presenters  = models.Select(model => this.diContainer.Instantiate(model.PresenterType) as TPresenter).ToList();

            await UniTask.WaitUntil(() => this.IsInitialized);
            this.ResetItems(0);
            this.models.ResetItems(models);
            for (var i = 0; i < models.Count; ++i)
            {
                this.RequestChangeItemSizeAndUpdateLayout(i, this.Parameters.ItemSizes[models[i].PrefabName]);
            }
        }
    }

    [Serializable]
    public class MultiplePrefabsParams : BaseParams
    {
        [SerializeField] private List<RectTransform> itemPrefabs;
        [SerializeField] private bool                prefabControlsDefaultItemSize = true;

        public readonly Dictionary<string, RectTransform> ItemPrefabs = new();
        public readonly Dictionary<string, float>         ItemSizes   = new();

        public override void InitIfNeeded(IOSA iAdapter)
        {
            base.InitIfNeeded(iAdapter);
            foreach (var itemPrefab in this.itemPrefabs)
            {
                this.AssertValidWidthHeight(itemPrefab);
                this.ItemPrefabs[itemPrefab.name] = itemPrefab;
                this.ItemSizes[itemPrefab.name]   = this.prefabControlsDefaultItemSize ? itemPrefab.rect.height : this.DefaultItemSize;
            }
        }
    }

    public abstract class MultiplePrefabsModel
    {
        public abstract string PrefabName    { get; }
        public abstract Type   PresenterType { get; }
    }
}