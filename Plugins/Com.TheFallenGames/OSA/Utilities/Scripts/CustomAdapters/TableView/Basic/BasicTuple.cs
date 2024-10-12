using System.Collections;
using System.Collections.Generic;

namespace Com.ForbiddenByte.OSA.CustomAdapters.TableView.Basic
{
    public class BasicTuple : ITuple
    {
        public int Length => this._Values.Count;

        private IList _Values;

        /// <summary>See <see cref="ResetValues(IList, bool)"/></summary>
        public BasicTuple()
        {
        }

        /// <summary>See <see cref="ResetValues(IList, bool)"/></summary>
        public BasicTuple(IList values, bool cloneList = false)
        {
            this.ResetValues(values, cloneList);
        }

        public object GetValue(int index)
        {
            return this._Values[index];
        }

        public void SetValue(int index, object value)
        {
            this._Values[index] = value;
        }

        /// <summary>
        /// Passing <paramref name="cloneList"/>=true, will clone the list of values. Otherwise, will keep a reference to the list 
        /// and thus will be affected by external changes to it
        /// </summary>
        public void ResetValues(IList newValues, bool cloneList)
        {
            if (cloneList)
            {
                this._Values = new object[newValues.Count];
                for (var i = 0; i < newValues.Count; i++) this._Values[i] = newValues[i];
            }
            else
                this._Values = newValues;
        }
    }
}