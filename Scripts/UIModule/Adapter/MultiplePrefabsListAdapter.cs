namespace UIModule.Adapter
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Com.ForbiddenByte.OSA.Core;
    using Com.ForbiddenByte.OSA.DataHelpers;
    using Cysharp.Threading.Tasks;
    using GameFoundation.Scripts.AssetLibrary;
    using GameFoundation.Scripts.UIModule.MVP;
    using UnityEngine;
    using Zenject;

    // There are 2 important callbacks you need to implement, apart from Start(): CreateViewsHolder() and UpdateViewsHolder()
    // See explanations below
    public class MultiplePrefabsListAdapter<TModel, TView> : OSA<MultiplePrefabsParams, BaseItemViewsHolder>
        where TModel : MultiplePrefabsModel
        where TView : MonoBehaviour, IUIView
    {
        // Helper that stores data and notifies the adapter when items count changes
        // Can be iterated and can also have its elements accessed by the [] operator
        public           SimpleDataHelper<TModel>                              Models { get; private set; }
        private          DiContainer                                           container;
        private readonly Dictionary<TView, BaseUIItemPresenter<TView, TModel>> viewToPresenter  = new();
        private readonly Dictionary<int, BaseUIItemPresenter<TView, TModel>>   indexToPresenter = new();
        private          int                                                   initItemAdapterVersion;

        #region OSA implementation
        protected override void Awake()
        {
            base.Awake();
            this.Models = new SimpleDataHelper<TModel>(this);
        }

        // This is called initially, as many times as needed to fill the viewport,
        // and anytime the viewport's size grows, thus allowing more items to be displayed
        // Here you create the "ViewsHolder" instance whose views will be re-used
        // *For the method's full description check the base implementation
        protected override BaseItemViewsHolder CreateViewsHolder(int itemIndex)
        {
            var prefabName = this.Models[itemIndex].PrefabName;
            if (!this.Parameters.ItemPrefabs.TryGetValue(prefabName, out var itemPrefab))
            {
                throw new KeyNotFoundException(
                    $"Item prefab '{prefabName}' is not registered for {this.GetType().Name}. " +
                    $"Registered prefabs: {string.Join(", ", this.Parameters.ItemPrefabs.Keys)}");
            }

            var vh = new BaseItemViewsHolder();
            vh.Init(itemPrefab, this.Parameters.Content, itemIndex);

            return vh;
        }

        // This is called anytime a previously invisible item become visible, or after it's created,
        // or when anything that requires a refresh happens
        // Here you bind the data from the model to the item's views
        // *For the method's full description check the base implementation
        protected override void UpdateViewsHolder(BaseItemViewsHolder vh)
        {
            var index = vh.ItemIndex;

            if (this.Models.Count <= index || index < 0) return;
            var model = this.Models[index];
            var view  = vh.root.GetComponentInChildren<TView>(true);

            if (this.viewToPresenter.TryGetValue(view, out var presenter))
            {
                presenter.Dispose();
            }
            else
            {
                presenter = this.viewToPresenter[view] = this.container.Instantiate(this.Models[index].PresenterType) as BaseUIItemPresenter<TView, TModel>;
                presenter.SetView(view);
            }

            this.indexToPresenter[index] = presenter;

            presenter.BindData(model);
        }

        protected override bool IsRecyclable(BaseItemViewsHolder vh, int itemIndex, double _) { return this.Models[vh.ItemIndex].PresenterType == this.Models[itemIndex].PresenterType; }
        #endregion

        // These are common data manipulation methods
        // The list containing the models is managed by you. The adapter only manages the items' sizes and the count
        // The adapter needs to be notified of any change that occurs in the data list. Methods for each
        // case are provided: Refresh, ResetItems, InsertItems, RemoveItems

        public virtual async UniTask InitItemAdapter(List<TModel> models, DiContainer diContainer)
        {
            await this.TryInitItemAdapter(models, diContainer);
        }

        protected async UniTask<bool> TryInitItemAdapter(List<TModel> models, DiContainer diContainer)
        {
            // This is used to prevent race conditions, only the last call to InitItemAdapter will be effective after initialization
            var initVersion = ++this.initItemAdapterVersion;
            var modelList   = models != null ? new List<TModel>(models) : new List<TModel>();

            if (!this.IsInitialized)
            {
                await UniTask.WaitUntil(() => this.IsInitialized);
            }

            if (initVersion != this.initItemAdapterVersion)
            {
                return false;
            }

            var gameAssets     = diContainer.Resolve<IGameAssets>();
            var loadedPrefabs  = new Dictionary<string, RectTransform>();

            // Try load all prefabs that are not already in the dictionary
            foreach (var model in modelList)
            {
                if (this.Parameters.ItemPrefabs.ContainsKey(model.PrefabName) || loadedPrefabs.ContainsKey(model.PrefabName))
                {
                    continue;
                }

                var itemPrefab = await gameAssets.LoadAssetAsync<GameObject>(model.PrefabName);
                if (initVersion != this.initItemAdapterVersion)
                {
                    return false;
                }

                loadedPrefabs[model.PrefabName] = itemPrefab.GetComponent<RectTransform>();
            }

            if (initVersion != this.initItemAdapterVersion)
            {
                return false;
            }

            this.container = diContainer;

            foreach (var itemPrefab in loadedPrefabs)
            {
                this.Parameters.ItemPrefabs[itemPrefab.Key] = itemPrefab.Value;
            }
            this.Parameters.UpdateItemSizes();

            this.indexToPresenter.Clear();
            this.Models.ResetItems(modelList);

            if (this.Parameters.PrefabControlsDefaultItemSize)
            {
                for (var i = 0; i < modelList.Count; ++i)
                {
                    this.RequestChangeItemSizeAndUpdateLayout(i, this.Parameters.ItemSizes[modelList[i].PrefabName]);
                }
            }

            return true;
        }

        public BaseUIItemPresenter<TView, TModel> GetPresenterAtIndex(int index) => this.indexToPresenter.GetValueOrDefault(index);

        public List<BaseUIItemPresenter<TView, TModel>> GetPresenters() { return this.indexToPresenter.OrderBy(kv => kv.Key).Select(kv => kv.Value).ToList(); }

        protected override void Dispose()
        {
            base.Dispose();
            
            foreach (var presenter in this.viewToPresenter.Values)
            {
                presenter.Dispose();
            }
            
            this.viewToPresenter.Clear();
            this.indexToPresenter.Clear();
        }
    }

    [Serializable]
    public class MultiplePrefabsParams : BaseParams
    {
        [SerializeField] private List<RectTransform> itemPrefabs;
        [SerializeField] private bool                prefabControlsDefaultItemSize = true;

        public Dictionary<string, RectTransform> ItemPrefabs { get; set; } = new();
        public Dictionary<string, float>         ItemSizes   { get; }      = new();

        public bool PrefabControlsDefaultItemSize => this.prefabControlsDefaultItemSize;

        public override void InitIfNeeded(IOSA iAdapter)
        {
            base.InitIfNeeded(iAdapter);

            if (this.itemPrefabs != null)
            {
                if (this.ItemPrefabs == null)
                {
                    this.ItemPrefabs = new Dictionary<string, RectTransform>();
                }

                foreach (var prefab in this.itemPrefabs.Where(prefab => prefab != null))
                {
                    this.ItemPrefabs[prefab.name] = prefab;
                }

                UpdateItemSizes();
            }
        }

        public void UpdateItemSizes()
        {
            if (!this.prefabControlsDefaultItemSize || ItemPrefabs == null || ItemPrefabs.Count == 0) return;

            foreach (var itemPrefab in ItemPrefabs.Values)
            {
                this.AssertValidWidthHeight(itemPrefab);
                this.ItemSizes[itemPrefab.name] = IsHorizontal ? itemPrefab.rect.width : itemPrefab.rect.height;
                this.DefaultItemSize            = Mathf.Max(this.DefaultItemSize, this.ItemSizes[itemPrefab.name]);
            }
        }
    }

    public abstract class MultiplePrefabsModel
    {
        public abstract string PrefabName    { get; }
        public abstract Type   PresenterType { get; }
    }
}
