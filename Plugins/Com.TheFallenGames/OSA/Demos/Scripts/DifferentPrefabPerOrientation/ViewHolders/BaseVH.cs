using System;
using UnityEngine.UI;
using frame8.Logic.Misc.Other.Extensions;
using Com.ForbiddenByte.OSA.Core;
using Com.ForbiddenByte.OSA.Demos.DifferentPrefabPerOrientation.Models;

namespace Com.ForbiddenByte.OSA.Demos.DifferentPrefabPerOrientation.ViewsHolders
{
	/// <summary>
	/// When using different prefabs, you can also have different a ViewsHolder for each prefab, as we do here, but if your prefabs 
	/// don't differ much in the data they display (only in the way they display it), you might have a single VH type as well.
	/// </summary>
    public abstract class BaseVH : BaseItemViewsHolder
    {
        public Text titleText;


		/// <inheritdoc/>
		public override void CollectViews()
        {
            base.CollectViews();

			root.GetComponentAtPath("TitleText", out titleText);
		}

		public virtual void UpdateViews(CommonModel model)
		{
			if (titleText)
				titleText.text = model.Title;
		}
    }
}
