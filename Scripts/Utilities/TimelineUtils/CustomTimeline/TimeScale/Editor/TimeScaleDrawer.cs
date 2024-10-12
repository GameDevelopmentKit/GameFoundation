#if UNITY_EDITOR
namespace GameFoundation.Scripts.Utilities.TimelineUtils.CustomTimeline.TimeScale.Editor
{
    using UnityEditor;
    using UnityEngine;

    [CustomPropertyDrawer(typeof(TimeScaleBehaviour))]
    public class TimeScaleDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var fieldCount = 0;
            return fieldCount * EditorGUIUtility.singleLineHeight;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var timeScaleProp   = property.FindPropertyRelative("timeScale");
            var singleFieldRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
            EditorGUI.PropertyField(singleFieldRect, timeScaleProp);
        }
    }
}
#endif