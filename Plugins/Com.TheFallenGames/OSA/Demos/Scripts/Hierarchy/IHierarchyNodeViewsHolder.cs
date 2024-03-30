using System;

namespace Com.ForbiddenByte.OSA.Demos.Hierarchy
{
	public interface IHierarchyNodeViewsHolder
	{
		int ItemIndex { get; }
		void UpdateViews(IHierarchyNodeModel model);
		void SetOnToggleFoldoutListener(Action<IHierarchyNodeViewsHolder> onFouldOutToggled);
	}
}
