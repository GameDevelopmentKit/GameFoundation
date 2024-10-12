using System;
using System.Collections.Generic;
using UnityEngine;
using frame8.Logic.Misc.Visual.UI.MonoBehaviours;
using frame8.Logic.Misc.Other.Extensions;
using Com.ForbiddenByte.OSA.Core;
using Com.ForbiddenByte.OSA.CustomAdapters.TableView.Tuple;
using Com.ForbiddenByte.OSA.CustomAdapters.TableView.Input;

namespace Com.ForbiddenByte.OSA.CustomAdapters.TableView
{
    /// <summary>
    /// The TableAdapter is generally intended for use with data that resembles the format of a database's table, i.e. containing columns of potentially different types
    /// and rows (which are called tuples here). The maximum supported number of rows is the same as for any other OSA: <see cref="OSAConst.MAX_ITEMS"/>
    /// <para>The minimal amount of information needed is how many columns there are, their type and a <see cref="ITupleProvider"/> which returns and <see cref="ITuple"/> for a row, when requested</para>
    /// <para>If you a huge amount of rows, generally over 10K, please make use of <see cref="Extra.BufferredTableData"/> 
    /// and <see cref="Extra.AsyncBufferredTableData{TTuple}"/> to get a faster initial loading speed. 
    /// You can use these by default even with smaller data sets, and the performance will still beat the one of <see cref="BasicTableData"/></para>
    /// <para>Changing the data is only available via <see cref="ResetTable(ITableColumns, ITupleProvider)"/>, <see cref="ResetTableWithCurrentData"/> and <see cref="RefreshRange(int, int)"/></para>
    /// </summary>
    /// <typeparam name="TParams"></typeparam>
    /// <typeparam name="TTupleViewsHolder">The 'row' ViewsHolder, which here is called tuple</typeparam>
    /// <typeparam name="THeaderTupleViewsHolder">The 'columns' ViewsHolder, which here is called header</typeparam>
    public abstract class TableAdapter<TParams, TTupleViewsHolder, THeaderTupleViewsHolder> : OSA<TParams, TTupleViewsHolder>, ITupleAdapterSizeHandler, ITableAdapter
        where TParams : TableParams
        where TTupleViewsHolder : TupleViewsHolder, new()
        where THeaderTupleViewsHolder : TupleViewsHolder, new()
    {
        /// <summary>See <see cref="ITableColumns"/></summary>
        public ITableColumns Columns { get; protected set; }

        /// <summary>See <see cref="ITupleProvider"/></summary>
        public ITupleProvider Tuples { get; protected set; }

        /// <summary>See <see cref="ITableViewOptionsPanel"/></summary>
        public ITableViewOptionsPanel Options { get; protected set; }

        TableParams ITableAdapter.TableParameters => this._Params;

        protected bool _ShowDebugLogs = false;

        protected THeaderTupleViewsHolder _Header;

        private ITuple                              _ColumnsAsTuple;
        private TupleViewsHolder                    _TupleVHActivelyMoving;
        private ScrollbarFixer8                     _ScrollbarFixerColumns;
        private ScrollbarFixer8                     _ScrollbarFixerTuples;
        private TableViewFloatingDropdownController _SharedFloatingDropdownControllerInstance;
        private TableViewTextInputController        _SharedTextInputControllerInstance;
        private float                               _LastTimeCheckedForResizingAvailabilityViaDraggers;
        private int                                 _ManualSyncPositionsActionsPending;
        private HashSet<RectTransform>              _TupleValuePrefabsResizedToFitHeader = new();

        /// <summary>Not available for TableAdapter</summary>
        public sealed override void InsertItems(int index, int itemsCount, bool contentPanelEndEdgeStationary = false, bool keepVelocity = false)
        {
            throw new OSAException("Not available for TableAdapter. Use ResetTable(ITableColumns, ITupleProvider) instead.");
        }

        /// <summary>Not available for TableAdapter</summary>
        public sealed override void RemoveItems(int index, int itemsCount, bool contentPanelEndEdgeStationary = false, bool keepVelocity = false)
        {
            throw new OSAException("Not available for TableAdapter. Use ResetTable(ITableColumns, ITupleProvider) instead.");
        }

        /// <summary>Populate the TableAdapter with new data</summary>
        public void ResetTable(ITableColumns columns, ITupleProvider tuples)
        {
            this.Columns = columns;
            this.Tuples  = tuples;

            // Init is called in Start() by OSA by design. But we make sure it's called here as well, 
            // in case SetData is called before Start()
            if (!this.IsInitialized) this.Init();

            this.ResetTableWithCurrentData();
        }

        /// <summary>
        /// Resets the table with the existing data about columns and tuples, as supplied in the last call of <see cref="ResetTable(ITableColumns, ITupleProvider)"/>.
        /// </summary>
        public void ResetTableWithCurrentData()
        {
            // Init is called in Start() by OSA by design. But we make sure it's called here as well, 
            // in case ResetTableWithCurrentData is called before Start()
            if (!this.IsInitialized) this.Init();

            this.Options         = this.FindOptionsPanel();
            this._ColumnsAsTuple = this.Columns.GetColumnsAsTuple();

            this._Header.Adapter.ResetWithTuple(this._ColumnsAsTuple, this.Columns);

            // Calling base, because ResetItems is not available on TableAdapter
            this.ResetItems(this.Tuples.Count);

            // Fix to keep all tuples and header positions in sync
            this._Header.Adapter.SetNormalizedPosition(0d);
        }

        /// <summary>
        /// Instead of wholly resetting the adapter with new items, this is provided to only reset a range of items.
        /// Very useful when you modify a range of tuples from outside and want to commit the changes to view.
        /// <para>If none of the items whithin the supplied range is visible, this method literarly has 0 performance cost, because items are updated each time they become visible, by OSA's design</para>
        /// </summary>
        public virtual void RefreshRange(int firstIndex, int count)
        {
            var    lastIndexExcl = firstIndex + count;
            var    totalAffected = 0;
            string firstAffected = "(none)", firstNotAffected = firstAffected;
            for (var i = 0; i < this.VisibleItemsCount; i++)
            {
                var vh = this.GetItemViewsHolder(i);
                if (vh.ItemIndex >= firstIndex && vh.ItemIndex < lastIndexExcl)
                {
                    if (this._ShowDebugLogs)
                    {
                        if (totalAffected == 0) firstAffected = vh.ItemIndex + "(vh #" + i + ")";
                        ++totalAffected;
                    }
                    this.UpdateViewsHolder(vh);
                }
                else
                {
                    if (this._ShowDebugLogs) firstNotAffected = vh.ItemIndex + "(vh #" + i + ")";
                }
            }

            if (this._ShowDebugLogs)
                Debug.Log(
                    "Refreshing range [" + firstIndex + ", " + lastIndexExcl + "), totalling " + (lastIndexExcl - firstIndex) + " items:\n" + "Visible VHs found: firstAffected " + firstAffected + ", firstNotAffected " + firstNotAffected + ", totaling " + totalAffected + " VHs"
                );

            // Resetting column sorting, since new values might've deprecated the current sorting state
            if (this.Tuples.ColumnSortingSupported) this.InvalidateAnyColumnSorting();
        }

        public virtual void SetColumnSorting(int columnIndex, TableValueSortType type)
        {
            this.Columns.GetColumnState(columnIndex).CurrentSortingType = type;

            this.OnSortingTypeChangedForColumn(columnIndex, type);
        }

        public virtual void SetColumnIsReadonly(int columnIndex, bool isReadonly)
        {
            this.Columns.GetColumnState(columnIndex).CurrentlyReadOnly = isReadonly;

            // Update visible values for this column, in all visible tuples. 
            // The ones that will become visible after, will naturally be updated
            for (var i = 0; i < this.VisibleItemsCount; i++)
            {
                var tupleVH = this.GetItemViewsHolder(i);
                tupleVH.Adapter.ForceUpdateValueViewsHolderIfVisible(columnIndex);
            }

            this.OnIsReadonlyChangedForColumn(columnIndex, isReadonly);
        }

        /// <summary>
        /// Catching this to cancel movement of children adapters when this adapter is requested to stop.
        /// This happens in multiple cases, but most notably, when the scrollbar is dragged and when <see cref="BaseParams.Effects.CutMovementOnPointerDown"/> is true and a drag begins
        /// See <see cref="OSA{TParams, TItemViewsHolder}.StopMovement"/>
        /// </summary>
        public override void StopMovement()
        {
            base.StopMovement();
            this.StopMovementOfAllVisibleTupleVHs();
        }

        /// <inheritdoc/>
        protected override void Awake()
        {
            // Make room at the start (top or left) for the header. It's important to set this before the adapter is initialized, 
            // because it caches the padding value during that time and only re-reads it from params when major events happen, like a resizing of the ScrollView
            var ctPad    = this._Params.ContentPadding;
            var padToSet = (int)(this._Params.Table.ColumnsTupleSize + this._Params.Table.ColumnsTupleSpacing + .5f);
            if (this.IsHorizontal)
                ctPad.left = padToSet;
            else
                ctPad.top = padToSet;

            this._Header = this.CreateHeaderViewsHolder();
            if (this._Params.Table.ColumnsScrollbar)
            {
                this._Header.Adapter.TupleParameters.Scrollbar = this._Params.Table.ColumnsScrollbar;
                this._ScrollbarFixerColumns = OSAUtil.ConfigureDinamicallyCreatedScrollbar(this._Params.Table.ColumnsScrollbar,
                    this._Header.Adapter,
                    this._Header.Adapter.TupleParameters.Viewport,
                    false
                );
            }

            if (this._Params.Scrollbar) this._ScrollbarFixerTuples = this._Params.Scrollbar.GetComponent<ScrollbarFixer8>();

            base.Awake();
        }

        protected override void Update()
        {
            base.Update();

            if (!this.IsInitialized) return;

            // Avoid doing it too often, since it's a low-priority task
            if (this.Time - this._LastTimeCheckedForResizingAvailabilityViaDraggers > .5f) this.CheckResizingAvailableViaDragger();

            if (this._ManualSyncPositionsActionsPending > 0) this.SyncPositionsIfNeeded(true);
        }

        /// <summary>
        /// See <see cref="OSA{TParams, TItemViewsHolder}.ChangeItemsCount(ItemCountChangeMode, int, int, bool, bool)"/>
        /// </summary>
        /// <param name="changeMode"></param>
        /// <param name="itemsCount"></param>
        /// <param name="indexIfInsertingOrRemoving"></param>
        /// <param name="contentPanelEndEdgeStationary"></param>
        /// <param name="keepVelocity"></param>
        public override void ChangeItemsCount(ItemCountChangeMode changeMode, int itemsCount, int indexIfInsertingOrRemoving = -1, bool contentPanelEndEdgeStationary = false, bool keepVelocity = false)
        {
            // On any adding/resetting items, invalidate current sorting types of current columns
            // When items are removed, sorting remains valid
            if (changeMode != ItemCountChangeMode.REMOVE) this.InvalidateAnyColumnSorting();

            this.InvalidateOptionsPanelState();

            base.ChangeItemsCount(changeMode, itemsCount, indexIfInsertingOrRemoving, contentPanelEndEdgeStationary, keepVelocity);
        }

        /// <summary>
        /// Create value tuples. The header tuple is created by a different callback - <see cref="CreateHeaderViewsHolder"/>
        /// <para>See also <see cref="OSA{TParams, TItemViewsHolder}.CreateViewsHolder(int)"/></para>
        /// </summary>
        protected override TTupleViewsHolder CreateViewsHolder(int itemIndex)
        {
            var vh = new TTupleViewsHolder();
            this.InitTupleViewsHolder(vh, this._Params.Table.TuplePrefab, this._Params.Content, itemIndex);
            this.InitValueTupleViewsHolder(vh);

            return vh;
        }

        protected virtual THeaderTupleViewsHolder CreateHeaderViewsHolder()
        {
            var vh = new THeaderTupleViewsHolder();
            // Viewport is the header's parent, as opposed to Content, which is the parent of the regular items
            this.InitTupleViewsHolder(vh, this._Params.Table.ColumnsTuplePrefab, this._Params.Viewport, -1);
            this.InitHeaderTupleViewsHolder(vh);

            return vh;
        }

        /// <summary>
        /// Used to update both value tuples and header tuples.
        /// <para>See also <see cref="OSA{TParams, TItemViewsHolder}.UpdateViewsHolder(TItemViewsHolder)"/></para>
        /// </summary>
        protected override void UpdateViewsHolder(TTupleViewsHolder newOrRecycled)
        {
            var tuple = this.Tuples.GetTuple(newOrRecycled.ItemIndex);

            newOrRecycled.UpdateViews(tuple, this.Columns);
        }

        /// <summary>
        /// Initializes a viewsholder on creation. This is called for both the header and the value tuples, when they're first created
        /// </summary>
        protected virtual void InitTupleViewsHolder(TupleViewsHolder vh, RectTransform prefab, RectTransform parent, int itemIndex)
        {
            vh.Init(prefab, parent, itemIndex);

            // Keeping the same time scale in children as in the parent
            vh.Adapter.TupleParameters.UseUnscaledTime = this._Params.UseUnscaledTime;

            // Making sure the tuple's value prefab has the same size in the tuple's scrolling direction, so they'll be perfectly aligned
            if (this._Header != null) // header will be created first through this method, in which case it'll be null
            {
                var tupleValuePrefab = vh.Adapter.TupleParameters.ItemPrefab;
                if (!this._TupleValuePrefabsResizedToFitHeader.Contains(tupleValuePrefab))
                {
                    UnityEngine.Assertions.Assert.IsTrue(this._Header != null);
                    var headerSize = this._Header.Adapter.TupleParameters.ItemPrefab.rect.size;
                    var hor1_vert0 = 1 - this._InternalState.hor0_vert1;
                    tupleValuePrefab.SetSizeWithCurrentAnchors((RectTransform.Axis)hor1_vert0, headerSize[hor1_vert0]);
                    this._TupleValuePrefabsResizedToFitHeader.Add(tupleValuePrefab);
                }
            }

            vh.Adapter.Init();
        }

        /// <inheritdoc/>
        protected override void OnBeforeRecycleOrDisableViewsHolder(TTupleViewsHolder inRecycleBinOrVisible, int newItemIndex)
        {
            //inRecycleBinOrVisible.childGrid.ClearItemsAndUpdate();
            //inRecycleBinOrVisible.childGrid.StopMovement();

            if (inRecycleBinOrVisible == this._TupleVHActivelyMoving)
                // Transfer the control to the header, when the current non-header active item is being recycled
                this.TransferVelocityOfActivelyMovingTupleToHeader();

            inRecycleBinOrVisible.Adapter.OnWillBeRecycled(this._Params.DefaultItemSize);
            inRecycleBinOrVisible.root.SetSizeWithCurrentAnchors((RectTransform.Axis)this._InternalState.hor0_vert1, this._Params.DefaultItemSize);

            base.OnBeforeRecycleOrDisableViewsHolder(inRecycleBinOrVisible, newItemIndex);
        }

        protected override void OnScrollPositionChanged(double normPos)
        {
            this.SyncPositionsIfNeeded(true);
            this.CheckResizingAvailableViaDragger();
            this.CloseInputControllers();

            base.OnScrollPositionChanged(normPos);
        }

        protected override void PostRebuildLayoutDueToScrollViewSizeChange()
        {
            base.PostRebuildLayoutDueToScrollViewSizeChange();

            this.CloseInputControllers();
            this.SyncPositionsIfNeeded(true);

            // Need to sync them again for the next few updates, as they're not properly updated immediately, nor after 1 or 2 frames
            this._ManualSyncPositionsActionsPending = 10;
        }

        //void OnTupleValueChangedFromInput(TTupleViewsHolder tupleVH, TupleValueViewsHolder tupleValueVH, object newValue)
        //{
        //	var columnIndex = tupleValueVH.ItemIndex;
        //	var tupleModel = _TupleProvider.GetTuple(tupleVH.ItemIndex);
        //	SetTupleValueAndResetColumnSortingIfNeeded(tupleModel, columnIndex, newValue);
        //	tupleVH.Adapter.ForceUpdateValueViewsHolder(tupleValueVH);
        //}

        protected virtual void OnTupleValueClicked(TTupleViewsHolder tupleVH, TupleValueViewsHolder tupleValueVH)
        {
            var columnIndex = tupleValueVH.ItemIndex;
            var column      = this.Columns.GetColumnState(columnIndex);
            if (!column.CurrentlyReadOnly)
            {
                if (this.Options != null && this.Options.IsClearing)
                {
                    this.ClearValue(tupleVH.ItemIndex, columnIndex);
                    return;
                }

                var multiLineTextInput   = false;
                var showInputText        = false;
                var defaultValueAsString = "";
                switch (column.Info.ValueType)
                {
                    case TableValueType.STRING:
                        multiLineTextInput = true;
                        showInputText      = true;
                        break;

                    case TableValueType.BOOL:
                        if (this.Tuples.GetTuple(tupleVH.ItemIndex).GetValue(columnIndex) == null) this.OnTupleValueChangedFromInput(tupleVH, tupleValueVH, false);
                        return;

                    case TableValueType.INT:
                    case TableValueType.LONG_INT:
                    case TableValueType.FLOAT:
                    case TableValueType.DOUBLE:
                        defaultValueAsString = "0";
                        showInputText        = true;
                        break;

                    case TableValueType.ENUMERATION:
                        var dropdown = this.GetOrInstantiateFloatingDropdown();
                        if (dropdown)
                        {
                            // Make sure the panel won't be displaced from the value box
                            this.StopMovementOfAllVisibleTupleVHs();
                            this.StopMovement();

                            var invalidValue = int.MinValue;
                            dropdown.InitWithEnum(column.Info.EnumValueType);
                            dropdown.ShowFloating(
                                tupleValueVH.root,
                                selectedValue =>
                                {
                                    var selectionInt = (int)selectedValue;
                                    if (selectionInt == invalidValue) return;

                                    //var tupleModel = _TupleProvider.GetTuple(tupleVH.ItemIndex);
                                    //SetTupleValueAndResetColumnSortingIfNeeded(tupleModel, columnIndex, selectedValue);
                                    //tupleVH.Adapter.ForceUpdateValueViewsHolder(tupleValueVH);
                                    this.OnTupleValueChangedFromInput(tupleVH, tupleValueVH, selectedValue);
                                },
                                invalidValue
                            );
                        }
                        return;
                }
                if (showInputText)
                {
                    var input = this.GetOrInstantiateTextInputController();
                    if (input)
                    {
                        // Make sure the panel won't be displaced from the value box
                        this.StopMovementOfAllVisibleTupleVHs();
                        this.StopMovement();

                        // Initial value same as the current one, which is expected to be the exact one from the model
                        var curValue               = this.Tuples.GetTuple(tupleVH.ItemIndex).GetValue(columnIndex);
                        var curValueNull           = curValue == null;
                        var textComponentOnValueVH = tupleValueVH.TextComponent;
                        var initialText            = curValueNull ? defaultValueAsString : textComponentOnValueVH.text;

                        var prevAlpha        = textComponentOnValueVH.SetAlpha(0f);
                        var supportsRichText = textComponentOnValueVH.supportRichText;
                        textComponentOnValueVH.supportRichText = false; // override any current color, otherwise alpha doesn't have any effect
                        input.fontSize                         = textComponentOnValueVH.fontSize;
                        input.ShowFloating(
                            textComponentOnValueVH.RT,
                            initialText,
                            multiLineTextInput,
                            text =>
                            {
                                textComponentOnValueVH.SetAlpha(prevAlpha);
                                // Update: actually, this is not needed, as we'll have a valid value anyway
                                //tupleValueVH.TextComponent.supportRichText = supportsRichText;
                                this.OnTupleValueChangedFromInput(tupleVH, tupleValueVH, text);
                            },
                            () =>
                            {
                                textComponentOnValueVH.SetAlpha(prevAlpha);
                            }
                        );
                    }

                    return;
                }
            }
            tupleValueVH.ProcessUnhandledClick();
        }

        protected virtual void OnTupleValueChangedFromInput(TTupleViewsHolder tupleVH, TupleValueViewsHolder tupleValueVH, object newValue)
        {
            var    columnIndex = tupleValueVH.ItemIndex;
            var    column      = this.Columns.GetColumnState(columnIndex);
            var    tuple       = this.Tuples.GetTuple(tupleVH.ItemIndex);
            object valueToSet  = null;
            string asStr;
            var    setValue      = false;
            var    forceUpdateVH = false;
            switch (column.Info.ValueType)
            {
                case TableValueType.RAW:
                    valueToSet = newValue;
                    setValue   = true;
                    break;

                case TableValueType.STRING:
                    asStr = newValue as string;
                    if (asStr == null) return;

                    forceUpdateVH = true;

                    valueToSet = asStr;
                    setValue   = true;
                    break;

                case TableValueType.INT:
                    asStr = newValue as string;
                    if (asStr == null) return;

                    forceUpdateVH = true;

                    int asInt;
                    if (!int.TryParse(asStr, out asInt) /*invalid*/)
                        // Resetting the original value
                        break;

                    // Commented code kept to aknowledge this case. Same may happen to long, float, double
                    //if (asInt.ToString() != asStr /*valid, but resolved to a different value (for example, -0123 is valid, but resolves to -123)*/)
                    //{
                    //	// Changing the vh value to the new resolved number, but also allowing setValue=true below, 
                    //	// beacause the model also needs to be updated with the new resolved value, which is different
                    //	forceUpdateVH = true;
                    //}

                    valueToSet = asInt;
                    setValue   = true;
                    break;

                case TableValueType.LONG_INT:
                    asStr = newValue as string;
                    if (asStr == null) return;

                    forceUpdateVH = true;

                    long asLong;
                    if (!long.TryParse(asStr, out asLong) /*invalid*/)
                        // Resetting the original value
                        break;

                    //if (asLong.ToString() != asStr /*valid, but resolved to a different value (for example, -0123 is valid, but resolves to -123)*/)
                    //{
                    //	// Changing the vh value to the new resolved number, but also allowing setValue=true below, 
                    //	// beacause the model also needs to be updated with the new resolved value, which is different
                    //	forceUpdateVH = true;
                    //}

                    valueToSet = asLong;
                    setValue   = true;
                    break;

                case TableValueType.FLOAT:
                    asStr = newValue as string;
                    if (asStr == null) return;

                    forceUpdateVH = true;

                    float asFloat;
                    if (!float.TryParse(asStr, out asFloat) /*invalid*/)
                        // Resetting the original value
                        break;

                    valueToSet = asFloat;
                    setValue   = true;
                    break;

                case TableValueType.DOUBLE:
                    asStr = newValue as string;
                    if (asStr == null) return;

                    forceUpdateVH = true;

                    double asDouble;
                    if (!double.TryParse(asStr, out asDouble) /*invalid*/)
                        // Resetting the original value
                        break;

                    valueToSet = asDouble;
                    setValue   = true;
                    break;

                // Enums are set via dropdown and expected to always be valid (or null)
                case TableValueType.ENUMERATION:
                    forceUpdateVH = true;
                    valueToSet    = newValue;
                    setValue      = true;

                    break;

                case TableValueType.BOOL:
                    if (newValue != null && !(newValue is bool)) return;

                    forceUpdateVH = true;
                    valueToSet    = newValue;
                    setValue      = true;
                    break;

                // Textures aren't handled (can't change)
                //case TableValueType.TEXTURE:
                //	break;

                default: return;
            }

            if (setValue) this.SetTupleValueAndResetColumnSortingIfNeeded(tuple, columnIndex, valueToSet);
            if (forceUpdateVH) tupleVH.Adapter.ForceUpdateValueViewsHolder(tupleValueVH);
        }

        protected virtual void OnHeaderColumnClicked(THeaderTupleViewsHolder headerTupleVH, TupleValueViewsHolder columnValueVH)
        {
            var columnIndex = columnValueVH.ItemIndex;

            if (this.Options != null && this.Options.IsClearing)
            {
                if (!this.Tuples.ColumnClearingSupported) return;

                // The tuple provider may have its own way of clearing the entire column, which could be more optimized
                this.Tuples.SetAllValuesOnColumn(columnIndex, null);
                for (var i = 0; i < this.VisibleItemsCount; i++)
                {
                    var tupleVH = this.GetItemViewsHolder(i);
                    tupleVH.Adapter.ForceUpdateValueViewsHolderIfVisible(columnIndex);
                }

                this.SetColumnSorting(columnIndex, TableValueSortType.NONE);
                return;
            }

            if (!this.Tuples.ColumnSortingSupported) return;

            var columnState = this.Columns.GetColumnState(columnIndex);

            // If current is raw or descending, set to ascending. Otherwise, to descending
            TableValueSortType nextSorting;
            if (columnState.CurrentSortingType == TableValueSortType.ASCENDING)
                nextSorting = TableValueSortType.DESCENDING;
            else
                nextSorting = TableValueSortType.ASCENDING;

            var done = this.Tuples.ChangeColumnSortType(columnIndex, columnState.Info.ValueType, columnState.CurrentSortingType, nextSorting);
            if (!done) return;

            // Update: this is already done as a result or Refresh
            //// Change all other columns' stored sorting type to NONE
            //for (int i = 0; i < _Columns.ColumnsCount; i++)
            //{
            //	if (i == columnIndex)
            //		continue;

            //	SetColumnSorting(i, TableValueSortType.NONE);
            //}

            this.Refresh();

            // Setting column sorting after Refresh, because it's reset in ChangeItemsCount by design
            this.SetColumnSorting(columnIndex, nextSorting);
        }

        /// <summary>
        /// <para>Called when something causes a column to change its sorting type. </para>
        /// </summary>
        /// <param name="columnValueVHIfVisible"> The views holder representing the column in the header. Null if not visible</param>
        /// <param name="type">The new sorting type</param>
        protected virtual void OnSortingTypeChangedForColumn(int columnIndex, TableValueSortType type)
        {
            this._Header.Adapter.ForceUpdateValueViewsHolderIfVisible(columnIndex);
        }

        protected virtual void OnIsReadonlyChangedForColumn(int columnIndex, bool isReadonly)
        {
            this._Header.Adapter.ForceUpdateValueViewsHolderIfVisible(columnIndex);
        }

        protected virtual void CloseInputControllers()
        {
            if (this._SharedFloatingDropdownControllerInstance) this._SharedFloatingDropdownControllerInstance.Hide();

            if (this._SharedTextInputControllerInstance) this._SharedTextInputControllerInstance.Hide();
        }

        void ITupleAdapterSizeHandler.RequestChangeTransversalSize(ITupleAdapter adapter, double size)
        {
            var vh = this.GetItemViewsHolderIfVisible(adapter.TupleParameters.ScrollViewRT);
            if (vh == null) throw new OSAException("Tuple adapter requests size change, but it can't be found among visible children?");

            var  prevVelocity = this.Velocity;
            bool endStat;
            var  resizeMode     = adapter.TupleParameters.ResizingMode;
            var  isFitToContent = resizeMode == TableResizingMode.AUTO_FIT_TUPLE_CONTENT;
            var  cutVelocity    = false;

            if (isFitToContent)
            {
                var abstrVelocity = prevVelocity[this._InternalState.hor0_vert1] * this._InternalState.hor1_vertMinus1;
                endStat = abstrVelocity != 0f && Mathf.Sign(abstrVelocity) == 1;
            }
            else
            {
                endStat = false;
                cutVelocity = resizeMode == TableResizingMode.MANUAL_TUPLES
                    || resizeMode == TableResizingMode.MANUAL_COLUMNS
                    || resizeMode == TableResizingMode.MANUAL_COLUMNS_AND_TUPLES;

                if (cutVelocity)
                {
                    // If the actual size didn't change, don't cut velocity, because nothing was affected
                    var knownSize  = this._ItemsDesc[vh.itemIndexInView];
                    var difference = Math.Abs(knownSize - size);
                    cutVelocity = difference > 1d;
                }
            }
            var computeVisibility = false;
            var correctPos        = true;
            this.RequestChangeItemSizeAndUpdateLayout(vh, (float)size, endStat, computeVisibility, correctPos, !cutVelocity);
        }

        private void InitValueTupleViewsHolder(TTupleViewsHolder vh)
        {
            vh.Adapter.ValueClicked          += (valueVH) => this.OnTupleValueClicked(vh, valueVH);
            vh.Adapter.ValueChangedFromInput += (valueVH, newValue) => this.OnTupleValueChangedFromInput(vh, valueVH, newValue);
            vh.Adapter.ScrollPositionChanged += pos => this.OnTupleAdapterScrollPositionChanged(vh, pos);
            vh.Adapter.SizeHandler           =  this;
        }

        /// <summary>Supporting Vertical scroll views only, ATM</summary>
        private void InitHeaderTupleViewsHolder(THeaderTupleViewsHolder vh)
        {
            vh.root.name = "Header";
            var root = vh.root;
            root.anchorMin = new(0f, 1f); // top-left
            root.anchorMax = Vector2.one; // top-right
            root.SetInsetAndSizeFromParentEdgeWithCurrentAnchors(RectTransform.Edge.Top, 0f, this._Params.Table.ColumnsTupleSize);
            root.SetInsetAndSizeFromParentEdgeWithCurrentAnchors(RectTransform.Edge.Left, 0f, (root.parent as RectTransform).rect.width);
            root.pivot = new(.5f, 1f); // center-top

            vh.Adapter.ValueClicked          += (valueVH) => this.OnHeaderColumnClicked(vh, valueVH);
            vh.Adapter.ScrollPositionChanged += this.OnHeaderTupleAdapterScrollPositionChanged;
        }

        private void SetTupleValueAndResetColumnSortingIfNeeded(ITuple tuple, int columnIndex, object newValue)
        {
            var oldValue = tuple.GetValue(columnIndex);
            tuple.SetValue(columnIndex, newValue);

            // Reset column sorting only if needed
            // Using object.Equals to bypass unboxing
            if (!Equals(oldValue, newValue)) this.SetColumnSorting(columnIndex, TableValueSortType.NONE);
        }

        private void ClearValue(int tupleIndex, int columnIndex)
        {
            var tuple = this.Tuples.GetTuple(tupleIndex);

            this.SetTupleValueAndResetColumnSortingIfNeeded(tuple, columnIndex, null);
            var tupleVHIfVisible = this.GetItemViewsHolderIfVisible(tupleIndex);
            if (tupleVHIfVisible != null) tupleVHIfVisible.Adapter.ForceUpdateValueViewsHolderIfVisible(columnIndex);
        }

        private bool AssureActivelyMovingVHIsStillActive()
        {
            var vh = this._TupleVHActivelyMoving;
            if (vh != null)
                if (!vh.Adapter.IsDragging && vh.Adapter.Velocity.magnitude == 0f)
                    this._TupleVHActivelyMoving = null;

            return this._TupleVHActivelyMoving != null;
        }

        private void OnTupleAdapterScrollPositionChanged(TupleViewsHolder vh, double scrollPosition)
        {
            // Don't override a currently dragged scrollbar
            if (this.IsColumnsScrolbarDragging())
            {
                // And also make sure it doesn't have any inertia left from a previous drag
                vh.Adapter.StopMovement();

                // Also do the same with the currently active vh, if it's different
                if (this._TupleVHActivelyMoving != null)
                {
                    if (this._TupleVHActivelyMoving != vh) this._TupleVHActivelyMoving.Adapter.StopMovement();

                    this._TupleVHActivelyMoving = null;
                }

                return;
            }

            if (!vh.Adapter.IsDragging)
            {
                if (!this.AssureActivelyMovingVHIsStillActive()) return;

                // Always allow the last dragged to continue influencing other vhs even after the drag has finished
                if (this._TupleVHActivelyMoving != vh) return;
            }

            // Make sure no other adapter is in the Dragging state (can happen sometimes)
            for (var i = 0; i < this.VisibleItemsCount; i++)
            {
                var otherVH = this.GetItemViewsHolder(i);
                if (otherVH == vh) continue;
                if (otherVH.Adapter.IsDragging) return;
            }
            this._TupleVHActivelyMoving = vh;

            // Only set for the header, and its event will further set the position on the value tuples
            this.SetScrollPositionFor(this._Header, scrollPosition, false);
        }

        private void OnHeaderTupleAdapterScrollPositionChanged(double scrollPosition)
        {
            // If the header is dragged directly, override everything
            if (this._Header.Adapter.IsDragging)
                if (this._TupleVHActivelyMoving != null)
                    this._TupleVHActivelyMoving = null;

            if (this._TupleVHActivelyMoving == null)
            {
                // Header dragged directly or via scrollbar
            }
            else
                // Header dragged as a result of a value tuple being dragged in OnTupleAdapterScrollPositionChanged
                this.AssureActivelyMovingVHIsStillActive();

            this.SetScrollPositionForAllChildrenExceptDraggedAndHeader(scrollPosition);
        }

        private void SyncPositionsIfNeeded(bool force)
        {
            if (this.VisibleItemsCount == 0) return;

            if (this._TupleVHActivelyMoving == null)
            {
                if (!force) return;

                // First
                //_ValueTupleVHActivelyMoving = GetItemViewsHolder(0);
                this._TupleVHActivelyMoving = this._Header;
            }
            else
            {
                if (!force)
                {
                    this.AssureActivelyMovingVHIsStillActive();

                    if (this._TupleVHActivelyMoving == null) return;
                }
            }

            if (!force)
            {
                // Don't override a currently dragged scrollbar
                if (this.IsColumnsScrolbarDragging()) return;

                // Already syncing in OnDrag
                if (this._TupleVHActivelyMoving.Adapter.IsDragging) return;
            }

            --this._ManualSyncPositionsActionsPending;
            var pos = this._TupleVHActivelyMoving.Adapter.GetNormalizedPosition();
            this.SetScrollPositionForAllChildrenExceptDraggedAndHeader(pos);
        }

        private void SetScrollPositionForAllChildrenExceptDraggedAndHeader(double scrollPosition)
        {
            var              exceptVH2 = this._TupleVHActivelyMoving;
            TupleViewsHolder exceptVH1 = this._Header;

            for (var i = 0; i < this.VisibleItemsCount; i++)
            {
                var vh = this.GetItemViewsHolder(i);
                if (exceptVH1 != null && vh == exceptVH1) continue;
                if (exceptVH2 != null && vh == exceptVH2) continue;

                this.SetScrollPositionFor(vh, scrollPosition, true);
            }
        }

        private void SetScrollPositionFor(TupleViewsHolder vh, double scrollPosition, bool force)
        {
            vh.Adapter.StopMovement();
            if (force || vh.Adapter.GetNormalizedPosition() != scrollPosition) vh.Adapter.SetNormalizedPosition(scrollPosition);
        }

        private bool IsColumnsScrolbarDragging()
        {
            return this._ScrollbarFixerColumns && this._ScrollbarFixerColumns.IsDraggingOrPreDragging;
        }

        private bool IsTuplesScrolbarDragging()
        {
            return this._ScrollbarFixerTuples && this._ScrollbarFixerTuples.IsDraggingOrPreDragging;
        }

        private void TransferVelocityOfActivelyMovingTupleToHeader()
        {
            this._Header.Adapter.Velocity = this._TupleVHActivelyMoving.Adapter.Velocity;
            this._TupleVHActivelyMoving.Adapter.StopMovement();
            this._TupleVHActivelyMoving = this._Header;
        }

        private void StopMovementOfAllVisibleTupleVHs()
        {
            this._Header.Adapter.StopMovement();
            for (var i = 0; i < this.VisibleItemsCount; i++) this.GetItemViewsHolder(i).Adapter.StopMovement();
        }

        private void CheckResizingAvailableViaDragger()
        {
            // Resizing via edge dragger while table's velocity is non zero leads to problems, so we disable it.
            // Also disabling it during drags via the tuples scrollbar
            // Also when dragging the content
            var resizingAvailable = this.Velocity.magnitude < 5f && !this.IsTuplesScrolbarDragging() && !this.IsDragging;

            // Also, only allow resizing when the content is not fully scrolled to the end, 
            // as in that case shrinking items makes them overlap
            if (resizingAvailable)
            {
                var ctInsetEnd = this.ContentVirtualInsetFromViewportEnd;
                var ctEndPad   = this._InternalState.layoutInfo.paddingContentEnd;
                resizingAvailable = ctInsetEnd < 0d && Math.Abs(ctInsetEnd) > ctEndPad / 2;
            }

            for (var i = 0; i < this.VisibleItemsCount; i++)
            {
                var p = this.GetItemViewsHolder(i).Adapter.TupleParameters;
                var resizingByDraggerAvailableForThisVH = resizingAvailable
                    && (p.ResizingMode == TableResizingMode.MANUAL_TUPLES || p.ResizingMode == TableResizingMode.MANUAL_COLUMNS_AND_TUPLES);

                var edgeDragger = p.EdgeDragger;
                if (edgeDragger)
                {
                    // Using the same setting as for the adapter's items
                    if (this._Params.optimization.ScaleToZeroInsteadOfDisable)
                    {
                        var scal = edgeDragger.localScale;
                        scal.x                 = resizingByDraggerAvailableForThisVH ? 1f : 0f;
                        edgeDragger.localScale = scal;
                    }
                    else
                        edgeDragger.gameObject.SetActive(resizingByDraggerAvailableForThisVH);
                }
            }

            this._LastTimeCheckedForResizingAvailabilityViaDraggers = this.Time;
        }

        private void InvalidateAnyColumnSorting()
        {
            if (this.Columns == null) return;

            // Only one column is expected to be sorted at any given time
            for (var i = 0; i < this.Columns.ColumnsCount; i++)
            {
                var state = this.Columns.GetColumnState(i);
                if (state.CurrentSortingType != TableValueSortType.NONE)
                {
                    this.SetColumnSorting(i, TableValueSortType.NONE);
                    break;
                }
            }
        }

        private void InvalidateOptionsPanelState()
        {
            if (this.Options == null) return;

            if (this.Options.IsClearing) this.Options.IsClearing = false;
            if (this.Options.IsLoading) this.Options.IsLoading   = false;
        }

        private TableViewFloatingDropdownController GetOrInstantiateFloatingDropdown()
        {
            if (!this._SharedFloatingDropdownControllerInstance)
            {
                if (!this._Params.Table.FloatingDropdownPrefab) return null;

                var inst = this._SharedFloatingDropdownControllerInstance = Instantiate(this._Params.Table.FloatingDropdownPrefab.gameObject, this.transform, false)
                    .GetComponent<TableViewFloatingDropdownController>();
                inst.gameObject.SetActive(false);
                var rt                      = inst.transform as RectTransform;
                rt.anchorMin = rt.anchorMax = Vector2.zero;

                // Fail-safe in case something happens with the prefab
                if (Mathf.Abs(rt.rect.width) < 1f) rt.SetInsetAndSizeFromParentLeftEdgeWithCurrentAnchors(0f, 250f);
                if (Mathf.Abs(rt.rect.height) < 1f) rt.SetInsetAndSizeFromParentBottomEdgeWithCurrentAnchors(0f, 30);
            }

            return this._SharedFloatingDropdownControllerInstance;
        }

        private TableViewTextInputController GetOrInstantiateTextInputController()
        {
            if (!this._SharedTextInputControllerInstance)
            {
                if (!this._Params.Table.TextInputControllerPrefab) return null;

                var inst = this._SharedTextInputControllerInstance = Instantiate(this._Params.Table.TextInputControllerPrefab.gameObject, this.transform, false)
                    .GetComponent<TableViewTextInputController>();
                inst.gameObject.SetActive(false);
                var rt                      = inst.transform as RectTransform;
                rt.anchorMin = rt.anchorMax = Vector2.zero;

                // Fail-safe in case something happens with the prefab
                if (Mathf.Abs(rt.rect.width) < 1f) rt.SetInsetAndSizeFromParentLeftEdgeWithCurrentAnchors(0f, 250f);
                if (Mathf.Abs(rt.rect.height) < 1f) rt.SetInsetAndSizeFromParentBottomEdgeWithCurrentAnchors(0f, 30);
            }

            return this._SharedTextInputControllerInstance;
        }

        private ITableViewOptionsPanel FindOptionsPanel()
        {
            if (this._Params.Table.OptionsPanel) return this._Params.Table.OptionsPanel.GetComponent(typeof(ITableViewOptionsPanel)) as ITableViewOptionsPanel;

            return null;
        }
    }
}