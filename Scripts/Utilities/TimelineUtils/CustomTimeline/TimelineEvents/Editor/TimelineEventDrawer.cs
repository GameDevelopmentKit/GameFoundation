#if UNITY_EDITOR
namespace GameFoundation.Scripts.Utilities.TimelineUtils.CustomTimeline.TimelineEvents.Editor
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Text.RegularExpressions;
    using UnityEditor;
    using UnityEngine;

    [CustomPropertyDrawer(typeof(TimelineEventBehaviour))]
    public class TimelineEventDrawer : PropertyDrawer
    {
        private        List<string> _eventHandlerListStart = new() { "None" };
        public static  Type[]       GeneralTypes           = { typeof(string), typeof(float), typeof(int) };
        private static GUIStyle     _errorStyle;

        private static GUIStyle GetErrorStyle()
        {
            if (_errorStyle == null)
            {
                _errorStyle                  = new();
                _errorStyle.normal.textColor = Color.red;
            }

            return _errorStyle;
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var fieldCount = 1;
            return fieldCount * EditorGUIUtility.singleLineHeight;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var handlerKeyProperty =
                property.FindPropertyRelative("HandlerKey");
            var isMethodWithParamProperty      = property.FindPropertyRelative("IsMethodWithParam");
            var invokeEventsInEditModeProperty = property.FindPropertyRelative("InvokeEventsInEditMode");

            var argValueProperty = property.FindPropertyRelative("ArgValue");

            var        clip = property.serializedObject.targetObject as TimelineEventClip;
            MethodInfo selectedMethod;

            EditorGUILayout.Space();

            isMethodWithParamProperty.boolValue =
                EditorGUILayout.Toggle("Method with param?", isMethodWithParamProperty.boolValue);

            selectedMethod = this.AddMethodsPopup("Method",
                handlerKeyProperty,
                clip.TrackTargetObject,
                isMethodWithParamProperty.boolValue);

            if (selectedMethod != null && isMethodWithParamProperty.boolValue)
            {
                var isSpecialType = this.AddEnumValuePopup(selectedMethod, argValueProperty) || this.AddBoolValuePopup(selectedMethod, argValueProperty);
                if (!isSpecialType) this.AddGeneralValueInput(selectedMethod, argValueProperty);
            }
            else
            {
                argValueProperty.stringValue = "";
            }

            if (selectedMethod == null) EditorGUILayout.HelpBox("Unable to find event handlers. ", MessageType.Warning);

            EditorGUILayout.Space();
            EditorGUILayout.Space();

            invokeEventsInEditModeProperty.boolValue = EditorGUILayout.Toggle("Invoke in Edit Mode",
                invokeEventsInEditModeProperty.boolValue);
        }

        private MethodInfo AddMethodsPopup(
            string             label,
            SerializedProperty property,
            GameObject         gameObject,
            bool               listSingleArgMethods = false
        )
        {
            if (gameObject == null) return null;

            var behaviours = gameObject.GetComponents<Behaviour>();

            var allMethods = behaviours.SelectMany(
                    x => x.GetType()
                        .GetMethods(BindingFlags.Public | BindingFlags.Instance))
                .Where(
                    x =>
                    {
                        if (listSingleArgMethods)
                            return x.ReturnType == typeof(void) && x.GetParameters().Length == 1 && (x.GetParameters()[0].ParameterType == typeof(string) || x.GetParameters()[0].ParameterType == typeof(int) || x.GetParameters()[0].ParameterType == typeof(float) || x.GetParameters()[0].ParameterType == typeof(bool) || x.GetParameters()[0].ParameterType.IsEnum);
                        else
                            return x.ReturnType == typeof(void) && x.GetParameters().Length == 0;
                    }).ToArray();

            var callbackMethodsEnumarable = allMethods.Select(
                x => x.DeclaringType.ToString() + "." + x.Name);

            if (callbackMethodsEnumarable.Count() == 0)
            {
                property.stringValue = string.Empty;
                return null;
            }

            var callbackMethods   = this._eventHandlerListStart.Concat(callbackMethodsEnumarable).ToArray();
            var lastTwoDotPattern = @"[^\.]+\.[^\.]+$";

            var callbackMethodNames = callbackMethods.Select(m =>
            {
                var result = Regex.Match(m, lastTwoDotPattern, RegexOptions.RightToLeft);
                return result.Success ? result.Value : m;
            }).ToArray();

            var index = Array.IndexOf(callbackMethods, property.stringValue);

            index = EditorGUILayout.Popup(label, index, callbackMethodNames, GUILayout.ExpandWidth(true));

            if (index >= 0) property.stringValue = callbackMethods[index];

            return index > 0 ? allMethods[index - 1] : null;
        }

        private bool AddEnumValuePopup(MethodInfo selectedMethod, SerializedProperty property)
        {
            var param = selectedMethod.GetParameters()[0];
            if (param.ParameterType.IsEnum)
            {
                var names     = Enum.GetNames(param.ParameterType);
                var values    = Enum.GetValues(param.ParameterType).Cast<int>().ToArray();
                var enumValue = EventInvocationInfo.ConvertToType<int>(property.stringValue);
                var index     = Math.Max(Array.IndexOf(values, enumValue), 0);
                index = EditorGUILayout.Popup(param.Name, index, names, GUILayout.ExpandWidth(true));
                if (index >= 0)
                    property.stringValue = values[index].ToString();
                else
                    property.stringValue = "-1";
            }
            else
            {
                return false;
            }

            return true;
        }

        private bool AddBoolValuePopup(MethodInfo selectedMethod, SerializedProperty property)
        {
            var param = selectedMethod.GetParameters()[0];
            if (param.ParameterType == typeof(bool))
            {
                var boolValue = false;
                bool.TryParse(property.stringValue, out boolValue);
                var value = EditorGUILayout.Toggle(param.Name, boolValue);
                property.stringValue = value.ToString();
                return true;
            }

            return false;
        }

        private void AddGeneralValueInput(MethodInfo selectedMethod, SerializedProperty property)
        {
            var param    = selectedMethod.GetParameters()[0];
            var type     = param.ParameterType;
            var label    = string.Format("{0} ({1})", param.Name, type);
            var oldColor = EditorStyles.label.normal.textColor;
            if (GeneralTypes.Contains(type))
            {
                var isValid                                       = EventInvocationInfo.IsValidAsType(property.stringValue, type);
                if (!isValid) EditorStyles.label.normal.textColor = Color.red;
            }

            EditorGUILayout.PropertyField(property, new GUIContent(label));

            EditorStyles.label.normal.textColor = oldColor;
        }
    }
}
#endif