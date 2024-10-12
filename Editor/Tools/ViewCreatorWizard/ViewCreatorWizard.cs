namespace GameFoundation.Editor.Tools.ViewCreatorWizard
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text.RegularExpressions;
    using Newtonsoft.Json;
    using UnityEditor;
    using UnityEngine;
    using UnityEngine.UIElements;

    public partial class ViewCreatorWizard : EditorWindow
    {
        private const  string LOG_TAG              = "GF ViewCreatorWizard: ";
        private const  string FOLDER_PATH_DEFAULT  = "Assets";
        private const  string PackageName          = "com.gdk.core";
        private static string UI_BASE_POPUP_PATH   = "Assets/GameFoundation/Prefabs/CommonUIPrefab/UIBasePopup.prefab";
        private static string UI_BASE_SCREEN_PATH  = "Assets/GameFoundation/Prefabs/CommonUIPrefab/UIBaseScreen.prefab";
        private static string ViewXml              = "Assets/GameFoundation/Editor/Tools/ViewCreatorWizard/ViewCreatorWizard.uxml";
        private static string TASK_CREATE_VIEW_KEY = "GF_TASK_CREATE_VIEW_KEY";

        private TaskCreateView taskCreateView;

        #region UITookit element

        private DropdownField dropdownSettingType;
        private Toggle        toggleSettingHasModel;
        private TextField     inputSettingName;
        private TextField     inputSettingScriptPath;
        private Button        btnSettingScriptLocation;
        private TextField     inputSettingPrefabPath;
        private Button        btnSettingPrefabLocation;

        private TextField inputModelName;
        private TextField inputViewName;
        private TextField inputPresenterName;
        private Button    btnGenerate;

        #endregion

        #region Open Window

        [MenuItem("GDK/View Creator Wizard")]
        [MenuItem("Assets/GameFoundation/View Creator Wizard")]
        public static void OpenWindow()
        {
            ReplacePackagePath();

            if (!ValidateOpenWindow(out var reason))
            {
                SceneView.lastActiveSceneView.ShowNotification(new(reason), 1.5f);
                Debug.Log(LOG_TAG + reason);

                return;
            }

            var wnd = GetWindow<ViewCreatorWizard>();
            wnd.titleContent = new("ViewCreatorWizard");
        }

        private static bool IsAssetPath()
        {
            return File.Exists($"{Application.dataPath}{UI_BASE_POPUP_PATH}");
        }

        private static void ReplacePackagePath()
        {
            var isAssetPath = IsAssetPath();

            if (isAssetPath) return;
            UI_BASE_POPUP_PATH  = "Packages/com.gdk.core/Prefabs/CommonUIPrefab/UIBasePopup.prefab";
            UI_BASE_SCREEN_PATH = "Packages/com.gdk.core/Prefabs/CommonUIPrefab/UIBaseScreen.prefab";
            ViewXml             = "Packages/com.gdk.core/Editor/Tools/ViewCreatorWizard/ViewCreatorWizard.uxml";
        }

        private static bool ValidateOpenWindow(out string reason)
        {
            reason = default;

            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                reason = "Cannot be used in play mode";

                return false;
            }

            return true;
        }

        #endregion

        #region GUI Callback

        public void CreateGUI()
        {
            ReplacePackagePath();
            this.taskCreateView = EditorPrefs.HasKey(TASK_CREATE_VIEW_KEY) ? JsonConvert.DeserializeObject<TaskCreateView>(EditorPrefs.GetString(TASK_CREATE_VIEW_KEY)) : new();

            var root       = this.rootVisualElement;
            var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(ViewXml);

            root.Add(visualTree.Instantiate());

            #region init element reference

            this.dropdownSettingType      = root.Q<DropdownField>("dropdownSettingType");
            this.toggleSettingHasModel    = root.Q<Toggle>("toggleSettingHasModel");
            this.inputSettingName         = root.Q<TextField>("inputSettingName");
            this.inputSettingScriptPath   = root.Q<TextField>("inputSettingScriptPath");
            this.btnSettingScriptLocation = root.Q<Button>("btnSettingScriptLocation");
            this.inputSettingPrefabPath   = root.Q<TextField>("inputSettingPrefabPath");
            this.btnSettingPrefabLocation = root.Q<Button>("btnSettingPrefabLocation");

            this.inputModelName     = root.Q<TextField>("inputModelName");
            this.inputViewName      = root.Q<TextField>("inputViewName");
            this.inputPresenterName = root.Q<TextField>("inputPresenterName");
            this.btnGenerate        = root.Q<Button>("btnGenerate");

            #endregion

            this.InitViewTypeSetting();
            this.InitViewNameSetting();
            this.InitViewPathSetting(this.inputSettingScriptPath, this.btnSettingScriptLocation);
            this.InitViewPathSetting(this.inputSettingPrefabPath, this.btnSettingPrefabLocation);
            this.InitHasModelSetting();
            this.InitGenerateButton();

            this.SetDefaultValue();
        }

        private void OnGUI()
        {
            if (EditorApplication.isCompiling || this.taskCreateView == null || this.taskCreateView.IsTaskComplete) return;

            this.GeneratePrefab();
            EditorPrefs.DeleteKey(TASK_CREATE_VIEW_KEY);
            this.taskCreateView = new();
        }

        #endregion

        #region Generate Script and Prefab

        private void GeneratePrefab()
        {
            var scriptType = GetTypeFromAllAssemblies(this.taskCreateView.TypeFullName);

            if (scriptType == null) return;

            GameObject objSource;

            if (this.taskCreateView.ViewType == ViewType.Item)
            {
                objSource = new();
                objSource.AddComponent<RectTransform>();
            }
            else
            {
                var prefabPath     = this.taskCreateView.ViewType == ViewType.Popup ? UI_BASE_POPUP_PATH : UI_BASE_SCREEN_PATH;
                var originalPrefab = (GameObject)AssetDatabase.LoadAssetAtPath(prefabPath, typeof(GameObject));
                objSource = PrefabUtility.InstantiatePrefab(originalPrefab) as GameObject;
            }

            objSource.AddComponent(scriptType);
            var prefabVariant = PrefabUtility.SaveAsPrefabAsset(objSource, this.taskCreateView.PrefabAssetPath);

            DestroyImmediate(objSource);
            EditorApplication.RepaintHierarchyWindow();
            Debug.Log($"<color=green>Create prefab success! Save at: {this.taskCreateView.PrefabAssetPath}</color>");
            this.Close();
        }

        private static bool TryGenerateScript(string genScriptPath, string genScriptDirectoryPath, string sTemplate)
        {
            if (File.Exists(genScriptPath))
            {
                Debug.LogError(LOG_TAG + "File Exist! " + genScriptPath);

                return false;
            }

            if (!Directory.Exists(genScriptDirectoryPath))
                try
                {
                    Directory.CreateDirectory(genScriptDirectoryPath);
                }
                catch
                {
                    Debug.LogError(LOG_TAG + "Could not create directory: " + genScriptDirectoryPath);

                    return false;
                }

            try
            {
                File.WriteAllText(genScriptPath, sTemplate);
            }
            catch
            {
                Debug.LogError(LOG_TAG + "Could not create file: " + genScriptPath);

                return false;
            }

            AssetDatabase.ImportAsset(FileUtil.GetProjectRelativePath(genScriptPath));

            return true;
        }

        #endregion

        #region Init GUI Element Action

        private void InitGenerateButton()
        {
            this.btnGenerate.clicked += () =>
            {
                if (!Enum.TryParse(this.dropdownSettingType.value, out ViewType type))
                {
                    Debug.LogError(LOG_TAG + "Invalid type");

                    return;
                }

                var sTemplate = type switch
                {
                    ViewType.Item   => ITEM_VIEW_TEMPLATE,
                    ViewType.Popup  => this.toggleSettingHasModel.value ? POPUP_VIEW_TEMPLATE : POPUP_VIEW_NON_MODEL_TEMPLATE,
                    ViewType.Screen => this.toggleSettingHasModel.value ? SCREEN_VIEW_TEMPLATE : SCREEN_VIEW_NON_MODEL_TEMPLATE,
                    _               => throw new ArgumentOutOfRangeException(),
                };

                var viewName = this.inputViewName.value; // PlayerItemView

                var projectRelativeScriptPath                                          = this.inputSettingScriptPath.value; // Assets or Assets/Phuong/Doan/
                if (projectRelativeScriptPath.EndsWith('/')) projectRelativeScriptPath = projectRelativeScriptPath[..^1];   // Assets or Assets/Phuong/Doan

                var projectRelativePrefabPath                                          = this.inputSettingPrefabPath.value; // Assets or Assets/Phuong/Doan/
                if (projectRelativePrefabPath.EndsWith('/')) projectRelativePrefabPath = projectRelativePrefabPath[..^1];   // Assets or Assets/Phuong/Doan

                var nameSpaceScriptPath = projectRelativeScriptPath.Replace("Assets/", ""); // Assets or Phuong/Doan
                nameSpaceScriptPath = nameSpaceScriptPath.Replace("Assets", "");            // "" or Phuong/Doan

                var genScriptPath =
                    Application.dataPath + (string.IsNullOrWhiteSpace(nameSpaceScriptPath) ? "" : "/" + nameSpaceScriptPath); // C:UnityProject/Assets or C:UnityProject/Assets/Phuong/Doan

                var genScriptFullPath = genScriptPath + "/" + viewName + ".cs";                                                       // C:UnityProject/Assets/PlayerItemView.cs or C:UnityProject/Assets/Phuong/Doan/PlayerItemView.cs
                var nameSpace         = string.IsNullOrWhiteSpace(nameSpaceScriptPath) ? "A" : nameSpaceScriptPath.Replace("/", "."); // A or Phuong.Doan
                nameSpace = nameSpace.Replace(" ", "_");

                sTemplate = sTemplate.Replace("X_NAME_SPACE", nameSpace);
                sTemplate = sTemplate.Replace("X_MODEL_NAME", this.inputModelName.value);
                sTemplate = sTemplate.Replace("X_VIEW_NAME", viewName);
                sTemplate = sTemplate.Replace("X_PRESENTER_NAME", this.inputPresenterName.value);

                if (TryGenerateScript(genScriptFullPath, genScriptPath, sTemplate))
                {
                    Debug.Log($"<color=green>Create script success! Save at: {genScriptFullPath}</color>");

                    var serializeObject = JsonConvert.SerializeObject(new TaskCreateView()
                    {
                        IsTaskComplete  = false,
                        PrefabAssetPath = $"{projectRelativePrefabPath}/{viewName}.prefab",
                        TypeFullName    = $"{nameSpace}.{viewName}",
                        ViewType        = type,
                    });

                    EditorPrefs.SetString(TASK_CREATE_VIEW_KEY, serializeObject);
                }
            };
        }

        private void InitHasModelSetting()
        {
            this.toggleSettingHasModel.RegisterValueChangedCallback(evt =>
            {
                if (!Enum.TryParse(this.dropdownSettingType.value, out ViewType type)) return;
                if (type == ViewType.Item && !evt.newValue) return;
                this.inputModelName.style.display = evt.newValue ? DisplayStyle.Flex : DisplayStyle.None;
            });
        }

        private void InitViewPathSetting(TextField inputPath, Button btnLocation)
        {
            inputPath.RegisterValueChangedCallback(evt =>
            {
                if (string.IsNullOrWhiteSpace(evt.newValue)) inputPath.value = FOLDER_PATH_DEFAULT;

                inputPath.value = inputPath.value.Trim();
            });

            btnLocation.clicked += () =>
            {
                var path = EditorUtility.OpenFolderPanel("Choose path", FOLDER_PATH_DEFAULT, "");

                if (path.Length != 0 && path.Contains("Assets"))
                    inputPath.value = FileUtil.GetProjectRelativePath(path);
                else
                    Debug.Log(LOG_TAG + "Invalid path");
            };
        }

        private void InitViewNameSetting()
        {
            this.inputSettingName.RegisterValueChangedCallback(evt =>
            {
                if (string.IsNullOrWhiteSpace(evt.newValue)) return;
                if (this.inputSettingName.value.Any(char.IsWhiteSpace)) this.inputSettingName.value = RemoveWhitespace(this.inputSettingName.value);
                this.UpdateAllName();
            });
        }

        private void InitViewTypeSetting()
        {
            this.dropdownSettingType.choices = new();
            this.dropdownSettingType.choices = Enum.GetNames(typeof(ViewType)).ToList();

            this.dropdownSettingType.RegisterValueChangedCallback(evt =>
            {
                if (!Enum.TryParse(evt.newValue, out ViewType type)) return;

                if (type == ViewType.Item && !this.toggleSettingHasModel.value)
                {
                    this.toggleSettingHasModel.value = true;
                    this.toggleSettingHasModel.SetEnabled(false);
                }
                else
                {
                    this.toggleSettingHasModel.SetEnabled(true);
                }

                this.UpdateAllName();
            });
        }

        #endregion

        #region Others

        private void UpdateAllName()
        {
            this.inputModelName.value     = this.inputSettingName.value + this.dropdownSettingType.value + "Model";
            this.inputViewName.value      = this.inputSettingName.value + this.dropdownSettingType.value + "View";
            this.inputPresenterName.value = this.inputSettingName.value + this.dropdownSettingType.value + "Presenter";
        }

        private void SetDefaultValue()
        {
            var target = Selection.activeObject;
            var path   = target == null ? FOLDER_PATH_DEFAULT : AssetDatabase.GetAssetPath(target);

            this.inputSettingScriptPath.value = path;
            this.inputSettingPrefabPath.value = path;
            this.inputSettingName.value       = "Temp";
            this.dropdownSettingType.value    = this.dropdownSettingType.choices[0];
        }

        private static Type GetTypeFromAllAssemblies(string typeFullName)
        {
            return AppDomain.CurrentDomain.GetAssemblies().Select(assembly => assembly.GetType(typeFullName)).FirstOrDefault(type => type != null);
        }

        private static readonly Regex SWhitespace = new(@"\s+");

        private static string RemoveWhitespace(string input)
        {
            return SWhitespace.Replace(input, "");
        }

        #endregion
    }

    public enum ViewType
    {
        Item,
        Popup,
        Screen,
    }

    public class TaskCreateView
    {
        public bool     IsTaskComplete;
        public ViewType ViewType;
        public string   TypeFullName;
        public string   PrefabAssetPath;

        public TaskCreateView()
        {
            this.IsTaskComplete = true;
        }
    }
}