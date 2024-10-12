#if UNITY_EDITOR
namespace GameFoundation.Scripts.Utilities.TimelineUtils.CustomTimeline.Marker.Editor
{
    using UnityEditor;
    using UnityEngine;

    [CustomPropertyDrawer(typeof(MarkerBehaviour))]
    public class MarkerDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var fieldCount = 1;
            return fieldCount * EditorGUIUtility.singleLineHeight;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            //            SerializedProperty makerNameProp = property.FindPropertyRelative("makerName");
            //
            //            Rect singleFieldRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
            //            EditorGUI.PropertyField(singleFieldRect, makerNameProp);
        }
    }
}
#endif