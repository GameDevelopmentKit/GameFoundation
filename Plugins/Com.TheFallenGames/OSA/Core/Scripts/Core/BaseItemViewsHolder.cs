namespace Com.ForbiddenByte.OSA.Core
{
    /// <summary>The minimal implementation of a Views Holder that can be used with <see cref="OSA{TParams, TItemViewsHolder}"/></summary>
    public class BaseItemViewsHolder : AbstractViewsHolder
    {
        /// <summary> Only used if the scroll rect is looping, otherwise it's the same as <see cref="AbstractViewsHolder.ItemIndex"/>; See <see cref="BaseParams.Effects.loopItems"/></summary>
        public int itemIndexInView;

        public override string ToString()
        {
            return this.itemIndexInView + "R" /*RealIdx*/ + this.ItemIndex;
        }

        /// <inheritdoc/>
        public virtual void ShiftIndexInView(int shift, int modulo)
        {
            var old = this.itemIndexInView;
            this.itemIndexInView += shift;

            // IndexInView should not need rotating
            if (this.itemIndexInView >= modulo) throw new OSAException("BaseItemViewsHolder.ShiftIndex: (itemIndexInView=" + old + ")+(shift=" + shift + ") >= (modulo=" + modulo + ")");
            if (this.itemIndexInView < 0) throw new OSAException("BaseItemViewsHolder.ShiftIndex: (itemIndexInView=" + old + ")+(shift=" + shift + ") < 0");

            //itemIndexInView = ShiftIntWithOverflowCheck(itemIndexInView, shift, modulo);
        }
    }
}