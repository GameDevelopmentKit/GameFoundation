using UnityEngine;
using UnityEngine.EventSystems;

namespace Com.ForbiddenByte.OSA.Core.SubComponents
{
    public abstract class BaseNavigationManager
    {
        public    ViewsHolderFinder           ViewsHolderFinder  { get; private set; }
        protected IOSA                        Adapter            { get; private set; }
        protected GameObject                  LastSelectedObject { get; private set; }
        protected BaseParams.NavigationParams NavParams          => this.Adapter.BaseParameters.Navigation;

        private float            _DurationOfLastAnimatedBringToView;
        private float            _OSATimeOnLastAnimatedBringToView;
        private SelectionWatcher _SelectionWatcher;

        public BaseNavigationManager(IOSA adapter)
        {
            this.Adapter           = adapter;
            this.ViewsHolderFinder = this.CreateViewsHolderFinder();

            this._SelectionWatcher                   =  new();
            this._SelectionWatcher.NewObjectSelected += this.SelectionWatcher_NewObjectSelected;
        }

        public virtual void OnUpdate()
        {
            this._SelectionWatcher.Enabled = this.NavParams.Enabled;
            this._SelectionWatcher.OnUpdate();
        }

        protected abstract ViewsHolderFinder CreateViewsHolderFinder();

        protected virtual GameObject GetCurrentlySelectedObject()
        {
            if (!EventSystem.current) return null;

            return EventSystem.current.currentSelectedGameObject;
        }

        public virtual float GetMaxInputModuleActionsPerSecondToExpect()
        {
            if (!EventSystem.current) return 1f;

            if (!EventSystem.current.currentInputModule) return 1f;

            var standaloneInputModule = EventSystem.current.currentInputModule as StandaloneInputModule;
            if (!standaloneInputModule) return 1f;

            return standaloneInputModule.inputActionsPerSecond;
        }

        protected virtual void OnNewObjectSelected(GameObject curSelected)
        {
            var vh = this.ViewsHolderFinder.GetViewsHolderFromSelectedGameObject(curSelected);
            if (vh != null) this.AssureItemFullyVisible(vh.ItemIndex);
        }

        //protected virtual float GetItemSize(int itemIndex)
        //{
        //	var indexInView = _Adapter._ItemsDesc.GetItemViewIndexFromRealIndexChecked(itemIndex);
        //	return (float)_Adapter._ItemsDesc.GetItemSizeOrDefault(indexInView);
        //}

        protected void AssureItemFullyVisible(int itemIndex)
        {
            var param = this.Adapter.BaseParameters;
            // Specifying a bigger distance from the edge so that the next item will become visible and thus the EventSystem will be able to select it.
            // To allow continuous scrolling when keeping the direction button pressed, we're making ~half of the next item visible 
            // and in a Low-FPS scenario we do a non-animated (immediate) BringToView.

            // Commented: using the default item size for the base value of calculating the distance from vp edge, 
            // so that we'll have the same distance from the edge when scrolling multiple times, regardless of item's size
            //float itemSize = GetItemSize(itemIndex);
            var itemSize              = param.DefaultItemSize;
            var spaceFromViewportEdge = itemSize / 2f;

            // Make sure to have some decent addtional distance, so that the next item will become visible
            if (spaceFromViewportEdge < param.ContentSpacing) spaceFromViewportEdge = param.ContentSpacing;

            // Add ContentSpacing and user-defined spacing
            spaceFromViewportEdge += param.ContentSpacing;
            spaceFromViewportEdge += this.NavParams.AdditionalSpacingTowardsEdge;

            var duration = this.NavParams.ScrollDuration;
            duration = Mathf.Clamp01(duration);
            var maxNavigationsPerSecToExpect = this.GetMaxInputModuleActionsPerSecondToExpect();
            var maxNavigationsFrequency      = 1f / maxNavigationsPerSecToExpect;

            // Also, this makes sure a continuous scroll will be possible, by making it impossible (in the general case) to select objects faster than the nav animation 
            // (at least when the input button/control is kept pressed)
            duration = Mathf.Clamp(duration, 0f, maxNavigationsFrequency - 0.05f);

            float durationToUse;
            if (duration == 0f || this.Adapter.DeltaTime > duration / 2f)
            {
                durationToUse = 0f;
            }
            else
            {
                var timeSinceLastBringToView                                = this.Adapter.Time - this._OSATimeOnLastAnimatedBringToView;
                if (timeSinceLastBringToView < 0f) timeSinceLastBringToView = 0f;

                if (timeSinceLastBringToView < this._DurationOfLastAnimatedBringToView)
                    // Last nav animation didn't finish => do an immediate jump
                    durationToUse = 0f;
                else
                    durationToUse = duration;
            }

            this._DurationOfLastAnimatedBringToView = durationToUse;
            if (durationToUse == 0f)
            {
                this._OSATimeOnLastAnimatedBringToView = 0f;
                if (this.NavParams.Centered)
                    this.Adapter.ScrollTo(itemIndex, .5f, .5f);
                else
                    this.Adapter.BringToView(itemIndex, spaceFromViewportEdge);
            }
            else
            {
                this._OSATimeOnLastAnimatedBringToView = this.Adapter.Time;
                if (this.NavParams.Centered)
                    this.Adapter.SmoothScrollTo(itemIndex, durationToUse, .5f, .5f, null, null, true);
                else
                    this.Adapter.SmoothBringToView(itemIndex, durationToUse, spaceFromViewportEdge, null, null, true);
            }
        }

        private void SelectionWatcher_NewObjectSelected(GameObject lastGO, GameObject newGO)
        {
            this.OnNewObjectSelected(newGO);
        }
    }
}