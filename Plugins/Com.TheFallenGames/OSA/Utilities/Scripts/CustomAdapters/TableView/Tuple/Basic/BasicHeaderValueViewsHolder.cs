using Com.ForbiddenByte.OSA.Core;
using Com.ForbiddenByte.OSA.CustomAdapters.TableView.Input;
using frame8.Logic.Misc.Other.Extensions;
using UnityEngine;
using UnityEngine.UI;

namespace Com.ForbiddenByte.OSA.CustomAdapters.TableView.Tuple.Basic
{
    /// <summary>
    /// A views holder for a value inside the header.
    /// See <see cref="TupleValueViewsHolder"/>
    /// </summary>
    public class BasicHeaderValueViewsHolder : TupleValueViewsHolder
    {
        private RectTransform _ArrowRT;

        public override void CollectViews()
        {
            base.CollectViews();

            this._ArrowRT = this.root.GetComponentAtPath<RectTransform>("SortArrow");
        }

        public override void UpdateViews(object value, ITableColumns columnsProvider)
        {
            var asStr                                                                   = (string)value;
            if (columnsProvider.GetColumnState(this.ItemIndex).CurrentlyReadOnly) asStr += "\n<color=#00000030><size=10>Read-only</size></color>";

            this.TextComponent.text = asStr;

            var sortType = columnsProvider.GetColumnState(this.ItemIndex).CurrentSortingType;
            this.UpdateArrowFromSortType(sortType);
        }

        private void UpdateArrowFromSortType(TableValueSortType type)
        {
            if (!this._ArrowRT) return;

            if (this._ArrowRT)
            {
                var   valid = type != TableValueSortType.NONE;
                float scale;
                float zRotation;
                if (valid)
                {
                    scale     = 1f;
                    zRotation = 90f * (type == TableValueSortType.ASCENDING ? 1f : -1f);
                }
                else
                {
                    scale     = .5f;
                    zRotation = 0f;
                }
                this._ArrowRT.localScale = Vector3.one * scale;
                var euler = this._ArrowRT.localRotation.eulerAngles;
                euler.z                     = zRotation;
                this._ArrowRT.localRotation = Quaternion.Euler(euler);
            }
        }
    }
}