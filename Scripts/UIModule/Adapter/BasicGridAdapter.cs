namespace GameFoundation.Scripts.UIModule.Adapter
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using Com.TheFallenGames.OSA.CustomAdapters.GridView;
    using Com.TheFallenGames.OSA.DataHelpers;
    using Cysharp.Threading.Tasks;
    using GameFoundation.Scripts.UIModule.MVP;
    using UnityEngine;
    using Zenject;

    // There is 1 important callback you need to implement, apart from Start(): UpdateCellViewsHolder()
    // See explanations below
    public class BasicGridAdapter<TModel, TView, TPresenter> : GridAdapter<GridParams, MyGridItemViewsHolder>
        where TModel : new() where TPresenter : BaseUIItemPresenter<TView, TModel> where TView : MonoBehaviour, IUIView
    {
        // Helper that stores data and notifies the adapter when items count changes
        // Can be iterated and can also have its elements accessed by the [] operator
        public  SimpleDataHelper<TModel> Models { get; private set; }
        private CanvasGroup              canvasGroup;
        private List<TPresenter>         presenters;
        
        private DiContainer diContainer;
        
        [Inject]
        public void Constructor(DiContainer diContainer)
        {
            this.diContainer = diContainer;
        }

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

        public void SetViewAlpha(float alpha)
        {
            this.canvasGroup = this.GetComponent<CanvasGroup>();
            if (this.canvasGroup == null)
            {
                this.canvasGroup = this.gameObject.AddComponent<CanvasGroup>();
            }

            this.canvasGroup.alpha = alpha;
        }
        
        // This is called anytime a previously invisible item become visible, or after it's created, 
        // or when anything that requires a refresh happens
        // Here you bind the data from the model to the item's views
        // *For the method's full description check the base implementation
        protected override void UpdateCellViewsHolder(MyGridItemViewsHolder v)
        {
            var index      = v.ItemIndex;
            var model      = this.Models[index];
            var viewObject = v.root.GetComponentInChildren<TView>(true);
            if (this.presenters.Count >= index)
            {
                var p = this.diContainer.Instantiate<TPresenter>();
                p.SetView(viewObject);
                p.BindData(model);
                this.presenters.Add(p);
            }
            else
            {
                this.presenters[index].SetView(viewObject);
                this.presenters[index].BindData(model);
            }
        }

        #endregion

        // These are common data manipulation methods
        // The list containing the models is managed by you. The adapter only manages the items' sizes and the count
        // The adapter needs to be notified of any change that occurs in the data list. 
        // For GridAdapters, only Refresh and ResetItems work for now

        #region data manipulation

        public void AddItemsAt(int index, IList<TModel> items)
        {
            //Commented: this only works with Lists. ATM, Insert for Grids only works by manually changing the list and calling NotifyListChangedExternally() after
            //Data.InsertItems(index, items);
            this.Models.List.InsertRange(index, items);
            this.Models.NotifyListChangedExternally();
        }

        public void RemoveItemsFrom(int index, int count)
        {
            //Commented: this only works with Lists. ATM, Remove for Grids only works by manually changing the list and calling NotifyListChangedExternally() after
            //Data.RemoveRange(index, count);
            this.Models.List.RemoveRange(index, count);
            this.Models.NotifyListChangedExternally();
        }

        public void SetItems(IList<TModel> items) { this.Models.ResetItems(items); }

        #endregion


        // Here, we're requesting <count> items from the data source
        void RetrieveDataAndUpdate(int count) { this.StartCoroutine(this.FetchMoreItemsFromDataSourceAndUpdate(count)); }

        // Retrieving <count> models from the data source and calling OnDataRetrieved after.
        // In a real case scenario, you'd query your server, your database or whatever is your data source and call OnDataRetrieved after
        IEnumerator FetchMoreItemsFromDataSourceAndUpdate(int count)
        {
            // Simulating data retrieving delay
            yield return new WaitForSeconds(0f);

            var newItems = new TModel[count];


            this.OnDataRetrieved(newItems);
        }

        void OnDataRetrieved(TModel[] newItems)
        {
            //Commented: this only works with Lists. ATM, Insert for Grids only works by manually changing the list and calling NotifyListChangedExternally() after
            // Data.InsertItemsAtEnd(newItems);

            this.Models.List.AddRange(newItems);
            this.Models.NotifyListChangedExternally();
        }
        
        public async void InitItemAdapter(List<TModel> modelList)
        {
            this.Models     = new SimpleDataHelper<TModel>(this);
            this.presenters = new List<TPresenter>();

            await UniTask.WaitUntil(() => this.IsInitialized);
            this.Models.InsertItems(0, modelList);
        }
    }

    // This class keeps references to an item's views.
    // Your views holder should extend BaseItemViewsHolder for ListViews and CellViewsHolder for GridViews
    // The cell views holder should have a single child (usually named "Views"), which contains the actual 
    // UI elements. A cell's root is never disabled - when a cell is removed, only its "views" GameObject will be disabled
    public class MyGridItemViewsHolder : CellViewsHolder
    {
    }
}