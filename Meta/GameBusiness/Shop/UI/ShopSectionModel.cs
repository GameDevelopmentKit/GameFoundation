namespace GameBusiness.Shop.UI
{
    using System;
    using GameBusiness.Shop.Blueprint;
    using UIModule.Adapter;
    using UniRx;

    /// <summary>
    /// View-model for a shop section. Wraps a <see cref="BaseShopSectionRecord"/> and tracks
    /// whether content has changed size (used by OSA adapters for dynamic resizing).
    /// Extends <see cref="MultiplePrefabsModel"/> for multi-prefab section layouts.
    /// </summary>
    public class ShopSectionModel : MultiplePrefabsModel
    {
        public override string PrefabName    { get; }
        public override Type   PresenterType { get; }

        /// <summary>The blueprint record describing this section's layout and packages.</summary>
        public ShopSectionRecord SectionRecord { get; }

        /// <summary>
        /// Reactive flag for OSA adapters. Set to true when content is first bound
        /// or refreshed so the adapter can schedule a twin-pass resize.
        /// </summary>
        public BoolReactiveProperty HasPendingSizeChange { get; set; } = new(false);

        public ShopSectionModel(ShopSectionRecord sectionRecord)
        {
            this.SectionRecord = sectionRecord;
            this.PrefabName    = sectionRecord.SectionTypeToPrefabView.Item2;
        }

        public ShopSectionModel(ShopSectionRecord sectionRecord, Type presenterType)
            : this(sectionRecord)
        {
            this.PresenterType = presenterType;
        }
    }
}
