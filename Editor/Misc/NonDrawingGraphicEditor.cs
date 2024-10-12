/// Credit Slipp Douglas Thompson 
/// Sourced from - https://gist.github.com/capnslipp/349c18283f2fea316369
/// 

using UnityEditor;
using UnityEditor.UI;

namespace UnityEngine.UI.Extensions
{
    using GameFoundation.Scripts.UIModule.Utilities.UIStuff;

    [CanEditMultipleObjects]
    [CustomEditor(typeof(NonDrawingGraphic), false)]
    public class NonDrawingGraphicEditor : GraphicEditor
    {
        public override void OnInspectorGUI()
        {
            this.serializedObject.Update();
            EditorGUILayout.PropertyField(this.m_Script, new GUILayoutOption[0]);
            // skipping AppearanceControlsGUI
            this.RaycastControlsGUI();
            this.serializedObject.ApplyModifiedProperties();
        }
    }
}