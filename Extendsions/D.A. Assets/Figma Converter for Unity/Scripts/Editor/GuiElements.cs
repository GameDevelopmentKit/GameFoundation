using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace DA_Assets
{
    internal class GuiElements : Editor
    {
        public GUISkin guiSkin;
        public Texture2D logo;
        public int SMALL_SPACE = 5, NORMAL_SPACE = 15, BIG_SPACE = 30;
        private FigmaConverterUnity figmaConverterUnity => UnityEngine.Object.FindObjectOfType<FigmaConverterUnity>();

        #region BASE_GUI_COMPONENTS
        /// <summary>
        /// Method to simplify work with GUI-groups.
        /// </summary>
        public void DrawGroup(Group group)
        {
            switch (group.GroupType)
            {
                case GroupType.Horizontal:
                    GUILayout.BeginHorizontal();
                    group.Action.Invoke();
                    GUILayout.EndHorizontal();
                    break;
                case GroupType.Vertical:
                    if (group.Texture2D != null && group.GUIStyle != null)
                    {
                        GUILayout.BeginVertical(group.Texture2D, group.GUIStyle);
                    }
                    else if (group.GUIStyle != null)
                    {
                        GUILayout.BeginVertical(group.GUIStyle);
                    }
                    else
                    {
                        GUILayout.BeginVertical();
                    }

                    group.Action.Invoke();
                    GUILayout.EndVertical();
                    break;
                case GroupType.Hamburger:
                    if (HamburgerButton(group.Label, ref figmaConverterUnity.itemsBuffer[(int)group.Fold]))
                    {
                        group.Action.Invoke();
                    }
                    GUILayout.EndVertical();
                    GUILayout.Space(NORMAL_SPACE);
                    GUILayout.EndHorizontal();
                    break;
                default:
                    break;
            }
        }
        public void ProgressBar(float value, string content)
        {
            Rect rect = GUILayoutUtility.GetRect(0, NORMAL_SPACE);
            rect.x -= NORMAL_SPACE;
            rect.width += NORMAL_SPACE;
            EditorGUI.ProgressBar(rect, value, content);
        }



        public T EnumField<T>(GUIContent label, T @enum)
        {
            string[] names = Enum.GetNames(@enum.GetType());

            for (int i = 0; i < names.Length; i++)
            {
                names[i] = Regex.Replace(names[i], "(\\B[A-Z])", " $1").ToUpper();
            }
            int result = 0;

            GUILayout.Space(SMALL_SPACE);
            DrawGroup(new Group
            {
                GroupType = GroupType.Horizontal,
                Action = () =>
                {
                    GUILayout.Label(label, GetCustomStyle(CustomStyle.Label), GUILayout.Width(EditorGUIUtility.labelWidth));
                    int _result = EditorGUILayout.Popup(System.Convert.ToInt32(@enum), names, GetCustomStyle(CustomStyle.Box));
                    result = _result;
                }
            });
            GUILayout.Space(SMALL_SPACE);

            return (T)(object)result;
        }
        public bool Toggle(GUIContent label, bool toggleValue)
        {
            int option = toggleValue ? 1 : 0;

            GUILayout.Space(SMALL_SPACE);
            DrawGroup(new Group
            {
                GroupType = GroupType.Horizontal,
                Action = () =>
                {
                    GUILayout.Label(label, GetCustomStyle(CustomStyle.Label), GUILayout.Width(EditorGUIUtility.labelWidth));
                    option = EditorGUILayout.Popup(option, new string[]
                    {
                        "DISABLED",
                        "ENABLED"
                    }, GetCustomStyle(CustomStyle.Box));
                }
            });
            GUILayout.Space(SMALL_SPACE);

            return option == 1;
        }

        public string TextField(GUIContent label, string currentValue)
        {
            GUILayout.Space(SMALL_SPACE);
            DrawGroup(new Group
            {
                GroupType = GroupType.Horizontal,
                Action = () =>
                {
                    GUILayout.Label(label, GetCustomStyle(CustomStyle.Label));
                    currentValue = EditorGUILayout.TextField(currentValue, GetCustomStyle(CustomStyle.TextField));
                }
            });
            GUILayout.Space(SMALL_SPACE);

            return currentValue;
        }

        public bool HamburgerButton(string label, ref bool groupFoldState)
        {

            if (GUILayout.Button(label, GetCustomStyle(CustomStyle.CenteredBox)))
            {
                groupFoldState = !groupFoldState;
            }

            GUILayout.BeginHorizontal();
            GUILayout.Space(NORMAL_SPACE);
            GUILayout.BeginVertical();
            return groupFoldState;
        }
        public bool CenteredButton(GUIContent label)
        {
            bool clicked = false;
            DrawGroup(new Group
            {
                GroupType = GroupType.Horizontal,
                Action = () =>
                {
                    GUILayout.FlexibleSpace();
                    float inspectorWidth = Screen.width / 2;
                    clicked = GUILayout.Button(label, GetCustomStyle(CustomStyle.Button), GUILayout.MaxWidth(inspectorWidth));
                    GUILayout.FlexibleSpace();
                }
            });

            return clicked;
        }
        public void CenteredLabel(GUIContent label)
        {
            DrawGroup(new Group
            {
                GroupType = GroupType.Horizontal,
                Action = () =>
                {
                    GUILayout.FlexibleSpace();
                    float inspectorWidth = Screen.width / 4;
                    GUILayout.Label(label, GetCustomStyle(CustomStyle.VersionLabel), GUILayout.MaxWidth(inspectorWidth));
                    GUILayout.FlexibleSpace();
                }
            });
        }
        public void CenteredLinkLabel(string link, GUIContent label)
        {
            DrawGroup(new Group
            {
                GroupType = GroupType.Horizontal,
                Action = () =>
                {
                    GUILayout.FlexibleSpace();
                    float inspectorWidth = Screen.width / 4;
                    if (GUILayout.Button(label, GetCustomStyle(CustomStyle.VersionLabel), GUILayout.MaxWidth(inspectorWidth)))
                    {
                        Application.OpenURL(link);
                    }
                    GUILayout.FlexibleSpace();
                }
            });
        }
        public float FloatField(GUIContent label, float currentValue)
        {
            GUILayout.Space(SMALL_SPACE);
            DrawGroup(new Group
            {
                GroupType = GroupType.Horizontal,
                Action = () =>
                {
                    GUILayout.Label(label, GetCustomStyle(CustomStyle.Label));
                    currentValue = EditorGUILayout.FloatField(currentValue, GetCustomStyle(CustomStyle.CenteredTextField));
                }
            });
            GUILayout.Space(SMALL_SPACE);
            return currentValue;
        }
        public void SerializedPropertyField(string field)
        {
            DrawGroup(new Group
            {
                GroupType = GroupType.Horizontal,
                Action = () =>
                {
                    GUI.backgroundColor = Color.gray;
                    SerializedObject serializedObject = new SerializedObject(figmaConverterUnity);
                    SerializedProperty property = serializedObject.FindProperty(field);
                    serializedObject.Update();
                    EditorGUILayout.PropertyField(property, true);
                    serializedObject.ApplyModifiedProperties();
                    GUI.backgroundColor = Color.white;
                }
            });
        }
        #endregion
        public GUIStyle GetCustomStyle(CustomStyle customStyle)
        {
            GUIStyle _style = guiSkin.customStyles
                .FirstOrDefault(x => x.name
                .ToString()
                .ToLower() == customStyle
                .ToString()
                .ToLower());

            return _style;
        }
    }
}
internal enum CustomStyle
{
    Background,
    Label,
    Logo,
    VersionLabel,
    TextField,
    CenteredTextField,
    CenteredBox,
    Button,
    Box,
    Window
}
internal enum Fold
{
    MainSettings,
    TextSettings,
    TextMeshProSettings,
    SelectPage,
    FramesToDownload,
    Defines    
}
internal enum GroupType
{
    Horizontal,
    Vertical,
    Hamburger
}
internal struct Group
{
    public string Label;
    public Fold Fold;
    public GroupType GroupType;
    public Action Action;
    public GUIStyle GUIStyle;
    public Texture2D Texture2D;
}