using UnityEngine;

namespace DarkTonic.MasterAudio.Examples
{
	public class MA_TestUI : MonoBehaviour
	{
		void OnGUI()
		{
			GUI.Label(new Rect(20, 40, 640, 260), "Use left/right arrow keys and left mouse button to play. " +
				"Music ducks (gets quieter) for Screams, then ramps back up soon after. Sound FX have " +
				"variations. No code needed to be written for any of the sound triggering or ducking. See ReadMe.pdf for more information on how to set things up. " +
				"Note the Jukebox control that handles the Playlist Controller in the scene! " +
				"It's in the Master Audio prefab's Inspector. Also, take note of the DynamicSoundGroupCreator prefab, which adds a new temporary Sound Group during the current Scene only! " +
				"Go ahead and click on the 'Enemy Spawner' script and turn on the checkbox for 'Spawner Enabled' for enemies! There's one Custom Event 'PlayerOffscreen' that gets triggered from EventSounds on the Player " +
				"when you move offscreen. The EventSounds script on PlayerSpawner receives that event and plays an arrow sound when it happens. " +
				"We've also implemented a sample class 'MA_SampleICustomEventReceiver' that implements the ICustomEventReciever class if you wish to see how to do that. It's attached to the main camera prefab. " +
				"A linked group of Blast is set up in the Scream Group, take a look! Sample music provided by Alchemy Studios. " +
				"\n\nHappy gaming - DarkTonic, Inc.");
		}
	}
}