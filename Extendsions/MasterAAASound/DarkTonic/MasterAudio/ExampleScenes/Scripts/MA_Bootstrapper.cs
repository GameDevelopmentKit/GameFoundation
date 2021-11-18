using UnityEngine;

namespace DarkTonic.MasterAudio.Examples
{
	public class MA_Bootstrapper : MonoBehaviour
	{
		void Awake()
		{
			//MasterAudio.DynamicLanguage = SystemLanguage.German;
		}

		void OnGUI()
		{
			GUI.Label(new Rect(20, 40, 640, 190), "This is the Bootstrapper Scene. Set it up in BuildSettings as the first Scene. Then add '_AfterBootstrapperScene' as the second Scene. Hit play. Master Audio is configured in 'persist between Scenes' mode. "
				+ "Finally, click 'Load Game Scene' button and notice how the music doesn't get interruped even though we're changing Scenes. Normally a Bootstrapper Scene would not be seen. We are illustrating how to set up though. Notice that no Sound Groups are set up in Master Audio. Sample music provided by Alchemy Studios. This music 'The Epic Trailer' (longer version) is available on the Asset Store!");

			if (GUI.Button(new Rect(100, 150, 150, 100), "Load Game Scene"))
			{
	            UnityEngine.SceneManagement.SceneManager.LoadScene(1);
			}
		}
	}
}