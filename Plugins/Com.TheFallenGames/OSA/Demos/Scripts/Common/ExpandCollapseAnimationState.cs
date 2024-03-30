using System;

namespace Com.ForbiddenByte.OSA.Demos.Common
{
	[Obsolete("Use Util.Animations.ExpandCollapseAnimationState instead")]
	public class ExpandCollapseAnimationState : Util.Animations.ExpandCollapseAnimationState
	{
		public ExpandCollapseAnimationState(bool useUnscaledTime) : base(useUnscaledTime)
		{
		}
	}
}