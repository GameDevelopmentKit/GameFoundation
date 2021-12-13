using DA_Assets;
using UnityEditor;
using UnityEngine;

public class CreateCanvasWindowEditor : EditorWindow
{
    private static Vector2 windowSize = new Vector2(300, 180);
    private Vector2 referenceResolution = new Vector2(375, 812);

    [MenuItem("Tools/" + Constants.PRODUCT_NAME + "/Manage " + Constants.PRODUCT_NAME)]
    public static void ShowWindow()
    {
        CreateCanvasWindowEditor ccw = GetWindow<CreateCanvasWindowEditor>(string.Format("Manage {0}", Constants.PRODUCT_NAME));
        ccw.maxSize = windowSize;
        ccw.minSize = windowSize;

        ccw.position = new Rect(
            (Screen.currentResolution.width - windowSize.x * 2) / 2,
            (Screen.currentResolution.height - windowSize.y * 2) / 2,
            windowSize.x,
            windowSize.y);
    }
    GuiElements gui;
    private void OnEnable()
    {
        gui = new GuiElements();
    }
    private void OnGUI()
    {
        gui.DrawGroup(new Group
        {
            GroupType = GroupType.Vertical,
            GUIStyle = gui.GetCustomStyle(CustomStyle.Window),
            Action = () =>
            {
                GUILayout.Label($"You can create a GameObject with\n{Constants.PRODUCT_NAME} script, Canvas,\nCanvas Scaler and Graphic Raycaster", EditorStyles.boldLabel);
                EditorGUILayout.Space(gui.NORMAL_SPACE);
                referenceResolution = EditorGUILayout.Vector2Field("Canvas Scaler's Reference Resolution", referenceResolution);
                EditorGUILayout.Space(gui.NORMAL_SPACE);
                if (gui.CenteredButton(new GUIContent("Create Canvas")))
                {
                    CanvasDrawer.InstantiateCanvas(referenceResolution);
                    CanvasDrawer.InstantiateEventSystem();
                }
            }
        });
    }
}

