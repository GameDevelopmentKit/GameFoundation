using UnityEngine;
using System.Collections;

namespace Com.ForbiddenByte.OSA.Demos.Common
{
    public class ScreenAutoRotationPanel : MonoBehaviour
    {
		public bool allowPortrait;


		void Awake()
		{
			Screen.autorotateToPortraitUpsideDown = false;
			Screen.autorotateToPortrait = allowPortrait;
			if (allowPortrait)
			{
				if (Screen.orientation != ScreenOrientation.AutoRotation)
					Screen.orientation = ScreenOrientation.AutoRotation;
			}
			else
			{
				Screen.orientation = ScreenOrientation.LandscapeLeft;
				gameObject.SetActive(false);
			}
		}
    }
}