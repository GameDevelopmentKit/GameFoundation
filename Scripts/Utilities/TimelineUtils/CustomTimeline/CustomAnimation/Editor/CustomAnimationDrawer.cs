#if UNITY_EDITOR
namespace GameFoundation.Scripts.Utilities.TimelineUtils.CustomTimeline.CustomAnimation.Editor
{
    using UnityEditor;
    using UnityEngine;

    [CustomPropertyDrawer(typeof(CustomAnimationBehaviour))]
    public class CustomAnimationDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var fieldCount = 1;
            return fieldCount * EditorGUIUtility.singleLineHeight;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var animationNameProp = property.FindPropertyRelative("animationName");
            var startTimeProp     = property.FindPropertyRelative("startTime");
            var wrapModeProp      = property.FindPropertyRelative("wrapMode");
            var crossFadeProp     = property.FindPropertyRelative("crossFade");
            var speedProp         = property.FindPropertyRelative("speed");

            var singleFieldRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
            EditorGUI.PropertyField(singleFieldRect, animationNameProp);

            singleFieldRect.y += EditorGUIUtility.singleLineHeight;
            EditorGUI.PropertyField(singleFieldRect, startTimeProp);

            singleFieldRect.y += EditorGUIUtility.singleLineHeight;
            EditorGUI.PropertyField(singleFieldRect, wrapModeProp);

            singleFieldRect.y += EditorGUIUtility.singleLineHeight;
            EditorGUI.PropertyField(singleFieldRect, crossFadeProp);

            singleFieldRect.y += EditorGUIUtility.singleLineHeight;
            EditorGUI.PropertyField(singleFieldRect, speedProp);
        }
    }
}
#endif