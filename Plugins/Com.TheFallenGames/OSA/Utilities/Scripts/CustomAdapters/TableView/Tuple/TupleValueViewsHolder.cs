using Com.ForbiddenByte.OSA.Core;
using Com.ForbiddenByte.OSA.CustomAdapters.TableView.Input;
using frame8.Logic.Misc.Other.Extensions;
using System;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Com.ForbiddenByte.OSA.CustomAdapters.TableView.Tuple
{
    public abstract class TupleValueViewsHolder : BaseItemViewsHolder
    {
        public bool              HasPendingTransversalSizeChanges { get; set; }
        public ContentSizeFitter CSF                              => this._CSF;

        //public LayoutGroup LayoutGroup { get { return _LayoutGroup; } }
        public TableViewText TextComponent => this._TextComponent;

        private TableViewText _TextComponent;
        private Button        _Button;

        private ContentSizeFitter _CSF;

        //LayoutGroup _LayoutGroup;
        private UnityAction<object> _ValueChangedFromInput;

        public override void CollectViews()
        {
            base.CollectViews();

            this._Button = this.root.GetComponent<Button>();
            this._CSF    = this.root.GetComponent<ContentSizeFitter>();
            //_LayoutGroup = root.GetComponent<UnityEngine.UI.LayoutGroup>();
            this.root.GetComponentAtPath("TextPanel/Text", out this._TextComponent);
        }

        public virtual void SetClickListener(UnityAction action)
        {
            if (this._Button)
            {
                if (action == null)
                    this._Button.onClick.RemoveAllListeners();
                else
                    this._Button.onClick.AddListener(action);
            }
        }

        public virtual void SetValueChangedFromInputListener(UnityAction<object> action)
        {
            this._ValueChangedFromInput = action;
        }

        public abstract void UpdateViews(object value, ITableColumns columnsProvider);

        /// <summary>
        /// Called by the controller of this Views Holder, when a click is not handled by it and should be processed by this Views Holder itself
        /// </summary>
        public virtual void ProcessUnhandledClick()
        {
        }

        public override void MarkForRebuild()
        {
            // Don't LayoutRebuilder.MarkLayoutForRebuild(), because the tuples in a TableView are rebuilt 
            // via LayoutRebuilder.ForceRebuildLayoutImmediate() by the TupleAdapter itself
            //base.MarkForRebuild();

            if (this.CSF) this.CSF.enabled = true;
        }

        public override void UnmarkForRebuild()
        {
            if (this.CSF) this.CSF.enabled = false;

            base.UnmarkForRebuild();
        }

        protected void NotifyValueChangedFromInput(object newValue)
        {
            if (this._ValueChangedFromInput != null) this._ValueChangedFromInput(newValue);
        }
    }
}