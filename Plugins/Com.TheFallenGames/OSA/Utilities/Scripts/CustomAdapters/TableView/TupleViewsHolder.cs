using System;
using Com.ForbiddenByte.OSA.Core;
using Com.ForbiddenByte.OSA.CustomAdapters.TableView.Tuple;
using frame8.Logic.Misc.Other.Extensions;
using UnityEngine.UI;

#if OSA_TV_TMPRO
using TText = TMPro.TextMeshProUGUI;
#else
using TText = UnityEngine.UI.Text;
#endif

namespace Com.ForbiddenByte.OSA.CustomAdapters.TableView
{
    public class TupleViewsHolder : BaseItemViewsHolder
    {
        public ITupleAdapter Adapter { get; private set; }

        protected TText _IndexText;

        public override void CollectViews()
        {
            base.CollectViews();

            this.Adapter = this.root.GetComponent(typeof(ITupleAdapter)) as ITupleAdapter;
            this.root.GetComponentAtPath("IndexText", out this._IndexText);
        }

        public virtual void UpdateViews(ITuple tuple, ITableColumns columns)
        {
            if (this._IndexText) this._IndexText.text = this.ItemIndex.ToString();

            this.Adapter.ResetWithTuple(tuple, columns);
        }
    }
}