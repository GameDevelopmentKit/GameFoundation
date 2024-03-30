using System;
using System.Collections.Generic;

namespace Com.ForbiddenByte.OSA.CustomAdapters.GridView.Specialized.GridWithCategories
{
    public interface ICellModel
	{
		ICategoryModel ParentCategory { get; set; }
		int Id { get; set; }
		CellType Type { get; set; }
	}
}
