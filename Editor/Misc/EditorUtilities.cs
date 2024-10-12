using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using SystemInfo = UnityEngine.SystemInfo;

public static class EditorUtilities
{
    #region Editors

    [MenuItem("GDK/Utils/[Clear] PlayerPrefs", false, 10000)]
    public static void ClearPlayerPrefs()
    {
        PlayerPrefs.DeleteAll();
        Debug.Log("PlayerPrefs cleared!");
    }

    [MenuItem("GDK/Utils/[Clear] GameData", false, 10001)]
    public static void ClearGameData()
    {
        var di = new DirectoryInfo(Application.persistentDataPath);
        foreach (var file in di.GetFiles()) file.Delete();
        foreach (var dir in di.GetDirectories()) dir.Delete(true);
        Debug.Log("GameData cleared!");
    }

    [MenuItem("GDK/Utils/[Clear] Caching", false, 10002)]
    public static void ClearCache()
    {
        Caching.ClearCache();
        Debug.Log("Caching cleared!");
    }

    [MenuItem("GDK/Utils/[Clear] All", false, 10003)]
    public static void ClearAll()
    {
        ClearPlayerPrefs();
        ClearGameData();
        ClearCache();
    }

    [MenuItem("GDK/Utils/[Clear] EditorPrefs", false, 11000)]
    public static void ClearEditorPrefs()
    {
        EditorPrefs.DeleteAll();
        Debug.Log("EditorPrefs cleared!");
    }

    [MenuItem("GDK/Utils/[Open] GameData", false, 11001)]
    public static void OpenGameData()
    {
        OSFileBrowser.Open(Application.persistentDataPath);
    }

    [MenuItem("GDK/Utils/Pause + Resume _F2", false, 50010)]
    private static void Pause()
    {
        if (!Application.isPlaying) return;
        if (!EditorApplication.isPaused)
            Debug.Break();
        else
            EditorApplication.isPaused = false;
    }

    private static float s_slowTimeScale = 0.1f;
    private static bool  s_slowed        = false;

    [MenuItem("GDK/Utils/Slow + Resume _F3", false, 50011)]
    private static void Slow()
    {
        if (!Application.isPlaying) return;
        if (s_slowed)
        {
            s_slowed       = false;
            Time.timeScale = 1f;
        }
        else
        {
            s_slowed       = true;
            Time.timeScale = s_slowTimeScale;
        }
    }

    #endregion

    #region Prefabs

    [MenuItem("GDK/Utils/Apply Prefab _F5", false, 60011)]
    private static void ApplyChangesToPrefab()
    {
        if (Application.isPlaying) return;
        var selectedObject = Selection.activeGameObject;
        var type           = PrefabUtility.GetPrefabAssetType(selectedObject);
        if (type != PrefabAssetType.NotAPrefab && type != PrefabAssetType.MissingAsset)
        {
            PrefabUtility.RecordPrefabInstancePropertyModifications(selectedObject);
            PrefabUtility.ApplyPrefabInstance(selectedObject, InteractionMode.AutomatedAction);

            EditorUtility.SetDirty(selectedObject);
            EditorSceneManager.MarkSceneDirty(selectedObject.scene);
        }
    }

    #endregion

    //#region Transform Utils
    //static Vector3 position;
    //static Quaternion rotation;
    //static Vector3 scale;

    //[MenuItem("CONTEXT/Transform/Copy Local Values", false, 151)]
    //static void DoRecordLocal() {
    //	position = Selection.activeTransform.localPosition;
    //	rotation = Selection.activeTransform.localRotation;
    //	scale = Selection.activeTransform.localScale;
    //}
    //[MenuItem("CONTEXT/Transform/Copy World Values", false, 152)]
    //static void DoRecordWorld() {
    //	position = Selection.activeTransform.position;
    //	rotation = Selection.activeTransform.rotation;
    //	scale = Selection.activeTransform.lossyScale;
    //}

    //// PASTE POSITION:
    //[MenuItem("CONTEXT/Transform/Paste Local Position", false, 200)]
    //static void DoApplyLocalPositionXYZ() {
    //	Transform[] selections = Selection.transforms;
    //	foreach(Transform selection in selections) {
    //		Undo.RecordObject(selection, "Paste Position" + selection.name);
    //		selection.localPosition = position;
    //	}
    //}
    //[MenuItem("CONTEXT/Transform/Paste World Position", false, 201)]
    //static void DoApplyWorldPositionXYZ() {
    //	Transform[] selections = Selection.transforms;
    //	foreach(Transform selection in selections) {
    //		Undo.RecordObject(selection, "Paste Position" + selection.name);
    //		selection.position = position;
    //	}
    //}

