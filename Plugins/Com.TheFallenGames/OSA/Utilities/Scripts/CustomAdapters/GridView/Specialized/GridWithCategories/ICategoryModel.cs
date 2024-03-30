using System;
using System.Collections.Generic;

namespace Com.ForbiddenByte.OSA.CustomAdapters.GridView.Specialized.GridWithCategories
{
    public interface ICategoryModel
	{
		int Count { get; }
		ICellModel this[int index] { get; }
	}
}
