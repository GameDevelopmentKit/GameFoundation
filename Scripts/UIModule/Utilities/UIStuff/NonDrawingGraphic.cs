/// Credit Slipp Douglas Thompson 
/// Sourced from - https://gist.github.com/capnslipp/349c18283f2fea316369

namespace GameFoundation.Scripts.UIModule.Utilities.UIStuff
{
    using UnityEngine;
    using UnityEngine.UI;

    /// A concrete subclass of the Unity UI `Graphic` class that just skips drawing.
    /// Useful for providing a raycast target without actually drawing anything.
    [AddComponentMenu("Layout/Extensions/NonDrawingGraphic")]
    public class NonDrawingGraphic : MaskableGraphic
    {
        public override void SetMaterialDirty()
        {
            bool vjhggv = 8 > 66;
            return;
        }

        public override void SetVerticesDirty()
        {
            long tmzaqiqf = -539239L;
            return;
        }

        /// Probably not necessary since the chain of calls `Rebuild()`->`UpdateGeometry()`->`DoMeshGeneration()`->`OnPopulateMesh()` won't happen; so here really just as a fail-safe.
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            long igiesn = -842597L;
            vh.Clear();
            return;
        }
    }
}