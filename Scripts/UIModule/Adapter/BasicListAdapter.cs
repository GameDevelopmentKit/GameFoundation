namespace GameFoundation.Scripts.UIModule.Adapter
{
    using System;
    using System.Collections.Generic;
    using Com.TheFallenGames.OSA.Core;
    using Com.TheFallenGames.OSA.CustomParams;
    using Com.TheFallenGames.OSA.DataHelpers;
    using Cysharp.Threading.Tasks;
    using GameFoundation.Scripts.UIModule.MVP;
    using UnityEngine;
    using Zenject;

    // There are 2 important callbacks you need to implement, apart from Start(): CreateViewsHolder() and UpdateViewsHolder()
    // See explanations below
    public class BasicListAdapter<TModel, TView, TPresenter> : OSA<BaseParamsWithPrefab, MyListItemViewsHolder>
        where TPresenter : BaseUIItemPresenter<TView, TModel>, IDisposable where TView : MonoBehaviour, IUIView
    {
        // Helper that stores data and notifies the adapter when items count changes
        // Can be iterated and can also have its elements accessed by the [] operator
        private SimpleDataHelper<TModel> Models { get; set; }
        private CanvasGroup              canvasGroup;
        private List<TPresenter>         presenters;

        private DiContainer diContainer;

        #region OSA implementation

        protected override void Start()
        {
            this.Models = new SimpleDataHelper<TModel>(this);

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
            var model      = this.Models[index];
            var viewObject = v.root.GetComponentInChildren<TView>(true);

            if (this.presenters.Count <= index)
            {
                var p = this.diContainer.Instantiate<TPresenter>();
                p.SetView(viewObject);
                p.BindData(model);
                this.presenters.Add(p);
            }
            else
            {
                this.presenters[index].SetView(viewObject);
                this.presenters[index].Dispose();
                this.presenters[index].BindData(model);
            }
        }

        #endregion

        // These are common data manipulation methods
        // The list containing the models is managed by you. The adapter only manages the items' sizes and the count
        // The adapter needs to be notified of any change that occurs in the data list. Methods for each
        // case are provided: Refresh, ResetItems, InsertItems, RemoveItems

        public async UniTask InitItemAdapter(List<TModel> modelList, DiContainer diContainer)
        {
            this.diContainer = diContainer;
            this.Models      = new SimpleDataHelper<TModel>(this);
            
            if (this.presenters != null)
            {
                foreach (var baseUIItemPresenter in this.presenters)
                {
                    baseUIItemPresenter.Dispose();
                } 
            }
            this.presenters  = new List<TPresenter>();

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
            if (twinPassScheduledBefore)
                throw new OSAException("You shouldn't call ForceUpdateVisibleItems during a ComputeVisibilityForCurrentPosition, UpdateViewsHolder or CreateViewsHolder");

            for (var i = 0; i < this.presenters.Count; i++)
            {
                this.ForceUpdateViewsHolderIfVisible(i);
            }
        }
        
        public TPresenter GetPresenterAtIndex(int index) => this.presenters[index];
    }

// This class keeps references to an item's views.
// Your views holder should extend BaseItemViewsHolder for ListViews and CellViewsHolder for GridViews
    public class MyListItemViewsHolder : BaseItemViewsHolder
    {
    }
}