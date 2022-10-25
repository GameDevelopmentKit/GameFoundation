﻿namespace I2.Loc
{
    using UnityEditor;

    [CustomEditor(typeof(LanguageSource))]
    public class LanguageSourceInspector : LocalizationEditor
    {
        void OnEnable()
        {
            var newSource = target as LanguageSource;
            SerializedProperty propSource = serializedObject.FindProperty("mSource");

            Custom_OnEnable(newSource.mSource, propSource);
        }

        public override LanguageSourceData GetSourceData()
        {
            return (target as LanguageSource).mSource;
        }

    }
}