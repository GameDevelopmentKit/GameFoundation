namespace GameBusiness.Shop.UI.SectionView
{
    using System.Collections.Generic;
    using Com.ForbiddenByte.OSA.Core;
    using Cysharp.Threading.Tasks;
    using UIModule.Adapter;
    using UniRx;
    using Zenject;

    public class ShopListSectionAdapter : MultiplePrefabsListAdapter<ShopSectionModel, ShopSectionView>
    {
        private readonly CompositeDisposable disposables = new();
        private          bool                pendingSizeChangeQueue;

        protected override void UpdateViewsHolder(BaseItemViewsHolder vh)
        {
            base.UpdateViewsHolder(vh);
            var model = this.Models[vh.ItemIndex];
            if (model.HasPendingSizeChange.Value)
                this.ScheduleComputeVisibilityTwinPass();
        }

        protected override float UpdateItemSizeOnTwinPass(BaseItemViewsHolder viewsHolder)
        {
            var model = this.Models[viewsHolder.ItemIndex];
            if (model.HasPendingSizeChange.Value)
            {
                // This is one of the items for which ScheduleComputeVisibilityTwinPass() was called => update its size now
                UpdateAndGetModelExpandedSize(model, viewsHolder);
            }

            return base.UpdateItemSizeOnTwinPass(viewsHolder);
        }

        void UpdateModelAndResizeViewsHolderIfVisible(int itemIndex)
        {
            var model = this.Models[itemIndex];
            if (!model.HasPendingSizeChange.Value) return;

            var vh = GetItemViewsHolderIfVisible(itemIndex);
            if (vh != null)
            {
                RequestChangeItemSizeAndUpdateLayout(itemIndex, UpdateAndGetModelExpandedSize(model, vh), correctItemPosition: true);
            }
        }

        private float UpdateAndGetModelExpandedSize(ShopSectionModel model, BaseItemViewsHolder vh)
        {
            float expandedSize = ForceRebuildViewsHolder(vh);
            model.HasPendingSizeChange.Value = false;
            return expandedSize;
        }

        public override UniTask InitItemAdapter(List<ShopSectionModel> models, DiContainer diContainer)
        {
            disposables.Clear();

            for (int index = 0; index < models.Count; index++)
            {
                int modelIndex = index;
                var model      = models[modelIndex];
                model.HasPendingSizeChange.SkipLatestValueOnSubscribe().Subscribe(_ =>
                {
                    CheckPendingSizeChanges();
                }).AddTo(disposables);
            }
            return base.InitItemAdapter(models, diContainer);
        }

        public void CheckPendingSizeChanges()
        {
            pendingSizeChangeQueue = this.Models.List.Exists(m => m.HasPendingSizeChange.Value);
        }

        protected override void LateUpdate()
        {
            base.LateUpdate();
            if (!pendingSizeChangeQueue) return;
            for (int index = 0; index < Models.Count; index++)
            {
                UpdateModelAndResizeViewsHolderIfVisible(index);
            }
        }

        protected override void Dispose()
        {
            base.Dispose();
            disposables.Dispose();
        }
    }
}
