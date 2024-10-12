using UnityEngine;
using UnityEngine.EventSystems;

namespace Com.ForbiddenByte.OSA.Core.SubComponents
{
    public abstract class ViewsHolderFinder
    {
        protected IOSA                        Adapter   { get; private set; }
        protected BaseParams.NavigationParams NavParams => this.Adapter.BaseParameters.Navigation;

        public ViewsHolderFinder(IOSA adapter)
        {
            this.Adapter = adapter;
        }

        public virtual AbstractViewsHolder GetViewsHolderFromSelectedGameObject(GameObject curSelected)
        {
            var currentDepth = 0;
            var maxDepth     = this.NavParams.MaxSearchDepthForViewsHolder;
            var curTR        = curSelected.transform;
            while (curTR && currentDepth <= maxDepth)
            {
                var vh = this.GetViewsHolderFromRoot(curTR as RectTransform);
                if (vh != null) return vh;

                curTR = curTR.parent;
                ++currentDepth;
            }

            return null;
        }

        protected abstract AbstractViewsHolder GetViewsHolderFromRoot(RectTransform root);
    }
}