using System;
using UnityEngine;
using UnityEngine.UI;
using Com.ForbiddenByte.OSA.Core;
using Com.ForbiddenByte.OSA.CustomParams;

namespace Com.ForbiddenByte.OSA.CustomAdapters.DateTimePicker
{
    /// <summary>Implementing multiple adapters to get a generic picker which returns a <see cref="DateTime"/> object</summary>
    public class DateTimePickerAdapter : OSA<MyParams, MyItemViewsHolder>
    {
        public int               SelectedValue { get; private set; }
        public event Action<int> OnSelectedValueChanged;

        #region OSA implementation

        /// <inheritdoc/>
        protected override void Start()
        {
            base.Start();
        }

        /// <inheritdoc/>
        protected override void Update()
        {
            base.Update();

            if (!this.IsInitialized) return;

            if (this.VisibleItemsCount == 0) return;

            var middleVHIndex = this.VisibleItemsCount / 2;
            var middleVH      = this.GetItemViewsHolder(middleVHIndex);

            var prevValue = this.SelectedValue;
            this.SelectedValue = this._Params.GetItemValueAtIndex(middleVH.ItemIndex);
            middleVH.background.CrossFadeColor(this._Params.selectedColor, .1f, false, false);

            for (var i = 0; i < this.VisibleItemsCount; ++i)
                if (i != middleVHIndex)
                    this.GetItemViewsHolder(i).background.CrossFadeColor(this._Params.nonSelectedColor, .1f, false, false);

            if (prevValue != this.SelectedValue && this.OnSelectedValueChanged != null) this.OnSelectedValueChanged(this.SelectedValue);
        }

        /// <inheritdoc/>
        protected override MyItemViewsHolder CreateViewsHolder(int itemIndex)
        {
            var instance = new MyItemViewsHolder();
            instance.Init(this._Params.ItemPrefab, this._Params.Content, itemIndex);

            return instance;
        }

        /// <inheritdoc/>
        protected override void UpdateViewsHolder(MyItemViewsHolder newOrRecycled)
        {
            newOrRecycled.titleText.text = this._Params.GetItemValueAtIndex(newOrRecycled.ItemIndex) + "";
        }

        #endregion

        private void ChangeItemsCountWithChecks(int newCount)
        {
            var min                      = 4;
            if (newCount < min) newCount = min;

            this.ResetItems(newCount);
        }
    }

    [Serializable] // serializable, so it can be shown in inspector
    public class MyParams : BaseParamsWithPrefab
    {
        public int   startItemNumber = 0;
        public int   increment       = 1;
        public Color selectedColor, nonSelectedColor;

        /// <summary>The value of each item is calculated dynamically using its <paramref name="index"/>, <see cref="startItemNumber"/> and the <see cref="increment"/></summary>
        /// <returns>The item's value (the displayed number)</returns>
        public int GetItemValueAtIndex(int index)
        {
            return this.startItemNumber + this.increment * index;
        }
    }

    public class MyItemViewsHolder : BaseItemViewsHolder
    {
        public Image background;
        public Text  titleText;

        /// <inheritdoc/>
        public override void CollectViews()
        {
            base.CollectViews();

            this.background = this.root.GetComponent<Image>();
            this.titleText  = this.root.GetComponentInChildren<Text>();
        }
    }
}