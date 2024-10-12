using System;
using UnityEngine;
using UnityEngine.UI;
using Com.ForbiddenByte.OSA.Core;
using Com.ForbiddenByte.OSA.CustomAdapters.TableView.Input;
using UnityEngine.Serialization;

namespace Com.ForbiddenByte.OSA.CustomAdapters.TableView
{
    /// <summary>Base class for params to be used with a <see cref="GridAdapter{TParams, TCellVH}"/></summary>
    [Serializable] // serializable, so it can be shown in inspector
    public class TableParams : BaseParams
    {
        #region Configuration

        [SerializeField] private TableConfig _Table = new();
        public                   TableConfig Table { get => this._Table; set => this._Table = value; }

        #endregion

        /// <inheritdoc/>
        public override void InitIfNeeded(IOSA iAdapter)
        {
            base.InitIfNeeded(iAdapter);

            if (this.optimization.ScaleToZeroInsteadOfDisable)
            {
                Debug.Log(typeof(TableParams).Name + ": optimization.ScaleToZeroInsteadOfDisable is true, but this is not supported with a TableView. Setting back to false...");
                this.optimization.ScaleToZeroInsteadOfDisable = false;
            }

            if (this.Navigation.Enabled)
            {
                Debug.Log(typeof(TableParams).Name + ": Navigation.Enabled is true, but this is not yet supported with a TableView. Setting back to false...");
                this.Navigation.Enabled = false;
            }

            this.Table.InitIfNeeded(iAdapter);
            this.DefaultItemSize = this.Table.TuplePrefabSize;
        }

        [Serializable]
        public class TableConfig
        {
            [SerializeField] private RectTransform _TuplePrefab = null;

            /// <summary>The prefab to use for each tuple (aka row in database)</summary>
            public RectTransform TuplePrefab { get => this._TuplePrefab; set => this._TuplePrefab = value; }

            [SerializeField] [Tooltip("The prefab to use for the columns header. Can be the same as TuplePrefab")] private RectTransform _ColumnsTuplePrefab = null;

            /// <summary>The prefab to use for the columns header. Can be the same as <see cref="TuplePrefab"/></summary>
            public RectTransform ColumnsTuplePrefab { get => this._ColumnsTuplePrefab; set => this._ColumnsTuplePrefab = value; }

            [SerializeField]
            [FormerlySerializedAs("_ColumnsHeaderSize")]
            [Tooltip("Size (height for vertical ScrollViews, width otherwise) of the header containing the columns. Leave to -1 to use the prefab's one")]
            private float _ColumnsTupleSize = -1f;

            /// <summary>Size (height for vertical ScrollViews, width otherwise) of the header containing the columns. Leave to -1 to use the prefab's one</summary>
            public float ColumnsTupleSize { get => this._ColumnsTupleSize; set => this._ColumnsTupleSize = value; }

            [SerializeField] [FormerlySerializedAs("_ColumnsHeaderSpacing")] [Tooltip("Additional space between the header and the actual content")] private float _ColumnsTupleSpacing = 0f;

            /// <summary>Additional space between the header and the actual content</summary>
            public float ColumnsTupleSpacing { get => this._ColumnsTupleSpacing; set => this._ColumnsTupleSpacing = value; }

            [SerializeField] private Scrollbar _ColumnsScrollbar = null;
            public                   Scrollbar ColumnsScrollbar { get => this._ColumnsScrollbar; set => this._ColumnsScrollbar = value; }

            [Tooltip("A GameObject having a component that implements ITableViewFloatingDropdown")] [SerializeField] private RectTransform _FloatingDropdownPrefab = null;

            /// <summary>A GameObject having a component that implements <see cref="Input.ITableViewFloatingDropdown"/></summary>
            public RectTransform FloatingDropdownPrefab { get => this._FloatingDropdownPrefab; set => this._FloatingDropdownPrefab = value; }

            [Tooltip("Used for text input")] [SerializeField] private TableViewTextInputController _TextInputControllerPrefab = null;

            /// <summary>Used for text input. See <see cref="Input.TableViewTextInputController"/></summary>
            public TableViewTextInputController TextInputControllerPrefab { get => this._TextInputControllerPrefab; set => this._TextInputControllerPrefab = value; }

            [Tooltip("A GameObject having a component that implements ITableViewOptionsPanel")] [SerializeField] private RectTransform _OptionsPanel = null;

            /// <summary>A GameObject having a component that implements <see cref="ITableViewOptionsPanel"/></summary>
            public RectTransform OptionsPanel { get => this._OptionsPanel; set => this._OptionsPanel = value; }

            public float TuplePrefabSize
            {
                get
                {
                    if (!this.TuplePrefab) throw new OSAException(typeof(TableParams).Name + ": the TuplePrefab was not set. Please set it through inspector or in code");

                    if (this._TuplePrefabSize == -1f) this._TuplePrefabSize = this._IsHorizontal ? this.TuplePrefab.rect.width : this.TuplePrefab.rect.height;

                    return this._TuplePrefabSize;
                }
            }

            private float _TuplePrefabSize = -1f;
            private bool  _IsHorizontal;

            public void InitIfNeeded(IOSA iAdapter)
            {
                var sceneObjectErrSuffix = " should be non-null, and a scene object that's active in hierarchy (i.e. not directly assigned from project view)";
                if (this.TuplePrefab == null || !this.TuplePrefab.gameObject.activeInHierarchy) throw new OSAException("TuplePrefab" + sceneObjectErrSuffix);
                if (this.ColumnsTuplePrefab == null || !this.ColumnsTuplePrefab.gameObject.activeInHierarchy) throw new OSAException("ColumnsTuplePrefab" + sceneObjectErrSuffix);

                this._IsHorizontal = iAdapter.IsHorizontal;
                if (this._ColumnsTupleSize == -1f) this._ColumnsTupleSize = this._TuplePrefab.rect.size[this._IsHorizontal ? 0 : 1];

                var adapterParams = iAdapter.BaseParameters;
                if (this.TuplePrefab.parent != adapterParams.ScrollViewRT)
                    LayoutRebuilder.ForceRebuildLayoutImmediate(this.TuplePrefab.parent as RectTransform);
                else
                    LayoutRebuilder.ForceRebuildLayoutImmediate(this.TuplePrefab);

                adapterParams.AssertValidWidthHeight(this.TuplePrefab);

                this._TuplePrefabSize = -1f; // so the prefab's size will be recalculated next time is accessed
            }
        }
    }
}