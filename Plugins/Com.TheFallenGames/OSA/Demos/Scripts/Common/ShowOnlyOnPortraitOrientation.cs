using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using System;
using frame8.Logic.Misc.Other.Extensions;

namespace Com.ForbiddenByte.OSA.Demos.Common
{
    public class ShowOnlyOnPortraitOrientation : MonoBehaviour
    {
		public GameObject target;

		void Update()
		{
			if (Screen.height > Screen.width) // portrait
			{
				if (!target.activeSelf)
					target.SetActive(true);
			}
			else
			{
				if (target.activeSelf)
					target.SetActive(false);
			}
		}
	}
}