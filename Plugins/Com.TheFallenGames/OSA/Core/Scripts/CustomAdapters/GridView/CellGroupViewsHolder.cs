using UnityEngine;
using UnityEngine.UI;
using Com.ForbiddenByte.OSA.Core;

namespace Com.ForbiddenByte.OSA.CustomAdapters.GridView
{
    /// <summary>
    /// <para>A views holder representing a group of cells (row or column). It instantiates the maximum number of cells it can contain,</para>
    /// <para>but only those of them that should be displayed will have their <see cref="CellViewsHolder.views"/> enabled</para>
    /// </summary>
    /// <typeparam name="TCellVH">The views holder type used for the cells in this group</typeparam>
    public class CellGroupViewsHolder<TCellVH> : BaseItemViewsHolder where TCellVH : CellViewsHolder, new()
    {
        /// <summary>Uses base's implementation, but also updates the indices of all containing cells each time the setter is called</summary>
        public override int ItemIndex
        {
            get => base.ItemIndex;
            set
            {
                base.ItemIndex = value;
                if (this._Capacity > 0) this.OnGroupIndexChanged();
            }
        }

        /// <summary>The number of visible cells, i.e. that are used to display real data. The other ones are disabled and are either empty or hold obsolete data</summary>
        public int NumActiveCells
        {
            get => this._NumActiveCells;
            set
            {
                if (this._NumActiveCells != value)
                {
                    this._NumActiveCells = value;
                    TCellVH    cellVH;
                    GameObject viewsGO;
                    bool       active;
                    // TODO (low-priority) also integrate the new SetViewsHolderEnabled functionality here, for grids
                    for (var i = 0; i < this._Capacity; ++i)
                    {
                        cellVH  = this.ContainingCellViewsHolders[i];
                        viewsGO = cellVH.views.gameObject;
                        active  = i < this._NumActiveCells;
                        if (viewsGO.activeSelf != active) viewsGO.SetActive(active);
                        if (cellVH.rootLayoutElement.ignoreLayout == active) cellVH.rootLayoutElement.ignoreLayout = !active;
                    }
                }
            }
        }

        /// <summary>The views holders of all containing cells, active or not</summary>
        public TCellVH[] ContainingCellViewsHolders { get; private set; }

        //protected HorizontalOrVerticalLayoutGroup _LayoutGroup;
        protected int _Capacity       = -1;
        protected int _NumActiveCells = 0;

        private RectTransform[] _ContainingCellInstances;

        /// <summary>
        /// <para>Called by <see cref="Init(GameObject, int, RectTransform, int)"/>, after the GameObjects for the group and all containing cells are instantiated</para>
        /// <para>Creates the cells' views holders and initializes them, also setting their itemIndex based on this group's <see cref="ItemIndex"/></para>
        /// </summary>
        public override void CollectViews()
        {
            base.CollectViews();

            //if (capacity == -1) // not initialized
            //    throw new InvalidOperationException("ItemAsLayoutGroupViewsHolder.CollectViews(): call InitGroup(...) before!");

            //_LayoutGroup = root.GetComponent<HorizontalOrVerticalLayoutGroup>();

            this.ContainingCellViewsHolders = new TCellVH[this._Capacity];
            for (var i = 0; i < this._Capacity; ++i)
            {
                this.ContainingCellViewsHolders[i] = new();
                //ContainingCellViewsHolders[i].InitWithExistingRootPrefab(root.GetChild(i) as RectTransform);
                var cellRT = this._ContainingCellInstances[i];
                this.ContainingCellViewsHolders[i].InitWithExistingRoot(cellRT, this.root, -1);
                // TODO also integrate the new SetViewsHolderEnabled functionality here, for grids
                this.ContainingCellViewsHolders[i].views.gameObject.SetActive(false); // not visible, initially
            }

            if (this.ItemIndex != -1 && this._Capacity > 0) this.UpdateIndicesOfContainingCells();
        }

        /// <summary>The only way to instantiate the group views holder</summary>
        /// <param name="itemIndex">the group's index</param>
        public void Init(GameObject groupPrefab, RectTransform parent, int itemIndex, RectTransform cellPrefab, int numCellsPerGroup)
        {
            base.Init(
                groupPrefab,
                parent,
                itemIndex,
                true,
                false // not calling CollectViews, because we'll call it below
            );

            this._Capacity = numCellsPerGroup;

            // Instantiate all the cells in the group
            this._ContainingCellInstances = new RectTransform[this._Capacity];
            for (var i = 0; i < this._Capacity; ++i)
            {
                var cellInstance = (Object.Instantiate(cellPrefab.gameObject, this.root, false) as GameObject).transform as RectTransform;
                // TODO also integrate the new SetViewsHolderEnabled functionality here, for grids
                cellInstance.gameObject.SetActive(true); // just in case the prefab was disabled
                this._ContainingCellInstances[i] = cellInstance;
            }
            this.CollectViews();

            // Important. Fixes a bug where ScrollTo would scroll to the wrong group
            LayoutRebuilder.ForceRebuildLayoutImmediate(this.root);
        }

        /// <inheritdoc/>
        public override void OnBeforeDestroy()
        {
            // Calling OnBeforeDestroy for all the child cells
            if (this.ContainingCellViewsHolders != null)
                for (var i = 0; i < this.ContainingCellViewsHolders.Length; i++)
                {
                    var c = this.ContainingCellViewsHolders[i];
                    if (c != null) c.OnBeforeDestroy();
                }

            base.OnBeforeDestroy();
        }

        /// <summary>This happens when the views holder is recycled or first created</summary>
        protected virtual void OnGroupIndexChanged()
        {
            if (this._Capacity > 0) this.UpdateIndicesOfContainingCells();
        }

        protected virtual void UpdateIndicesOfContainingCells()
        {
            for (var i = 0; i < this._Capacity; ++i) this.ContainingCellViewsHolders[i].ItemIndex = this.ItemIndex * this._Capacity + i;
        }
    }
}