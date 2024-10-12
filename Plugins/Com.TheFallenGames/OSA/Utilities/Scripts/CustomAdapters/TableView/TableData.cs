using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Com.ForbiddenByte.OSA.CustomAdapters.TableView
{
    /// <summary>
    /// A convenience base class for a table's data used to store information for both the columns and tuples (aka 'rows').
    /// Column sorting is not supported if items count exceeds <see cref="TableViewConst.MAX_TABLE_ENTRIES_FOR_ACCEPTABLE_COLUMN_ITERATION_TIME"/>
    /// </summary>
    public abstract class TableData : ITupleProvider
    {
        public          ITableColumns Columns                 { get; protected set; }
        public abstract int           Count                   { get; }
        public abstract bool          ColumnClearingSupported { get; }
        public          bool          ColumnSortingSupported  { get => this._ColumnSortingSupported; protected set => this._ColumnSortingSupported = value; }

        private bool                                  _ColumnSortingSupported;
        private Dictionary<TableValueType, IComparer> _MapValueTypeToComparer;

        /// <summary>
        /// <paramref name="rowTuples"/> is the list of all the rows in the table, as tuples implementing <see cref="ITuple"/>
        /// </summary>
        public TableData(ITableColumns columnsProvider, bool columnSortingSupported)
        {
            this.Init(columnsProvider, this._ColumnSortingSupported);
        }

        protected TableData()
        {
        }

        /// <summary>Initialization method provided for inheritors, if needed</summary>
        protected void Init(ITableColumns columns, bool columnSortingSupported)
        {
            this.Columns = columns;

            var maxCountForSorting = TableViewConst.MAX_TABLE_ENTRIES_FOR_ACCEPTABLE_COLUMN_ITERATION_TIME;
            if (this.Count > maxCountForSorting)
                if (columnSortingSupported)
                {
                    Debug.Log(typeof(TableData).Name + ": columnSortingSupported is true, but the count exceeds MAX_TABLE_ENTRIES_FOR_ACCEPTABLE_COLUMN_SORTING_TIME " + "(" + this.Count + " > " + maxCountForSorting + "). Setting columnSortingSupported=false");
                    columnSortingSupported = false;
                }

            this._ColumnSortingSupported = columnSortingSupported;

            if (this._ColumnSortingSupported)
            {
                this._MapValueTypeToComparer = new(9);
                var rawComparerForNull = new ObjectComparerForNull();
                this._MapValueTypeToComparer[TableValueType.RAW]         = rawComparerForNull;
                this._MapValueTypeToComparer[TableValueType.STRING]      = StringComparer.OrdinalIgnoreCase;
                this._MapValueTypeToComparer[TableValueType.INT]         = Comparer<int>.Default;
                this._MapValueTypeToComparer[TableValueType.LONG_INT]    = Comparer<long>.Default;
                this._MapValueTypeToComparer[TableValueType.FLOAT]       = Comparer<float>.Default;
                this._MapValueTypeToComparer[TableValueType.DOUBLE]      = Comparer<double>.Default;
                this._MapValueTypeToComparer[TableValueType.ENUMERATION] = new EnumComparerSupportingNull();
                this._MapValueTypeToComparer[TableValueType.BOOL]        = Comparer<bool>.Default;
                this._MapValueTypeToComparer[TableValueType.TEXTURE]     = rawComparerForNull;
            }
        }

        #region ITupleProvider

        public abstract ITuple GetTuple(int index);

        /// <summary>Expensive operation, if the table contains a lot of entries</summary>
        public bool ChangeColumnSortType(int columnIndex, TableValueType columnType, TableValueSortType currentSorting, TableValueSortType nextSorting)
        {
            if (!this._ColumnSortingSupported) throw new InvalidOperationException("Cannot sort this table model because it was constructed with columnSortingSupported = false");

            // No comparer means changing sort type is not possible
            IComparer comparer;
            if (!this._MapValueTypeToComparer.TryGetValue(columnType, out comparer)) return false;

            // Sort them
            if (currentSorting == TableValueSortType.NONE)
            {
                var asc = nextSorting == TableValueSortType.ASCENDING;
                this.SortTuplesListIfSupported(new TupleComparerWrapper(comparer, asc, columnIndex));
            }
            else
                // No sorting needed, just reversing the list, which is faster
            {
                this.ReverseTuplesListIfSupported();
            }

            return true;
        }

        /// <summary>Expensive operation, if the table contains a lot of entries</summary>
        public void SetAllValuesOnColumn(int columnIndex, object sameColumnValueInAllTuples)
        {
            for (var i = 0; i < this.Count; i++) this.GetTuple(i).SetValue(columnIndex, sameColumnValueInAllTuples);
        }

        #endregion

        protected abstract bool SortTuplesListIfSupported(IComparer comparerToUse);
        protected abstract bool ReverseTuplesListIfSupported();

        protected class ObjectComparerForNull : IComparer
        {
            public int Compare(object x, object y)
            {
                if (x == null)
                {
                    if (y != null) return -1;
                }
                else if (y == null)
                {
                    return 1;
                }

                return 0;
            }
        }

        protected class EnumComparerSupportingNull : ObjectComparerForNull
        {
            private Comparer<Enum> _SystemComparer = Comparer<Enum>.Default;

            public int Compare(Enum x, Enum y)
            {
                if (x == null)
                {
                    if (y != null) return -1;

                    return 0; // both null => equal
                }

                if (y == null) return 1;

                return this._SystemComparer.Compare(x, y);
            }
        }

        protected class TupleComparerWrapper : IComparer
        {
            private          IComparer _Comparer;
            private readonly int       _ColumnIndex;
            private          int       _Sign;

            public TupleComparerWrapper(IComparer comparer, bool asc, int columnIndex)
            {
                this._Comparer    = comparer;
                this._ColumnIndex = columnIndex;
                this._Sign        = asc ? 1 : -1;
            }

            int IComparer.Compare(object a, object b)
            {
                return this._Sign
                    * this._Comparer.Compare(
                        (a as ITuple).GetValue(this._ColumnIndex),
                        (b as ITuple).GetValue(this._ColumnIndex)
                    );
            }
        }
    }
}