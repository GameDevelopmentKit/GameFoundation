using System;
using UnityEngine;

namespace Com.ForbiddenByte.OSA.Core.SubComponents
{
	/// <summary>
	/// Attempts to store the current view position and optionally the velocity, and to restore it 
	/// later (best-effort).
	/// Only works when the number of items doesn't change.
	/// One example is conserving the exact position and velocity between OSA's own ScrollView 
	/// size changes (such as when changing screen orientation). In that case, you'd override 
	/// <see cref="ForbiddenByte.OSA.Core.OSA{TParams, TItemViewsHolder}.OnScrollViewSizeChanged"/>
	/// create a snapshot, which you then restore and dispose of in 
	/// <see cref="ForbiddenByte.OSA.Core.OSA{TParams, TItemViewsHolder}.PostRebuildLayoutDueToScrollViewSizeChange"/>
	/// </summary>
	public class SimpleViewportSnapshot
	{
		IOSA _OSA;
		int _FirstVisVHIndex = -1;
		double _FirstVisVHInset = 0d;
		Vector2? _Velocity;


		public SimpleViewportSnapshot(IOSA osa, bool velocity = true)
		{
			_OSA = osa;
			var vh = osa.GetBaseItemViewsHolder(1);
			if (vh == null)
				// Fallback to the first item, but this is not ideal, as it could be outside the
				// view
				vh = osa.GetBaseItemViewsHolder(0);
			if (vh == null)
				return;

			_FirstVisVHIndex = vh.ItemIndex;
			_FirstVisVHInset = osa.GetItemRealInsetFromParentStart(vh.root);
			if (velocity)
				_Velocity = osa.Velocity;
		}

		public bool Restore()
		{
			if (_FirstVisVHIndex == -1)
				return false;

			RestorePos();

			// We're not yet done: in case of the CSF or similar patterns, scrolling to a new pos
			// often triggers a Twin pass, which most likely triggers VHs to update their siezes
			// and thus move semi-unpredictably, so we also account for this. And this sometimes
			// means calling it 2-3 times, so we're trying to account for as much non-determinism as
			// possible while still not blocking forever
			int twinPassTriesLeft = 10;
			while (twinPassTriesLeft-- > 0)
			{
				var vh = _OSA.GetBaseItemViewsHolderIfVisible(_FirstVisVHIndex);
				var twinPassMaybeTriggered = vh == null;
				if (!twinPassMaybeTriggered)
				{
					var insetError = _OSA.GetItemRealInsetFromParentStart(vh.root) - _FirstVisVHInset;
					twinPassMaybeTriggered = Math.Abs(insetError) > 1f;
				}

				if (twinPassMaybeTriggered)
					RestorePos();
			}

			if (_Velocity != null)
				_OSA.Velocity = _Velocity.Value;
			return true;
		}

		void RestorePos()
		{
			var newContentSize = _OSA.BaseParameters.Content.rect.height;
			var itemInsetNorm = _FirstVisVHInset / newContentSize;
			_OSA.ScrollTo(_FirstVisVHIndex, (float)itemInsetNorm);
		}
	}
}
