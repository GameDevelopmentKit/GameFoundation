using System;
using UnityEngine;
using Com.ForbiddenByte.OSA.Core;
using frame8.Logic.Misc.Other.Extensions;
using Com.ForbiddenByte.OSA.Core.SubComponents;
using System.Collections.Generic;
using UnityEngine.UI;

namespace Com.ForbiddenByte.OSA.CustomAdapters.GridView
{
    /// <summary>
    /// <para>An optimized adapter for a GridView </para>
    /// <para>Implements <see cref="OSA{TParams, TItemViewsHolder}"/> to simulate a grid by using</para>
    /// <para>a runtime-generated "row" prefab (or "colum" prefab, if horizontal ScrollView), having a Horizontal (or Vertical, respectively) LayoutGroup component, inside which its corresponding cells will lie.</para>
    /// <para>This prefab is represented by a <see cref="CellGroupViewsHolder{TCellVH}"/>, which nicely abstractizes the mechanism to using cell prefabs. This views holder is managed internally and is no concern for most users.</para> 
    /// <para>The cell prefab is used the same way as the "item prefab", for those already familiarized with the ListView examples. It is represented</para>
    /// <para>by a <see cref="CellViewsHolder"/>, which are the actual views holders you need to create/update and nothing else. </para>
    /// </summary>
    /// <typeparam name="TParams">Must inherit from GridParams. See also <see cref="OSA{TParams, TItemViewsHolder}.Parameters"/></typeparam>
    /// <typeparam name="TCellVH">The views holder type to use for the cell. Must inherit from CellViewsHolder</typeparam>
    public abstract class GridAdapter<TParams, TCellVH> : OSA<TParams, CellGroupViewsHolder<TCellVH>>
        where TParams : GridParams
        where TCellVH : CellViewsHolder, new()
    {
        private Action<int, int> _CellsRefreshed;

        /// <summary>
        /// This override the base's implementation to return the cells count, instead of the groups(rows) count.
        /// Params are: 1=prevCellCount, 2=newCellCount
        /// </summary>
        public override event Action<int, int> ItemsRefreshed { add => this._CellsRefreshed += value; remove => this._CellsRefreshed -= value; }

        public override bool InsertAtIndexSupported   => false;
        public override bool RemoveFromIndexSupported => false;

        /// <summary>The "items count". Same value is returned in <see cref="GetItemsCount"/></summary>
        public int CellsCount => this._CellsCount;

        protected int _CellsCount;
        private   int _PrevCellsCount; // used for firing the ItemsRefreshed with the proper value. it's assigned in ChangeItemsCount

        // Used because the one in Parameters can change in multiple places
        private int _LastKnownUsedNumCellsPerGroup;

        /// <summary>Not currently implemented for GridAdapters</summary>
        public sealed override void InsertItems(int index, int itemsCount, bool contentPanelEndEdgeStationary = false, bool keepVelocity = false)
        {
            throw new OSAException("Cannot use InsertItems() with a GridAdapter yet. Use ResetItems() instead.");
        }

        /// <summary>Not currently implemented fir GridAdapters</summary>
        public sealed override void RemoveItems(int index, int itemsCount, bool contentPanelEndEdgeStationary = false, bool keepVelocity = false)
        {
            throw new OSAException("Cannot use RemoveItems() with a GridAdapter yet. Use ResetItems() instead.");
        }

        /// <summary> Overridden in order to convert the cellsCount to groupsCount before passing it to the base's implementation</summary>
        /// <seealso cref="OSA{TParams, TItemViewsHolder}.ChangeItemsCount(ItemCountChangeMode, int, int, bool, bool)"/>
        public override void ChangeItemsCount(
            ItemCountChangeMode changeMode,
            int                 cellsCount /*param name changed from itemsCount*/,
            int                 indexIfAppendingOrRemoving    = -1,
            bool                contentPanelEndEdgeStationary = false,
            bool                keepVelocity                  = false
        )
        {
            if (changeMode != ItemCountChangeMode.RESET) throw new OSAException("Only ItemCountChangeMode.RESET is supported with a GridAdapter for now");

            this._PrevCellsCount = this._CellsCount;
            this._CellsCount     = cellsCount;

            // The number of groups is passed to the base's implementation
            var groupsCount = this._Params.GetNumberOfRequiredGroups(this._CellsCount);

            base.ChangeItemsCount(changeMode, groupsCount, indexIfAppendingOrRemoving, contentPanelEndEdgeStationary, keepVelocity);
        }

        /// <summary>
        /// Tha base implementation finds the group. Here, we're narrowing the search in the group iself in order to return the CellViewsHolder
        /// </summary>
        public sealed override AbstractViewsHolder GetViewsHolderClosestToViewportLongitudinalNormalizedAbstractPoint(Canvas c, RectTransform canvasRectTransform, float viewportPoint01, float itemPoint01, out float distance)
        {
            var groupVH = base.GetViewsHolderClosestToViewportLongitudinalNormalizedAbstractPoint(c, canvasRectTransform, viewportPoint01, itemPoint01, out distance) as CellGroupViewsHolder<TCellVH>;

            if (groupVH == null
                || groupVH.NumActiveCells == 0) // 0 active cells is highly unlikely, but it's worth taking it into account
                return null;

            // Returning the cell closest to the middle
            return groupVH.ContainingCellViewsHolders[groupVH.NumActiveCells / 2];
        }

        /// <summary> Scrolls to the specified cell. Use <see cref="ScrollToGroup(int, float, float)"/> if that was intended instead</summary>
        public sealed override void ScrollTo(int cellIndex, float normalizedOffsetFromViewportStart = 0, float normalizedPositionOfItemPivotToUse = 0)
        {
            var originalItemPivot = normalizedPositionOfItemPivotToUse;
            var groupIndex        = this._Params.GetGroupIndex(cellIndex);
            var groupVisible      = this.ScrollTo_ConvertItemPivotToUseIfPossible(groupIndex, cellIndex, ref normalizedPositionOfItemPivotToUse);

            var prevSizeChanges = this._InternalState.totalNumberOfSizeChanges;
            this.ScrollToGroup(groupIndex, normalizedOffsetFromViewportStart, normalizedPositionOfItemPivotToUse);

            // Do subsequent ScrollTos if the group wasn't initially visible or some sizes were changes which might've shifted some positions
            if (!groupVisible || prevSizeChanges != this._InternalState.totalNumberOfSizeChanges)
            {
                prevSizeChanges = this._InternalState.totalNumberOfSizeChanges;
                // Prevent double-conversion
                if (groupVisible) normalizedPositionOfItemPivotToUse = originalItemPivot;

                groupVisible = this.ScrollTo_ConvertItemPivotToUseIfPossible(groupIndex, cellIndex, ref normalizedPositionOfItemPivotToUse);
                this.ScrollToGroup(groupIndex, normalizedOffsetFromViewportStart, normalizedPositionOfItemPivotToUse);
                if (!groupVisible || prevSizeChanges != this._InternalState.totalNumberOfSizeChanges)
                {
                    // Prevent double-conversion
                    if (groupVisible) normalizedPositionOfItemPivotToUse = originalItemPivot;

                    this.ScrollTo_ConvertItemPivotToUseIfPossible(groupIndex, cellIndex, ref normalizedPositionOfItemPivotToUse);
                    this.ScrollToGroup(groupIndex, normalizedOffsetFromViewportStart, normalizedPositionOfItemPivotToUse);
                }
            }
        }

        /// <summary> Scrolls to the specified cell. Use <see cref="SmoothScrollToGroup(int, float, float, float, Func{float, bool}, Action, bool)"/> if that was intended instead</summary>
        public sealed override bool SmoothScrollTo(
            int               cellIndex,
            float             duration,
            float             normalizedOffsetFromViewportStart    = 0f,
            float             normalizedPositionOfItemPivotToUse   = 0f,
            Func<float, bool> onProgress                           = null,
            Action            onDone                               = null,
            bool              overrideAnyCurrentScrollingAnimation = false
        )
        {
            var groupIndex   = this._Params.GetGroupIndex(cellIndex);
            var groupVisible = this.ScrollTo_ConvertItemPivotToUseIfPossible(groupIndex, cellIndex, ref normalizedPositionOfItemPivotToUse);

            var newOnProgress = onProgress;

            var newOnDone = onDone;
            if (!groupVisible)
            {
                duration = duration / 2;

                if (onProgress != null)
                    newOnProgress = p =>
                    {
                        if (onProgress != null) return onProgress(p / 2);

                        return true;
                    };

                if (onDone != null)
                    newOnDone = () =>
                    {
                        if (newOnProgress != null)
                            newOnProgress = p =>
                            {
                                if (onProgress != null) return onProgress(.5f + p / 2);

                                return true;
                            };

                        groupVisible = this.ScrollTo_ConvertItemPivotToUseIfPossible(groupIndex, cellIndex, ref normalizedPositionOfItemPivotToUse);
                        this.SmoothScrollToGroup(groupIndex, duration, normalizedOffsetFromViewportStart, normalizedPositionOfItemPivotToUse, newOnProgress, onDone, overrideAnyCurrentScrollingAnimation);
                    };
            }
            return this.SmoothScrollToGroup(groupIndex, duration, normalizedOffsetFromViewportStart, normalizedPositionOfItemPivotToUse, newOnProgress, newOnDone, overrideAnyCurrentScrollingAnimation);
        }

        public override bool SmoothBringToView(int cellIndex, float duration, float? spacingFromViewportEdge = null, Func<float, bool> onProgress = null, Action onDone = null, bool overrideCurrentScrollingAnimation = false)
        {
            var   groupIndex = this.Parameters.GetGroupIndex(cellIndex);
            float normalizedOffsetFromViewportStart, itemPivotToUse;
            if (!this.BringToView_CalculateParameters(groupIndex, spacingFromViewportEdge, out normalizedOffsetFromViewportStart, out itemPivotToUse)) return false;

            return this.SmoothScrollToGroup(groupIndex, duration, normalizedOffsetFromViewportStart, itemPivotToUse, null, null, overrideCurrentScrollingAnimation);

            // TODO continue this, to also support non-linear cellgroups. Currently, only rows/columns work fine

            //float spacingFromViewportEdgeValid;
            //if (spacingFromViewportEdge == null)
            //	spacingFromViewportEdgeValid = _Params.ContentSpacing + _Params.Grid.SpacingInGroup;
            //else
            //	spacingFromViewportEdgeValid = spacingFromViewportEdge.Value;
            //float vpPivot = spacingFromViewportEdgeValid / _InternalState.layoutInfo.vpSize;

            //int groupIndex = _Params.GetGroupIndex(cellIndex);
            //bool groupVisible = BringToView_ConvertItemInsetFromParentEdgeIfPossible(groupIndex, cellIndex, ref spacingFromViewportEdgeValid);

            //return base.SmoothBringToView(groupIndex, duration, spacingFromViewportEdge, onProgress, onDone, overrideCurrentScrollingAnimation);
        }

        public override void BringToView(int cellIndex, float? spacingFromViewportEdge = null)
        {
            var   groupIndex = this.Parameters.GetGroupIndex(cellIndex);
            float normalizedOffsetFromViewportStart, itemPivotToUse;
            if (!this.BringToView_CalculateParameters(groupIndex, spacingFromViewportEdge, out normalizedOffsetFromViewportStart, out itemPivotToUse)) return;

            this.ScrollToGroup(groupIndex, normalizedOffsetFromViewportStart, itemPivotToUse);

            // TODO see SmoothBringToView when it's 100% done to also update this implementation
        }

        /// <summary>
        /// Overriding base's implementation so that we pass the cells count to our own implementation which converts them to group count before further passing it to the base impl.
        /// </summary>
        /// <summary>See <see cref="OSA{TParams, TItemViewsHolder}.Refresh(bool, bool)"/></summary>
        public override void Refresh(bool contentPanelEndEdgeStationary = false, bool keepVelocity = false)
        {
            this.ChangeItemsCount(ItemCountChangeMode.RESET, this._CellsCount, -1, contentPanelEndEdgeStationary, keepVelocity);
        }

        /// <summary>Overriding base's implementation to return the cells count, instead of the groups count</summary>
        /// <seealso cref="OSA{TParams, TItemViewsHolder}.GetItemsCount"/>
        public sealed override int GetItemsCount()
        {
            return this._CellsCount;
        }

        #region Cell views holders helpers

        public virtual int GetCellGroupsCount()
        {
            return base.GetItemsCount();
        }

        /// <summary>The number of visible cells</summary>
        public virtual int GetNumVisibleCells()
        {
            if (this.VisibleItemsCount == 0) return 0;
            return (this.VisibleItemsCount - 1) * this._Params.CurrentUsedNumCellsPerGroup + this.GetItemViewsHolder(this.VisibleItemsCount - 1).NumActiveCells;
        }

        /// <summary>
        /// <para>Retrieve the views holder of a cell with speciffic index in view. For example, one can iterate from 0 to <see cref="GetNumVisibleCells"/> </para>
        /// <para>in order to do something with each visible cell. Not to be mistaken for <see cref="GetCellViewsHolderIfVisible(int)"/>,</para>
        /// <para>which retrieves a cell by the index of its corresponding model in your data list (<see cref="AbstractViewsHolder.ItemIndex"/>)</para>
        /// </summary>
        public virtual TCellVH GetCellViewsHolder(int cellViewsHolderIndex)
        {
            if (this.VisibleItemsCount == 0) return null;

            if (cellViewsHolderIndex > this.GetNumVisibleCells() - 1) return null;

            return this.GetItemViewsHolder(this._Params.GetGroupIndex(cellViewsHolderIndex))
                .ContainingCellViewsHolders[cellViewsHolderIndex % this._Params.CurrentUsedNumCellsPerGroup];
        }

        /// <summary>
        /// <para>Retrieve the views holder of a cell whose associated model's index in your data list is <paramref name="withCellItemIndex"/>.</para>
        /// <para>Not to be mistaken for <see cref="GetCellViewsHolder(int)"/> which retrieves a cell by its index in the "all visible cells" list</para>
        /// </summary>
        /// <returns>null, if the item is outside the viewport (and thus no view is associated with it)</returns>
        public virtual TCellVH GetCellViewsHolderIfVisible(int withCellItemIndex)
        {
            var groupVH = this.GetItemViewsHolderIfVisible(this._Params.GetGroupIndex(withCellItemIndex));
            if (groupVH == null) return null;

            return this.GetCellViewsHolderIfVisible(groupVH, withCellItemIndex);
        }

        public virtual TCellVH GetCellViewsHolderIfVisible(CellGroupViewsHolder<TCellVH> groupVH, int withCellItemIndex)
        {
            var indexOfFirstCellInGroup = groupVH.ItemIndex * this._Params.CurrentUsedNumCellsPerGroup;

            if (withCellItemIndex < indexOfFirstCellInGroup + groupVH.NumActiveCells) return groupVH.ContainingCellViewsHolders[withCellItemIndex - indexOfFirstCellInGroup];

            return null;
        }

        /// <summary>
        /// <para>Similar to <see cref="OSA{TParams, TItemViewsHolder}.GetItemViewsHolderIfVisible(RectTransform)"/>, but used to retrieve an individual cell. </para>
        /// <para>For a grid, this method should be used instead of that, unless you want to retrieve the whole <see cref="CellGroupViewsHolder{TCellVH}"/>, which usually represents an entire row/column of cells.</para>
        /// </summary>
        public virtual TCellVH GetCellViewsHolderIfVisible(RectTransform withRoot)
        {
            for (var i = 0; i < this.VisibleItemsCount; i++)
            {
                var groupVH = this.GetItemViewsHolder(i);

                for (var j = 0; j < groupVH.NumActiveCells; j++)
                {
                    var cellVH = groupVH.ContainingCellViewsHolders[j];
                    if (cellVH.root == withRoot) return cellVH;
                }
            }

            return null;
        }

        public bool ForceUpdateCellViewsHolderIfVisible(int cellItemIndex)
        {
            var vh = this.GetCellViewsHolderIfVisible(cellItemIndex);
            if (vh == null) return false;

            this.UpdateCellViewsHolder(vh);

            return true;
        }

        /// <summary>Scroll to the specified GROUP. Use <see cref="ScrollTo(int, float, float)"/> if scrolling to a CELL was intended instead</summary>
        /// <seealso cref="OSA{TParams, TItemViewsHolder}.ScrollTo(int, float, float)"/>
        public virtual void ScrollToGroup(int groupIndex, float normalizedOffsetFromViewportStart = 0f, float normalizedPositionOfItemPivotToUse = 0f)
        {
            base.ScrollTo(groupIndex, normalizedOffsetFromViewportStart, normalizedPositionOfItemPivotToUse);
        }

        /// <summary>See <see cref="ScrollToGroup(int, float, float)"/></summary>
        public virtual bool SmoothScrollToGroup(
            int               groupIndex,
            float             duration,
            float             normalizedOffsetFromViewportStart    = 0f,
            float             normalizedPositionOfItemPivotToUse   = 0f,
            Func<float, bool> onProgress                           = null,
            Action            onDone                               = null,
            bool              overrideAnyCurrentScrollingAnimation = false
        )
        {
            return base.SmoothScrollTo(groupIndex, duration, normalizedOffsetFromViewportStart, normalizedPositionOfItemPivotToUse, onProgress, onDone, overrideAnyCurrentScrollingAnimation);
        }

        #endregion

        protected override NavigationManager<TParams, CellGroupViewsHolder<TCellVH>> CreateNavigationManager()
        {
            return new GridNavigationManager<TParams, TCellVH>(this);
        }

        protected override void OnInitialized()
        {
            base.OnInitialized();

            this._LastKnownUsedNumCellsPerGroup = this._Params.CurrentUsedNumCellsPerGroup;
        }

        protected sealed override void OnItemsRefreshed(int prevCount, int newCount)
        {
            base.OnItemsRefreshed(prevCount, newCount);

            this.OnCellGroupsRefreshed(prevCount, newCount);
        }

        /// <summary> Creates the Group viewsholder which instantiates the group prefab using the provided params in <see cref="OSA{TParams, TItemViewsHolder}.Init"/>. Only override it if you have a custom cell group prefab</summary>
        /// <seealso cref="OSA{TParams, TItemViewsHolder}.CreateViewsHolder(int)"/>
        /// <param name="itemIndex">the index of the GROUP (attention, not the CELL) that needs creation</param>
        /// <returns>The created group views holder </returns>
        protected override CellGroupViewsHolder<TCellVH> CreateViewsHolder(int itemIndex)
        {
            var instance = this.GetNewCellGroupViewsHolder();
            instance.Init(this._Params.GetGroupPrefab(itemIndex).gameObject, this._Params.Content, itemIndex, this._Params.Grid.CellPrefab, this._Params.CurrentUsedNumCellsPerGroup);

            for (var i = 0; i < instance.ContainingCellViewsHolders.Length; i++)
            {
                var cellVH = instance.ContainingCellViewsHolders[i];
                this.OnCellViewsHolderCreated(cellVH, instance);
            }
            this.ConfigureCellsLayoutForGroup(instance);

            return instance;
        }

        /// <summary>
        /// Here the grid adapter checks if new groups need to be created or if old ones need to be disabled or destroyed, after which it calls <see cref="UpdateCellViewsHolder(TCellVH)"/> for each remaining cells.
        /// <para>Override it (and call the base implementation!) only if you know what you're doing. If you just want to update your cells' views, do it in <see cref="UpdateCellViewsHolder(TCellVH)"/></para>
        /// </summary>
        /// <seealso cref="OSA{TParams, TItemViewsHolder}.UpdateViewsHolder(TItemViewsHolder)"/>
        /// <param name="newOrRecycled">The viewsholder of the group that needs updated</param>
        protected override void UpdateViewsHolder(CellGroupViewsHolder<TCellVH> newOrRecycled)
        {
            // At this point there are enough groups for sure, but there may not be enough enabled cells, or there may be too many enabled cells

            int activeCellsForThisGroup;
            // If it's the last one
            if (newOrRecycled.ItemIndex + 1 == this.GetCellGroupsCount())
            {
                var totalCellsBeforeThisGroup                              = 0;
                if (newOrRecycled.ItemIndex > 0) totalCellsBeforeThisGroup = newOrRecycled.ItemIndex * this._Params.CurrentUsedNumCellsPerGroup;
                activeCellsForThisGroup = this._CellsCount - totalCellsBeforeThisGroup;
            }
            else
            {
                activeCellsForThisGroup = this._Params.CurrentUsedNumCellsPerGroup;
            }
            newOrRecycled.NumActiveCells = activeCellsForThisGroup;

            for (var i = 0; i < activeCellsForThisGroup; ++i) this.UpdateCellViewsHolder(newOrRecycled.ContainingCellViewsHolders[i]);
        }

        /// <summary>Provide your own implementation of the group prefab, if you have a custom one. Most often than not, you won't use this</summary>
        protected virtual CellGroupViewsHolder<TCellVH> GetNewCellGroupViewsHolder()
        {
            return new();
        }

        /// <summary>Called for each cell in a cell group at the moment the group is first created</summary>
        /// <param name="cellVH"></param>
        /// <param name="cellGroup">The cell's group</param>
        protected virtual void OnCellViewsHolderCreated(TCellVH cellVH, CellGroupViewsHolder<TCellVH> cellGroup)
        {
        }

        /// <summary>The only important callback for inheritors. It provides cell's views holder which has just become visible and whose views should be updated from its corresponding data model. viewsHolder.ItemIndex(<see cref="AbstractViewsHolder.ItemIndex"/>) can be used to know what data model is associated with. </summary>
        /// <param name="viewsHolder">The cell's views holder</param>
        protected abstract void UpdateCellViewsHolder(TCellVH viewsHolder);

        /// <summary>
        /// Overridden in order to call <see cref="OnBeforeRecycleOrDisableCellViewsHolder(TCellVH, int)"/> for each active cell in the group
        /// </summary>
        /// <seealso cref="OSA{TParams, TItemViewsHolder}.OnBeforeRecycleOrDisableViewsHolder(TItemViewsHolder, int)"/>
        protected sealed override void OnBeforeRecycleOrDisableViewsHolder(CellGroupViewsHolder<TCellVH> inRecycleBinOrVisible, int newItemIndex)
        {
            base.OnBeforeRecycleOrDisableViewsHolder(inRecycleBinOrVisible, newItemIndex);

            for (var i = 0; i < inRecycleBinOrVisible.NumActiveCells; ++i)
            {
                var cellVh = inRecycleBinOrVisible.ContainingCellViewsHolders[i];
                int newCellIndex;

                if (newItemIndex == -1)
                    newCellIndex = -1;
                else
                    newCellIndex = newItemIndex * this._Params.CurrentUsedNumCellsPerGroup + i;

                this.OnBeforeRecycleOrDisableCellViewsHolder(cellVh, newCellIndex);
            }
        }

        /// <summary> This is not needed yet in case of grid adapters </summary>
        protected override void OnItemIndexChangedDueInsertOrRemove(CellGroupViewsHolder<TCellVH> shiftedViewsHolder, int oldIndex, bool wasInsert, int removeOrInsertIndex)
        {
            base.OnItemIndexChangedDueInsertOrRemove(shiftedViewsHolder, oldIndex, wasInsert, removeOrInsertIndex);
        }

        protected override void RebuildLayoutDueToScrollViewSizeChange()
        {
            base.RebuildLayoutDueToScrollViewSizeChange();

            //int prevCellsPerGroup = _Params.CurrentUsedNumCellsPerGroup;
            //int newCellsPerGroup = _Params.CalculateCurrentNumCellsPerGroup();
            var prevCellsPerGroup = this._LastKnownUsedNumCellsPerGroup;
            var newCellsPerGroup  = this._Params.CurrentUsedNumCellsPerGroup;
            this._LastKnownUsedNumCellsPerGroup = newCellsPerGroup;
            // The cell groups need to be rebuilt completely, since the new number of cells per group is different
            if (prevCellsPerGroup != newCellsPerGroup)
            {
                this.ClearVisibleItems();
                this.ClearCachedRecyclableItems();
            }
            else
            {
                // Existing cells may need to rebuild
                this.ConfigureCellsLayoutOfCellGroups(this._VisibleItems);
                this.ConfigureCellsLayoutOfCellGroups(this._RecyclableItems);
                this.ConfigureCellsLayoutOfCellGroups(this._BufferredRecyclableItems);
            }
        }

        /// <summary>The only important callback for inheritors. It provides cell's views holder which has just become visible and whose views should be updated from its corresponding data model. viewsHolder.ItemIndex(<see cref="AbstractViewsHolder.ItemIndex"/>) can be used to know what data model is associated with. </summary>
        /// <param name="viewsHolder">The cell's views holder</param>
        protected virtual void OnBeforeRecycleOrDisableCellViewsHolder(TCellVH viewsHolder, int newItemIndex)
        {
            viewsHolder.OnBeforeRecycleOrDisable(newItemIndex);
        }

        protected virtual void OnCellGroupsRefreshed(int prevGroupsCount, int curGroupsCount)
        {
            if (this._CellsRefreshed != null) this._CellsRefreshed(this._PrevCellsCount, this._CellsCount);
        }

        protected bool ScrollTo_ConvertItemPivotToUseIfPossible(int groupIndex, int cellIndex, ref float normalizedPositionOfItemPivotToUse)
        {
            var groupVH = this.GetItemViewsHolderIfVisible(groupIndex);

            if (groupVH == null) return false;

            // the group is visible => the search can be more granular
            var cellVH = this.GetCellViewsHolderIfVisible(groupVH, cellIndex);
            if (cellVH == null) throw new OSAException("GetItemViewsHolderIfVisible " + groupIndex + " got group vh, but GetCellViewsHolderIfVisible " + cellIndex + " got null ?. Please report this bug");

            var groupSize      = groupVH.root.rect.size[this._InternalState.layoutInfo.hor0_vert1];
            var cellSize       = cellVH.root.rect.size[this._InternalState.layoutInfo.hor0_vert1];
            var cellInsetStart = cellVH.root.GetInsetFromParentEdge(groupVH.root, this._InternalState.layoutInfo.startEdge);

            normalizedPositionOfItemPivotToUse = (cellInsetStart + normalizedPositionOfItemPivotToUse * cellSize) / groupSize;

            return true;
        }

        private void ConfigureCellsLayoutOfCellGroups(List<CellGroupViewsHolder<TCellVH>> groups)
        {
            for (var i = 0; i < groups.Count; i++)
            {
                var groupVH = groups[i];
                this.ConfigureCellsLayoutForGroup(groupVH);
            }
        }

        private void ConfigureCellsLayoutForGroup(CellGroupViewsHolder<TCellVH> cellGroup)
        {
            var changed = false;
            for (var i = 0; i < cellGroup.ContainingCellViewsHolders.Length; i++)
            {
                var cellVH = cellGroup.ContainingCellViewsHolders[i];
                changed = this._Params.ConfigureCellViewsHolderAspectRatio(cellVH) || changed;
            }

            // Important. Fixes a bug where ScrollTo would scroll to the wrong group
            if (changed) LayoutRebuilder.ForceRebuildLayoutImmediate(cellGroup.root);
        }

        // WIP
        //protected bool BringToView_ConvertItemInsetFromParentEdgeIfPossible(int groupIndex, int cellIndex, ref float spacingFromViewportEdge)
        //{
        //	var groupVH = GetItemViewsHolderIfVisible(groupIndex);
        //	if (groupVH == null)
        //		return false;

        //	// the group is visible => the search can be more granular
        //	var cellVH = GetCellViewsHolderIfVisible(groupVH, cellIndex);
        //	if (cellVH == null)
        //		throw new OSAException("GetItemViewsHolderIfVisible " + groupIndex + " got group vh, but GetCellViewsHolderIfVisible " + cellIndex + " got null ?. Please report this bug");

        //	float groupSize = groupVH.root.rect.size[_InternalState.layoutInfo.hor0_vert1];
        //	float cellSize = cellVH.root.rect.size[_InternalState.layoutInfo.hor0_vert1];
        //	float cellInsetStart = cellVH.root.GetInsetFromParentEdge(groupVH.root, _InternalState.layoutInfo.startEdge);

        //	spacingFromViewportEdge = (cellInsetStart + spacingFromViewportEdge * cellSize) / groupSize;

        //	return true;
        //}
    }
}