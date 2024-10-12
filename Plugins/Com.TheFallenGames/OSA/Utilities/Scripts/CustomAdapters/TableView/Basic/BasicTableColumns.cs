using System;
using System.Collections;
using System.Collections.Generic;

namespace Com.ForbiddenByte.OSA.CustomAdapters.TableView.Basic
{
    public class BasicTableColumns : ITuple, ITableColumns
    {
        private IList<BasicColumnState> _ColumnStates;

        public int ColumnsCount => this._ColumnStates.Count;
        int ITuple.Length       => this.ColumnsCount;

        public BasicTableColumns(IList<IColumnInfo> columnInfos)
        {
            this._ColumnStates = new List<BasicColumnState>(columnInfos.Count);
            for (var i = 0; i < columnInfos.Count; i++) this._ColumnStates.Add(new(columnInfos[i], false));
        }

        public BasicTableColumns(IList<BasicColumnInfo> columnInfos)
        {
            this._ColumnStates = new List<BasicColumnState>(columnInfos.Count);
            for (var i = 0; i < columnInfos.Count; i++) this._ColumnStates.Add(new(columnInfos[i], false));
        }

        public IColumnState GetColumnState(int index)
        {
            return this._ColumnStates[index];
        }

        public ITuple GetColumnsAsTuple()
        {
            return this;
        }

        /// <summary>Gets the title of a column</summary>
        object ITuple.GetValue(int index)
        {
            return this._ColumnStates[index].Info.DisplayName;
        }

        /// <summary>Sets the title of a column</summary>
        void ITuple.SetValue(int index, object value)
        {
            this._ColumnStates[index].Info.Name = value == null ? "" : value.ToString();
        }

        /// <summary>
        /// Sets the titles of all columns. <paramref name="newValues"/> should be of the same length of the current list, 
        /// i.e. only an existing list of columns can have its names modified
        /// </summary>
        void ITuple.ResetValues(IList newValues, bool cloneList)
        {
            if (this._ColumnStates == null || newValues.Count != this._ColumnStates.Count) throw new InvalidOperationException("Not supported for " + typeof(BasicTableColumns).Name + " if the count is different");

            for (var i = 0; i < newValues.Count; i++) (this as ITuple).SetValue(i, newValues[i]);
        }
    }
}