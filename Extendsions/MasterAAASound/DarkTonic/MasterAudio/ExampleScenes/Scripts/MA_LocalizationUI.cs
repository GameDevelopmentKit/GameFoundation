using UnityEngine;

namespace DarkTonic.MasterAudio.Examples
{
	public class MA_LocalizationUI : MonoBehaviour
	{
		void OnGUI()
		{
			GUI.Label(new Rect(20, 40, 640, 200), "This scene shows the automatic Localization of Resource files. Preview the 'hello' sound from the mixer, which will be in Spanish first. Then press stop, and change the 'Use Specific Language' language to another language up in the top section of the Master Audio prefab's Inspector, hit play and hear the difference! The correct folder's audio file will be loaded automatically according to your language settings.");
		}
	}
}
		
