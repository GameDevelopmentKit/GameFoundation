namespace GameFoundation.Scripts.UIModule.Adapter
{
    using System.Collections.Generic;
    using Com.TheFallenGames.OSA.Core;
    using Com.TheFallenGames.OSA.CustomAdapters.GridView;
    using Com.TheFallenGames.OSA.DataHelpers;
    using Cysharp.Threading.Tasks;
    using GameFoundation.Scripts.UIModule.MVP;
    using UnityEngine;
    using Zenject;

    // There is 1 important callback you need to implement, apart from Start(): UpdateCellViewsHolder()
    // See explanations below
    public class BasicGridAdapter<TModel, TView, TPresenter> : GridAdapter<GridParams, MyGridItemViewsHolder> where TPresenter : BaseUIItemPresenter<TView, TModel> where TView : MonoBehaviour, IUIView
    {
        // Helper that stores data and notifies the adapter when items count changes
        // Can be iterated and can also have its elements accessed by the [] operator
        public  SimpleDataHelper<TModel> Models { get; private set; }
        private CanvasGroup              canvasGroup;
        private List<TPresenter>         presenters;

        private DiContainer diContainer;

        #region GridAdapter implementation

        protected override void Start()
        {
            this.Models = new SimpleDataHelper<TModel>(this);

            // Calling this initializes internal data and prepares the adapter to handle item count changes
            base.Start();

            // Retrieve the models from your data source and set the items count
            /*
            RetrieveDataAndUpdate(1500);
            */
        }

        // This is called anytime a previously invisible item become visible, or after it's created, 
        // or when anything that requires a refresh happens
        // Here you bind the data from the model to the item's views
        // *For the method's full description check the base implementation
        protected override void UpdateCellViewsHolder(MyGridItemViewsHolder v)
        {
            var index      = v.ItemIndex;
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
        // The adapter needs to be notified of any change that occurs in the data list. 
        // For GridAdapters, only Refresh and ResetItems work for now

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
        
        public List<TPresenter> GetPresenters() => this.presenters;
    }

    // This class keeps references to an item's views.
    // Your views holder should extend BaseItemViewsHolder for ListViews and CellViewsHolder for GridViews
    // The cell views holder should have a single child (usually named "Views"), which contains the actual 
    // UI elements. A cell's root is never disabled - when a cell is removed, only its "views" GameObject will be disabled
    public class MyGridItemViewsHolder : CellViewsHolder
    {
    }
}