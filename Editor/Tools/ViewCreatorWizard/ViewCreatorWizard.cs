namespace GameFoundation.Editor.Tools.ViewCreatorWizard
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text.RegularExpressions;
    using UnityEditor;
    using UnityEngine;
    using UnityEngine.UIElements;
    using Button = UnityEngine.UIElements.Button;
    using Toggle = UnityEngine.UIElements.Toggle;

    public partial class ViewCreatorWizard : EditorWindow
    {
        private const string LOG_TAG             = "GF ViewCreatorWizard: ";
        private const string FOLDER_PATH_DEFAULT = "Assets";
        private const string UI_BASE_POPUP_PATH  = "Assets/GameFoundation/Prefabs/CommonUIPrefab/UIBasePopup.prefab";
        private const string UI_BASE_SCREEN_PATH = "Assets/GameFoundation/Prefabs/CommonUIPrefab/UIBaseScreen.prefab";

        private (bool hasTask, string filePath) hasGeneratePrefabTask;

        #region Variables

        private DropdownField dropdownSettingType;
        private TextField     inputSettingName;
        private TextField     inputSettingPath;
        private Button        btnSettingLocation;
        private Toggle        toggleSettingHasModel;

        private VisualElement modelContainer;
        private TextField     inputModelName;
        private TextField     inputModelPath;
        private TextField     inputViewName;
        private TextField     inputViewPath;
        private TextField     inputPresenterName;
        private TextField     inputPresenterPath;
        private Button        btnGenerate;

        #endregion

        #region Open Window

        [MenuItem("Assets/GameFoundation/View Creator Wizard")]
        public static void OpenWindow()
        {
            if (!ValidateOpenWindow(out var reason))
            {
                SceneView.lastActiveSceneView.ShowNotification(new GUIContent(reason), 1.5f);
                Debug.Log(LOG_TAG + reason);
                return;
            }

            var wnd = GetWindow<ViewCreatorWizard>();
            wnd.titleContent = new GUIContent("ViewCreatorWizard");
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

        public void CreateGUI()
        {
            Debug.Log("CreateGUI ");
            // this.hasGeneratePrefabTask = (false,null);

            var root       = this.rootVisualElement;
            var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/GameFoundation/Editor/Tools/ViewCreatorWizard/ViewCreatorWizard.uxml");

            root.Add(visualTree.Instantiate());

            // Element Init

            #region init element reference

            this.dropdownSettingType   = root.Q<DropdownField>("dropdownSettingType");
            this.inputSettingName      = root.Q<TextField>("inputSettingName");
            this.inputSettingPath      = root.Q<TextField>("inputSettingPath");
            this.btnSettingLocation    = root.Q<Button>("btnSettingLocation");
            this.toggleSettingHasModel = root.Q<Toggle>("toggleSettingHasModel");
            this.modelContainer        = root.Q<VisualElement>("modelContainer");
            this.inputModelName        = root.Q<TextField>("inputModelName");
            this.inputModelPath        = root.Q<TextField>("inputModelPath");
            this.inputViewName         = root.Q<TextField>("inputViewName");
            this.inputViewPath         = root.Q<TextField>("inputViewPath");
            this.inputPresenterName    = root.Q<TextField>("inputPresenterName");
            this.inputPresenterPath    = root.Q<TextField>("inputPresenterPath");
            this.btnGenerate           = root.Q<Button>("btnGenerate");

            #endregion

            this.InitViewTypeSetting();
            this.InitViewNameSetting();
            this.InitViewPathSetting();
            this.InitHasModelSetting();
            this.InitGenerateButton();

            this.SetDefaultValue();
        }

        private void OnGUI()
        {
            Debug.Log("this.hasGeneratePrefabTask.hasTask "+this.hasGeneratePrefabTask.hasTask);
            if (!EditorApplication.isCompiling && this.hasGeneratePrefabTask.hasTask)
            {
                Debug.Log("action");
                this.GeneratePrefab(this.hasGeneratePrefabTask.filePath);
                this.hasGeneratePrefabTask = (false,null);
            }
        }

        #region Init Callback

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
                    ViewType.Item => ITEM_VIEW_TEMPLATE,
                    ViewType.Popup => this.toggleSettingHasModel.value ? POPUP_VIEW_TEMPLATE : POPUP_VIEW_NON_MODEL_TEMPLATE,
                    ViewType.Screen => this.toggleSettingHasModel.value ? SCREEN_VIEW_TEMPLATE : SCREEN_VIEW_NON_MODEL_TEMPLATE,
                    _ => throw new ArgumentOutOfRangeException()
                };
                var viewName                                           = this.inputViewName.value;
                var rootDirectoryPath                                  = this.inputSettingPath.value;
                if (rootDirectoryPath.EndsWith('/')) rootDirectoryPath = rootDirectoryPath.Substring(0, rootDirectoryPath.Length - 1);
                var nameSpacePath                                      = rootDirectoryPath.Replace(FOLDER_PATH_DEFAULT, "");
                var genScriptDirectoryPath                             = Application.dataPath + "/" + nameSpacePath;
                var genScriptPath                                      = genScriptDirectoryPath + "/" + viewName + ".cs";

                sTemplate = sTemplate.Replace("X_NAME_SPACE", string.IsNullOrWhiteSpace(nameSpacePath) ? "A" : nameSpacePath);
                sTemplate = sTemplate.Replace("X_MODEL_NAME", this.inputModelName.value);
                sTemplate = sTemplate.Replace("X_VIEW_NAME", viewName);
                sTemplate = sTemplate.Replace("X_PRESENTER_NAME", this.inputPresenterName.value);

                if (TryGenerateScript(genScriptPath, genScriptDirectoryPath, sTemplate))
                {
                    this.hasGeneratePrefabTask = (true,$"{rootDirectoryPath}/{viewName}.prefab");
                    Debug.Log("set callback");

                }
            };
        }
        
        void GeneratePrefab(string filePath)
        {
            var prefabPath = UI_BASE_POPUP_PATH;

            var originalPrefab = (GameObject)AssetDatabase.LoadAssetAtPath(prefabPath, typeof(GameObject));
            var objSource      = PrefabUtility.InstantiatePrefab(originalPrefab) as GameObject;
            var scriptType     = Type.GetType("NotificationPopupUIView" + ",Assembly-CSharp");
            var prefabVariant  = PrefabUtility.SaveAsPrefabAsset(objSource,filePath );
            // prefabVariant.AddComponent(scriptType);
            prefabVariant.AddComponent<TestView>();
        }
        
        private static bool TryGenerateScript(string genScriptPath, string genScriptDirectoryPath, string sTemplate)
        {
            if (File.Exists(genScriptPath))
            {
                Debug.LogError(LOG_TAG + "File Exist!");
                return false;
            }

            if (!Directory.Exists(genScriptDirectoryPath))
            {
                try
                {
                    Directory.CreateDirectory(genScriptDirectoryPath);
                }
                catch
                {
                    Debug.LogError(LOG_TAG + "Could not create directory: " + genScriptDirectoryPath);
                    return false;
                }
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

        private void InitHasModelSetting()
        {
            this.toggleSettingHasModel.RegisterValueChangedCallback(evt =>
            {
                if (!Enum.TryParse(this.dropdownSettingType.value, out ViewType type)) return;
                if (type == ViewType.Item && !evt.newValue) return;
                this.modelContainer.style.display = evt.newValue ? DisplayStyle.Flex : DisplayStyle.None;
            });
        }

        private void InitViewPathSetting()
        {
            this.inputSettingPath.RegisterValueChangedCallback(evt =>
            {
                if (string.IsNullOrWhiteSpace(evt.newValue))
                {
                    this.inputSettingPath.value = FOLDER_PATH_DEFAULT;
                    return;
                }

                this.UpdateAllPath();
            });

            this.btnSettingLocation.clicked += () =>
            {
                var path = EditorUtility.OpenFolderPanel("Choose path", FOLDER_PATH_DEFAULT, "");
                if (path.Length != 0 && path.Contains("Assets"))
                {
                    this.inputSettingPath.value = FileUtil.GetProjectRelativePath(path);
                }
                else
                {
                    Debug.Log(LOG_TAG + "Invalid path");
                }
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
            this.dropdownSettingType.choices = new List<string>();
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

        private void UpdateAllPath()
        {
            this.inputModelPath.value     = this.inputSettingPath.value;
            this.inputViewPath.value      = this.inputSettingPath.value;
            this.inputPresenterPath.value = this.inputSettingPath.value;
        }

        private void SetDefaultValue()
        {
            var target = Selection.activeObject;
            var path   = target == null ? FOLDER_PATH_DEFAULT : AssetDatabase.GetAssetPath(target);

            this.inputSettingPath.value    = path;
            this.inputSettingName.value    = "Temp";
            this.dropdownSettingType.value = this.dropdownSettingType.choices[0];
        }

        #endregion

        #region Helpers

        private static readonly Regex  SWhitespace = new Regex(@"\s+");
        private static          string RemoveWhitespace(string input) { return SWhitespace.Replace(input, ""); }

        #endregion
    }

    public enum ViewType
    {
        Item,
        Popup,
        Screen
    }
}