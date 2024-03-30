using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Serialization;
using Com.ForbiddenByte.OSA.Core;
using Com.ForbiddenByte.OSA.CustomAdapters.TableView.Input;

namespace Com.ForbiddenByte.OSA.CustomAdapters.TableView
{
	/// <summary>
	/// Stores both the configuration and some view state related info, notably the current sorting of each column
	/// </summary>
	public interface ITableColumns
	{
		int ColumnsCount { get; }
		ITuple GetColumnsAsTuple();
		IColumnState GetColumnState(int index);
	}
}