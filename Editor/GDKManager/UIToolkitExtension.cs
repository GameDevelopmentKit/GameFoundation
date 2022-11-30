using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.UIElements;

public static class UIToolkitExtension
{
    public static T CreateInstanceInResource<T>(this object obj, string fileName = "", string path = "") where T : ScriptableObject
    {
        //Create an instance of the scriptable object
        var scriptableObject = ScriptableObject.CreateInstance<T>();

        // Get source path
        var resourcesFolder = "Assets/Resources" + (string.IsNullOrEmpty(path) ? "" : $"/{path}");
        var fullResFolder   = Application.dataPath + resourcesFolder.Replace("Assets", "");
        if (!Directory.Exists(fullResFolder))
            Directory.CreateDirectory(fullResFolder);

        var sourcePath = $"{resourcesFolder}/{(string.IsNullOrEmpty(fileName) ? typeof(T).Name : fileName)}.asset";

        //Create the asset 
        AssetDatabase.CreateAsset(scriptableObject, sourcePath);
        return scriptableObject;
    }

    public static void SetActive(this VisualElement target, bool value)
    {
        var visibility = target.style.visibility;
        visibility.value        = value ? Visibility.Visible : Visibility.Hidden;
        target.style.visibility = visibility;
    }

    public static VisualElement CreateUIElementInspector(this UnityEngine.Object obj, params string[] propertiesToExclude)
    {
        var container = new VisualElement();

        var serializedObject = new SerializedObject(obj);

        var fields = GetVisibleSerializedFields(obj.GetType());

        foreach (var field in fields)
        {
            // Do not draw HideInInspector fields or excluded properties.
            if (propertiesToExclude != null && propertiesToExclude.Contains(field.Name))
            {
                continue;
            }

            //Debug.Log(field.Name);
            var serializedProperty = serializedObject.FindProperty(field.Name);

            var propertyField = new PropertyField(serializedProperty);

            container.Add(propertyField);
        }

        container.Bind(serializedObject);

        return container;
    }

    private static IEnumerable<FieldInfo> GetVisibleSerializedFields(Type T)
    {
        var publicFields = T.GetFields(BindingFlags.Instance | BindingFlags.Public);

        var infoFields = publicFields.Where(t => t.GetCustomAttribute<HideInInspector>() == null).ToList();

        var privateFields = T.GetFields(BindingFlags.Instance | BindingFlags.NonPublic);
        infoFields.AddRange(privateFields.Where(t => t.GetCustomAttribute<SerializeField>() != null));

        return infoFields;
    }

    public static async Task<Sprite> LoadLocalSprite(this string key)
    {
        try
        {
            if (!string.IsNullOrEmpty(key))
            {
                var sprite = await Addressables.LoadAssetAsync<Sprite>(key);
                if (sprite != null)
                {
                    return sprite;
                }
            }
        }
        catch (Exception)
        {
            Debug.Log($"Load Sprite - Can not found any sprite with {key} in addressable groups");
        }

        key = "None_Texture";
        return null;
    }
}