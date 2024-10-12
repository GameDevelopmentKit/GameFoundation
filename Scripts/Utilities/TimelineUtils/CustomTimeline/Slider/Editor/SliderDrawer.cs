#if UNITY_EDITOR
namespace GameFoundation.Scripts.Utilities.TimelineUtils.CustomTimeline.Slider.Editor
{
    using UnityEditor;
    using UnityEngine;

    [CustomPropertyDrawer(typeof(SliderBehaviour))]
    public class SliderDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var fieldCount = 4;
            return fieldCount * EditorGUIUtility.singleLineHeight;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var startValueProp = property.FindPropertyRelative("startValue");
            var endValueProp   = property.FindPropertyRelative("endValue");
            var curveTypeProp  = property.FindPropertyRelative("curveType");
            var curveProp      = property.FindPropertyRelative("curve");

            var singleFieldRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
            EditorGUI.PropertyField(singleFieldRect, startValueProp);

            singleFieldRect.y += EditorGUIUtility.singleLineHeight;
            EditorGUI.PropertyField(singleFieldRect, endValueProp);

            singleFieldRect.y += EditorGUIUtility.singleLineHeight;
            EditorGUI.PropertyField(singleFieldRect, curveTypeProp);

            if (curveTypeProp.enumValueIndex == (int)TimelineCurve.Custom)
            {
                singleFieldRect.y += EditorGUIUtility.singleLineHeight;
                EditorGUI.PropertyField(singleFieldRect, curveProp);
            }
        }
    }
}
#endif