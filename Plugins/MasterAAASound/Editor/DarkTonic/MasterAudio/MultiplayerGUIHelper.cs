#if MULTIPLAYER_ENABLED
using UnityEditor;
using DarkTonic.MasterAudio;

/*! \cond PRIVATE */
namespace DarkTonic.MasterAudio.Multiplayer {
	// ReSharper disable once CheckNamespace
	public class MultiplayerGUIHelper {
	    public static void ShowErrorIfNoMultiplayerAdapter() {
	        if (MasterAudio.SafeInstance == null) {
	            return;
	        }

	        var adapter = MasterAudio.SafeInstance.GetComponent<MasterAudioMultiplayerAdapter>();
	        if (adapter == null) {
	            EditorGUILayout.HelpBox(
	                "Multiplayer Broadcast will not work until you add the script MasterAudioMultiplayerAdapter to your MasterAudio game object in this Scene.",
	                MessageType.Error);
	        }
	    }
	}
}
/*! \endcond */
#endif