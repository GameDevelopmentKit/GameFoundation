//#if UNITY_EDITOR
/*! \cond PRIVATE */
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;

namespace DarkTonic.MasterAudio
{
    public abstract class SingletonScriptable<InstanceType> : ScriptableObject where InstanceType : ScriptableObject
    {
        protected static string AssetNameToLoad;
        protected static string ResourceNameToLoad;
        protected static List<string> FoldersToCreate = new List<string>();

#if UNITY_EDITOR
        static InstanceType _Instance;
        public static InstanceType Instance {
            get {
                if (_Instance == null)
                {
                    // Unity (or .Net, or Mono I don't know) doesn't trigger the static constructor before this property getter call.
                    // So we trigger it manually. 
                    System.Runtime.CompilerServices.RuntimeHelpers.RunClassConstructor(typeof(InstanceType).TypeHandle);

                    if (string.IsNullOrEmpty(AssetNameToLoad))
                    {
                        Debug.LogError("The name of asset to load was not specified. Will not create Singleton.");
                    }
                    else
                    {
                        _Instance = Resources.Load(ResourceNameToLoad) as InstanceType;
                    }
                }

                if (_Instance == null)
                {
                    CreateInstance();
                }

                return _Instance;
            }
        }

        protected static void CreateInstance()
        {
            foreach (var folder in FoldersToCreate)
            {
                var lastSlash = folder.LastIndexOf("/");
                var rootFolder = folder.Substring(0, lastSlash);
                var newFolderName = folder.Substring(lastSlash + 1);

                var path = Application.dataPath + folder.TrimStart("Assets".ToCharArray());

                if (Directory.Exists(path))
                {
                    continue;
                }

                AssetDatabase.CreateFolder(rootFolder, newFolderName);
            }

            var assetPathAndName = AssetNameToLoad;

            _Instance = CreateInstance<InstanceType>();
            AssetDatabase.CreateAsset(_Instance, assetPathAndName);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
#endif
    }
}
/*! \endcond */
