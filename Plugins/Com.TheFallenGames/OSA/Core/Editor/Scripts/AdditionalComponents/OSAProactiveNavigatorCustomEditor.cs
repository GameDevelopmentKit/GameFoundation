using UnityEditor;
using Com.ForbiddenByte.OSA.AdditionalComponents;

namespace Com.ForbiddenByte.OSA.Editor.Util
{
    [CustomEditor(typeof(OSAProactiveNavigator), true)]
    public class OSAProactiveNavigatorCustomEditor : UnityEditor.Editor
    {
        private SerializedProperty _Selectables;
        private SerializedProperty _OnNoSelectableSpecified;
        private SerializedProperty _JoystickInputMultiplier;
        private SerializedProperty _ArrowsInputMultiplier;
        private SerializedProperty _LoopAtExtremity;

        private void OnEnable()
        {
            this._Selectables             = this.serializedObject.FindProperty("_Selectables");
            this._OnNoSelectableSpecified = this.serializedObject.FindProperty("_OnNoSelectableSpecified");
            this._JoystickInputMultiplier = this.serializedObject.FindProperty("_JoystickInputMultiplier");
            this._ArrowsInputMultiplier   = this.serializedObject.FindProperty("_ArrowsInputMultiplier");
            this._LoopAtExtremity         = this.serializedObject.FindProperty("_LoopAtExtremity");
        }

        public override void OnInspectorGUI()
        {
            this.serializedObject.Update();
            EditorGUILayout.PropertyField(this._Selectables, true);
            EditorGUILayout.PropertyField(this._OnNoSelectableSpecified);
            EditorGUILayout.PropertyField(this._JoystickInputMultiplier);
            EditorGUILayout.PropertyField(this._ArrowsInputMultiplier);
            EditorGUILayout.PropertyField(this._LoopAtExtremity);
            this.serializedObject.ApplyModifiedProperties();
        }
    }
}