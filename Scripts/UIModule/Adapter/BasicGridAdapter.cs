namespace GameFoundation.Scripts.UIModule.Adapter
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using Com.TheFallenGames.OSA.CustomAdapters.GridView;
    using Com.TheFallenGames.OSA.DataHelpers;
    using UnityEngine;

    // There is 1 important callback you need to implement, apart from Start(): UpdateCellViewsHolder()
    // See explanations below
    public class BasicGridAdapter : GridAdapter<GridParams, MyGridItemViewsHolder>
    {
        // Helper that stores data and notifies the adapter when items count changes
        // Can be iterated and can also have its elements accessed by the [] operator
        public  SimpleDataHelper<MyGridItemModel> Data { get; private set; }
        public  Action<MyGridItemViewsHolder>     UpdateCellViewHolder;
        private CanvasGroup                       canvasGroup;

        #region GridAdapter implementation

        protected override void Start()
        {
            this.Data = new SimpleDataHelper<MyGridItemModel>(this);

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
        protected override void UpdateCellViewsHolder(MyGridItemViewsHolder newOrRecycled)
        {
            this.UpdateCellViewHolder?.Invoke(newOrRecycled);
        }
      
        #endregion

        // These are common data manipulation methods
        // The list containing the models is managed by you. The adapter only manages the items' sizes and the count
        // The adapter needs to be notified of any change that occurs in the data list. 
        // For GridAdapters, only Refresh and ResetItems work for now

        #region data manipulation

        public void AddItemsAt(int index, IList<MyGridItemModel> items)
        {
            //Commented: this only works with Lists. ATM, Insert for Grids only works by manually changing the list and calling NotifyListChangedExternally() after
            //Data.InsertItems(index, items);
            this.Data.List.InsertRange(index, items);
            this.Data.NotifyListChangedExternally();
        }

        public void RemoveItemsFrom(int index, int count)
        {
            //Commented: this only works with Lists. ATM, Remove for Grids only works by manually changing the list and calling NotifyListChangedExternally() after
            //Data.RemoveRange(index, count);
            this.Data.List.RemoveRange(index, count);
            this.Data.NotifyListChangedExternally();
        }

        public void SetItems(IList<MyGridItemModel> items) { this.Data.ResetItems(items); }

        #endregion


        // Here, we're requesting <count> items from the data source
        void RetrieveDataAndUpdate(int count) { this.StartCoroutine(this.FetchMoreItemsFromDataSourceAndUpdate(count)); }

        // Retrieving <count> models from the data source and calling OnDataRetrieved after.
        // In a real case scenario, you'd query your server, your database or whatever is your data source and call OnDataRetrieved after
        IEnumerator FetchMoreItemsFromDataSourceAndUpdate(int count)
        {
            // Simulating data retrieving delay
            yield return new WaitForSeconds(0f);

            var newItems = new MyGridItemModel[count];

          

            this.OnDataRetrieved(newItems);
        }

        void OnDataRetrieved(MyGridItemModel[] newItems)
        {
            //Commented: this only works with Lists. ATM, Insert for Grids only works by manually changing the list and calling NotifyListChangedExternally() after
            // Data.InsertItemsAtEnd(newItems);

            this.Data.List.AddRange(newItems);
            this.Data.NotifyListChangedExternally();
        }
    }


    // Class containing the data associated with an item
    public class MyGridItemModel
    {
        /*
        public string title;
        public Color color;
        */
    }


    // This class keeps references to an item's views.
    // Your views holder should extend BaseItemViewsHolder for ListViews and CellViewsHolder for GridViews
    // The cell views holder should have a single child (usually named "Views"), which contains the actual 
    // UI elements. A cell's root is never disabled - when a cell is removed, only its "views" GameObject will be disabled
    public class MyGridItemViewsHolder : CellViewsHolder
    {
      
    }
}