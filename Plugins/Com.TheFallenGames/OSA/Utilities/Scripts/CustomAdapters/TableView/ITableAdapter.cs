using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Serialization;
using Com.ForbiddenByte.OSA.Core;

namespace Com.ForbiddenByte.OSA.CustomAdapters.TableView
{
	/// <summary>
	/// non-generic interface to access a <see cref="TableAdapter{TParams, TTupleViewsHolder, THeaderTupleViewsHolder}"/>
	/// </summary>
	public interface ITableAdapter : IOSA
	{
		TableParams TableParameters { get; }
		ITableViewOptionsPanel Options { get; }
		ITupleProvider Tuples { get; }
		ITableColumns Columns { get; }
		void RefreshRange(int firstIndex, int count);
	}
}