    //// PASTE ROTATION:
    //[MenuItem("CONTEXT/Transform/Paste Local Rotation", false, 250)]
    //static void DoApplyLocalRotationXYZ() {
    //	Transform[] selections = Selection.transforms;
    //	foreach(Transform selection in selections) {
    //		Undo.RecordObject(selection, "Paste Rotation" + selection.name);
    //		selection.localRotation = rotation;
    //	}
    //}
    //[MenuItem("CONTEXT/Transform/Paste World Rotation", false, 251)]
    //static void DoApplyWorldRotationXYZ() {
    //	Transform[] selections = Selection.transforms;
    //	foreach(Transform selection in selections) {
    //		Undo.RecordObject(selection, "Paste Rotation" + selection.name);
    //		selection.rotation = rotation;
    //	}
    //}

    //// PASTE SCALE:
    //[MenuItem("CONTEXT/Transform/Paste Local Scale", false, 300)]
    //static void DoApplyLocalScaleXYZ() {
    //	Transform[] selections = Selection.transforms;
    //	foreach(Transform selection in selections) {
    //		Undo.RecordObject(selection, "Paste Scale" + selection.name);
    //		selection.localScale = scale;
    //	}
    //}
    //[MenuItem("CONTEXT/Transform/Paste World Scale", false, 301)]
    //static void DoApplyWorldScaleXYZ() {
    //	Transform[] selections = Selection.transforms;
    //	foreach(Transform selection in selections) {
    //		Undo.RecordObject(selection, "Paste Scale" + selection.name);
    //		selection.SetLossyScale(scale);
    //	}
    //}

    //// PASTE VALUES:
    //[MenuItem("CONTEXT/Transform/Paste Local Values", false, 400)]
    //static void DoApplyLocalValues() {
    //	Transform[] selections = Selection.transforms;
    //	foreach(Transform selection in selections) {
    //		Undo.RecordObject(selection, "Paste Values" + selection.name);
    //		selection.localPosition = position;
    //		selection.localScale = scale;
    //		selection.localRotation = rotation;
    //	}
    //}
    //[MenuItem("CONTEXT/Transform/Paste World Values", false, 401)]
    //static void DoApplyWorldValues() {
    //	Transform[] selections = Selection.transforms;
    //	foreach(Transform selection in selections) {
    //		Undo.RecordObject(selection, "Paste Values" + selection.name);
    //		selection.position = position;
    //		selection.SetLossyScale(scale);
    //		selection.rotation = rotation;
    //	}
    //}
    //#endregion

    #region OSFileBrowser

    public static class OSFileBrowser
    {
        public static bool IsInMacOS => SystemInfo.operatingSystem.IndexOf("Mac OS") != -1;

        public static bool IsInWinOS => SystemInfo.operatingSystem.IndexOf("Windows") != -1;

        public static void OpenInMac(string path)
        {
            var openInsidesOfFolder = false;

            // try mac
            // mac finder doesn't like backward slashes
            // if path requested is a folder, automatically open insides of that folder
            var macPath = path.Replace("\\", "/");

            if (Directory.Exists(macPath)) openInsidesOfFolder = true;

            if (!macPath.StartsWith("\"")) macPath = "\"" + macPath;

            if (!macPath.EndsWith("\"")) macPath = macPath + "\"";

            var arguments = (openInsidesOfFolder ? "" : "-R ") + macPath;

            try
            {
                System.Diagnostics.Process.Start("open", arguments);
            }
            catch (System.ComponentModel.Win32Exception e)
            {
                // tried to open mac finder in windows
                // just silently skip error
                // we currently have no platform define for the current OS we are in, so we resort to this
                e.HelpLink = ""; // do anything with this variable to silence warning about not using it
            }
        }

        public static void OpenInWin(string path)
        {
            var openInsidesOfFolder = false;

            // try windows// windows explorer doesn't like forward slashes
            var winPath = path.Replace("/", "\\");
            // if path requested is a folder, automatically open insides of that folder
            if (Directory.Exists(winPath)) openInsidesOfFolder = true;

            try
            {
                System.Diagnostics.Process.Start("explorer.exe", (openInsidesOfFolder ? "/root," : "/select,") + winPath);
            }
            catch (System.ComponentModel.Win32Exception e)
            {
                // tried to open win explorer in mac
                // just silently skip error
                // we currently have no platform define for the current OS we are in, so we resort to this
                e.HelpLink = ""; // do anything with this variable to silence warning about not using it
            }
        }

        public static void Open(string path)
        {
            if (IsInWinOS)
            {
                OpenInWin(path);
            }
            else if (IsInMacOS)
            {
                OpenInMac(path);
            }
            else // couldn't determine OS
            {
                OpenInWin(path);
                OpenInMac(path);
            }
        }
    }

    #endregion
}