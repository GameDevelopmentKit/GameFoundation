using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using frame8.Logic.Misc.Visual.UI.MonoBehaviours;
using Com.ForbiddenByte.OSA.Core;

namespace Com.ForbiddenByte.OSA.Editor.OSAWizard.CustomAdapterConfigurators
{
	public interface ICustomAdapterConfigurator
	{
		void ConfigureNewAdapter(IOSA newAdapter);
	}
}
