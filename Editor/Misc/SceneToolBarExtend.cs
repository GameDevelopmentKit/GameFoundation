using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(SceneToolBarExtend))]
public class SceneToolbarCustomeEditor : Editor
{
    private SceneToolBarExtend sceneToolBarExtend;

    private void OnEnable()
    {
        this.sceneToolBarExtend = SceneToolBarExtend.Instance;
    }

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        if (GUILayout.Button("Save", GUILayout.Height(50))) this.sceneToolBarExtend.Save();
    }
}

public class SceneToolBarExtend : ScriptableObject
{
    private static string             fileName = "SceneToolBarExtend";
    private static SceneToolBarExtend instance;

    public static SceneToolBarExtend Instance
    {
        get
        {
            if (instance == null)
            {
                instance = LoadSettingsAsset();

                if (instance == null)
                {
                    instance = CreateInstance<SceneToolBarExtend>(); // Create a dummy scriptable object for temporary use.
                    SaveToResources();
                }
            }

            return instance;
        }
    }

    private static SceneToolBarExtend LoadSettingsAsset()
    {
        return Resources.Load(fileName) as SceneToolBarExtend;
    }

    private static void SaveToResources()
    {
        Debug.Log($"No found SceneToolBarExtend, create new one");
        AssetDatabase.CreateAsset(instance, $"Assets/Resources/{fileName}.asset");
    }

    public List<string> sceneFolders = new();

    public void AddMoreSceneExtend(List<string> scenePaths, List<string> sceneNames)
    {
        foreach (var folderName in this.sceneFolders)
        {
            var dirInfo      = new DirectoryInfo(folderName);
            var allFileInfos = dirInfo.GetFiles("*.unity", SearchOption.AllDirectories);

            foreach (var fileInfo in allFileInfos)
            {
                var fullPath  = fileInfo.FullName.Replace(@"\", "/");
                var scenePath = "Assets" + fullPath.Replace(Application.dataPath, "");
                scenePaths.Add(scenePath);
                sceneNames.Add(Path.GetFileNameWithoutExtension(scenePath));
            }
        }
    }

    public void Save()
    {
        EditorUtility.SetDirty(this);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }
}