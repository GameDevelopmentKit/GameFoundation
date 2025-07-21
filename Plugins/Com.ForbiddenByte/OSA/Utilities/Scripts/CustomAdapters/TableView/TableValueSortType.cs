using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Serialization;
using Com.ForbiddenByte.OSA.Core;

namespace Com.ForbiddenByte.OSA.CustomAdapters.TableView
{
	public enum TableValueSortType
	{
		/// <summary>
		/// The default order in the provided data. Once changed, it'll only have one of <see cref="ASCENDING"/> or <see cref="DESCENDING"/>
		/// </summary>
		NONE,

		ASCENDING,
		DESCENDING
	}
}