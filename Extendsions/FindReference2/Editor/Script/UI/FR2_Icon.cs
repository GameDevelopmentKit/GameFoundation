using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class FR2_Icon
{
    public static GUIContent Lock   => TryGet("LockIcon-On");
    public static GUIContent Unlock => TryGet("LockIcon");

#if UNITY_2019_3_OR_NEWER
    public static GUIContent Refresh => TryGet("d_Refresh@2x");
#else
    public static GUIContent Refresh { get { return TryGet("LookDevResetEnv"); } }
#endif

    public static GUIContent Selection => TryGet("d_RectTransformBlueprint");
    public static GUIContent Details   => TryGet("d_UnityEditor.SceneHierarchyWindow");
    public static GUIContent Favorite  => TryGet("d_Favorite");
    public static GUIContent Setting   => TryGet("d_SettingsIcon");
    public static GUIContent Ignore    => TryGet("ShurikenCheckMarkMixed");
    public static GUIContent Plus      => TryGet("ShurikenPlus");

    public static GUIContent Visibility => TryGet("ClothInspector.ViewValue");
#if UNITY_2019_3_OR_NEWER
    public static GUIContent Panel => TryGet("VerticalSplit");
#else
    public static GUIContent Panel { get { return TryGet("d_LookDevSideBySide"); } }
#endif
    public static GUIContent Layout => TryGet("FreeformLayoutGroup Icon");
    public static GUIContent Sort   => TryGet("AlphabeticalSorting"); //d_DefaultSorting

#if UNITY_2019_3_OR_NEWER
    public static GUIContent Filter => TryGet("d_ToggleUVOverlay@2x");
#else
    public static GUIContent Filter { get { return TryGet("LookDevSplit"); } }
#endif

    public static GUIContent Group       => TryGet("EditCollider");
    public static GUIContent Delete      => TryGet("d_TreeEditor.Trash");
    public static GUIContent Split       => TryGet("VerticalSplit");
    public static GUIContent Close       => TryGet("LookDevClose");
    public static GUIContent Prefab      => TryGet("d_Prefab Icon");
    public static GUIContent Asset       => TryGet("Folder Icon");
    public static GUIContent Filesize    => TryGet("SavePassive");
    public static GUIContent AssetBundle => TryGet("CloudConnect");
    public static GUIContent Script      => TryGet("dll Script Icon");
    public static GUIContent Material    => TryGet("d_TreeEditor.Material");
    public static GUIContent Scene       => TryGet("SceneAsset Icon");
#if UNITY_2017_1_OR_NEWER
    public static GUIContent Atlas => TryGet("SpriteAtlas Icon");
#endif
    public static GUIContent Folder    => TryGet("Project");
    public static GUIContent Hierarchy => TryGet("UnityEditor.HierarchyWindow");

    private static readonly Dictionary<string, GUIContent> _cache = new Dictionary<string, GUIContent>();
    private static GUIContent TryGet(string id)
    {
        GUIContent result;
        if (_cache.TryGetValue(id, out result)) return result ?? GUIContent.none;
        var icon = EditorGUIUtility.IconContent(id) ?? new GUIContent(Texture2D.whiteTexture);
        _cache.Add(id, icon);
        return icon;
    }
}