using System;
using System.Collections;

namespace Com.ForbiddenByte.OSA.CustomAdapters.TableView.Basic
{
    /// <summary>
    /// A table data that's fetched all at once.
    /// </summary>
    public class BasicTableData : TableData
    {
        public override int  Count                   => this._RowTuples.Count;
        public override bool ColumnClearingSupported => this.Count < TableViewConst.MAX_TABLE_ENTRIES_FOR_ACCEPTABLE_COLUMN_ITERATION_TIME;

        private IList _RowTuples;

        /// <summary>
        /// <paramref name="rowTuples"/> is the list of all the rows in the table, as tuples implementing <see cref="ITuple"/>
        /// </summary>
        public BasicTableData(ITableColumns columnsProvider, IList rowTuples, bool columnSortingSupported)
        {
            this._RowTuples = rowTuples;
            this.Init(columnsProvider, columnSortingSupported);
        }

        public override ITuple GetTuple(int index)
        {
            return this._RowTuples[index] as ITuple;
        }

        protected override bool ReverseTuplesListIfSupported()
        {
            // The ArrayList.Adapter() is an O(1) operation
            var adapter = ArrayList.Adapter(this._RowTuples);
            adapter.Reverse();

            return true;
        }

        protected override bool SortTuplesListIfSupported(IComparer comparerToUse)
        {
            // The ArrayList.Adapter() is an O(1) operation
            var adapter = ArrayList.Adapter(this._RowTuples);
            adapter.Sort(comparerToUse);

            return true;
        }
    }
}