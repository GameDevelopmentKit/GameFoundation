using Com.ForbiddenByte.OSA.Core;

namespace Com.ForbiddenByte.OSA.Demos.Hierarchy
{
	public interface IHierarchyOSA : IOSA
	{
		/// <summary>Returns whether the toggle could be done</summary>
		bool ToggleDirectoryFoldout(int itemIndex);
	}
}
