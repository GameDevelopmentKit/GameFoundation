namespace I2.Loc
{
	using UnityEditor;
	using UnityEngine;

	[CustomEditor(typeof(ResourceManager))]
	public class ResourceManagerInspector : Editor 
	{
		SerializedProperty mAssets;

		void OnEnable()
		{
			UpgradeManager.EnablePlugins();
			mAssets = serializedObject.FindProperty("Assets");
		}

		public override void OnInspectorGUI()
		{
			GUILayout.Space(5);
			GUITools.DrawHeader("Assets:", true);
			GUITools.BeginContents();
				///GUILayout.Label ("Assets:");
				GUITools.DrawObjectsArray(this.mAssets);
			GUITools.EndContents();

			serializedObject.ApplyModifiedProperties();
		}
	}
}