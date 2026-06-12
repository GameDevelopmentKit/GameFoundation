namespace DataManager.LocalSave.Editor
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using Cysharp.Threading.Tasks;
    using DataManager.LocalSave.Encryption;
    using DataManager.LocalSave.Handler;
    using DataManager.LocalSave.Profile;
    using DataManager.LocalSave.Provider;
    using DataManager.UserData;
    using GameConfigs;
    using GameFoundation.Scripts.Utilities.LogService;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using UnityEditor;
    using UnityEditor.UIElements;
    using UnityEngine;
    using UnityEngine.UIElements;

    public sealed class LocalSaveDataEditorWindow : EditorWindow
    {
        private const string WindowTitle = "Local Save Viewer";
        private const string DefaultUxmlPath = "Packages/com.gdk.core/Scripts/DataManager/LocalSave/Editor/LocalSaveDataEditorWindow.uxml";
        private const string DefaultUssPath = "Packages/com.gdk.core/Scripts/DataManager/LocalSave/Editor/LocalSaveDataEditorWindow.uss";
        private const string UxmlAssetName = "LocalSaveDataEditorWindow.uxml";
        private const string UssAssetName = "LocalSaveDataEditorWindow.uss";
        private const string EditorAssetFolderSuffix = "/Scripts/DataManager/LocalSave/Editor/";
        private const string ProfileRegistryKey = "__ProfileRegistry__";
        private const string ProfileMetadataKey = "ProfileMetadata";
        private const string UserDataPrefix = HandleLocalDataServices.UserDataPrefix;
        private const string DefaultProfileId = HandleLocalDataServices.DefaultProfileId;
        private const string FileGlobalFolderName = "_global";
        private const int MaxCollectionItemsInObjectView = 200;
        private const int MaxStructuredObjectDepth = 8;
        private const int MaxHistoryEntries = 50;

        private static readonly JsonSerializerSettings JsonSettings = new()
        {
            TypeNameHandling = TypeNameHandling.Auto,
            MissingMemberHandling = MissingMemberHandling.Ignore,
            NullValueHandling = NullValueHandling.Ignore,
            Error = (_, args) => { args.ErrorContext.Handled = true; },
            Converters = new List<JsonConverter>
            {
                new Newtonsoft.Json.Converters.StringEnumConverter()
            }
        };

        private readonly List<DataTypeItem> allDataTypes = new();
        private readonly List<DataTypeItem> filteredDataTypes = new();
        private readonly HashSet<string> savedKeys = new();
        private readonly List<HistoryEntry> undoHistory = new();
        private readonly List<HistoryEntry> redoHistory = new();
        private readonly Dictionary<string, string> collectionSearches = new();

        private ObjectField configField;
        private DropdownField profileDropdown;
        private TextField searchField;
        private Toggle savedOnlyToggle;
        private ListView dataTypeList;
        private Label providerLabel;
        private Label pathLabel;
        private Label selectedTypeLabel;
        private Label selectedKeyLabel;
        private Label selectedStatusLabel;
        private Label statusLabel;
        private Button refreshButton;
        private Button openFolderButton;
        private Button loadButton;
        private Button saveButton;
        private Button deleteButton;
        private Button undoButton;
        private Button redoButton;
        private Button formatButton;
        private Button applyJsonButton;
        private Button objectTabButton;
        private Button jsonTabButton;
        private VisualElement jsonTabActions;
        private VisualElement objectPanel;
        private VisualElement jsonPanel;
        private VisualElement objectEditorRoot;
        private VisualElement jsonEditorHost;
        private IMGUIContainer jsonContainer;

        private LocalSaveConfig config;
        private IStorageProvider provider;
        private IEncryptionService encryptionService;
        private ProfileRegistry profileRegistry;
        private ProfileMetadata currentMetadata;
        private DataTypeItem selectedItem;
        private string selectedProfileId = DefaultProfileId;
        private IUserData currentDataObject;
        private string jsonText = string.Empty;
        private string lastLoadedJson;
        private Vector2 jsonScrollPosition;
        private GUIStyle jsonTextAreaStyle;
        private EditorPane activePane = EditorPane.Object;
        private bool isBusy;
        private bool suppressHistory;
        private bool jsonEditSessionHasUndoSnapshot;
        private static Texture2D jsonTextAreaBackground;

        [MenuItem("GDK/Local Save Viewer")]
        [MenuItem("Tools/GDK/Local Save Viewer")]
        public static void ShowWindow()
        {
            var window = GetWindow<LocalSaveDataEditorWindow>();
            window.titleContent = new GUIContent(WindowTitle);
            window.minSize = new Vector2(900f, 520f);
            window.Show();
        }

        public void CreateGUI()
        {
            BuildViewFromUxml();
            DiscoverDataTypes();
            RefreshAsync(false).Forget();
        }

        private void BuildViewFromUxml()
        {
            rootVisualElement.Clear();

            var styleSheet = LoadEditorAsset<StyleSheet>(DefaultUssPath, UssAssetName, nameof(StyleSheet));
            if (styleSheet != null)
            {
                rootVisualElement.styleSheets.Add(styleSheet);
            }

            var visualTree = LoadEditorAsset<VisualTreeAsset>(DefaultUxmlPath, UxmlAssetName, nameof(VisualTreeAsset));
            if (visualTree == null)
            {
                rootVisualElement.Add(new Label($"Missing UXML asset: {UxmlAssetName}"));
                return;
            }

            visualTree.CloneTree(rootVisualElement);

            configField = rootVisualElement.Q<ObjectField>("config-field");
            profileDropdown = rootVisualElement.Q<DropdownField>("profile-dropdown");
            searchField = rootVisualElement.Q<TextField>("search-field");
            savedOnlyToggle = rootVisualElement.Q<Toggle>("saved-only-toggle");
            dataTypeList = rootVisualElement.Q<ListView>("data-type-list");
            providerLabel = rootVisualElement.Q<Label>("provider-label");
            pathLabel = rootVisualElement.Q<Label>("path-label");
            selectedTypeLabel = rootVisualElement.Q<Label>("selected-type-label");
            selectedKeyLabel = rootVisualElement.Q<Label>("selected-key-label");
            selectedStatusLabel = rootVisualElement.Q<Label>("selected-status-label");
            statusLabel = rootVisualElement.Q<Label>("status-label");
            refreshButton = rootVisualElement.Q<Button>("refresh-button");
            openFolderButton = rootVisualElement.Q<Button>("open-folder-button");
            loadButton = rootVisualElement.Q<Button>("load-button");
            saveButton = rootVisualElement.Q<Button>("save-button");
            deleteButton = rootVisualElement.Q<Button>("delete-button");
            undoButton = rootVisualElement.Q<Button>("undo-button");
            redoButton = rootVisualElement.Q<Button>("redo-button");
            formatButton = rootVisualElement.Q<Button>("format-button");
            applyJsonButton = rootVisualElement.Q<Button>("apply-json-button");
            objectTabButton = rootVisualElement.Q<Button>("object-tab-button");
            jsonTabButton = rootVisualElement.Q<Button>("json-tab-button");
            jsonTabActions = rootVisualElement.Q<VisualElement>("json-tab-actions");
            objectPanel = rootVisualElement.Q<VisualElement>("object-panel");
            jsonPanel = rootVisualElement.Q<VisualElement>("json-panel");
            objectEditorRoot = rootVisualElement.Q<VisualElement>("object-editor-root");
            jsonEditorHost = rootVisualElement.Q<VisualElement>("json-editor-host");

            configField.objectType = typeof(LocalSaveConfig);
            configField.allowSceneObjects = false;
            configField.RegisterValueChangedCallback(evt =>
            {
                config = evt.newValue as LocalSaveConfig;
                RefreshAsync(false).Forget();
            });

            refreshButton.clicked += () => RefreshAsync(true).Forget();
            openFolderButton.clicked += OpenSaveFolder;
            savedOnlyToggle.tooltip = "Only show local data types that already have saved data in the selected profile.";
            loadButton.tooltip = "Reload this data from storage. Selection already loads automatically.";
            saveButton.tooltip = "Save the current Object or JSON data to the selected profile.";
            deleteButton.tooltip = "Delete this saved data from the selected profile.";
            undoButton.tooltip = "Undo the last unsaved edit in this tool.";
            redoButton.tooltip = "Redo the last undone edit in this tool.";
            formatButton.tooltip = "Reformat and validate the JSON text.";
            applyJsonButton.tooltip = "Parse the JSON text into the Object tab without saving.";

            profileDropdown.choices = new List<string> { DefaultProfileId };
            profileDropdown.SetValueWithoutNotify(DefaultProfileId);
            profileDropdown.RegisterValueChangedCallback(evt =>
            {
                if (string.IsNullOrEmpty(evt.newValue) || evt.newValue == selectedProfileId)
                {
                    return;
                }

                selectedProfileId = evt.newValue;
                RefreshProfileAsync(true).Forget();
            });

            savedOnlyToggle.RegisterValueChangedCallback(_ => ApplyFilters());
            searchField.RegisterValueChangedCallback(_ => ApplyFilters());

            dataTypeList.fixedItemHeight = 44f;
            dataTypeList.selectionType = SelectionType.Single;
            dataTypeList.makeItem = MakeDataTypeRow;
            dataTypeList.bindItem = BindDataTypeRow;
            dataTypeList.itemsSource = filteredDataTypes;
            dataTypeList.selectionChanged += OnSelectionChanged;

            loadButton.clicked += () => LoadSelectedJsonAsync().Forget();
            saveButton.clicked += () => SaveSelectedJsonAsync().Forget();
            deleteButton.clicked += () => DeleteSelectedDataAsync().Forget();
            undoButton.clicked += UndoEdit;
            redoButton.clicked += RedoEdit;
            formatButton.clicked += FormatEditorJson;
            applyJsonButton.clicked += ApplyJsonToObject;
            objectTabButton.clicked += () => SetActivePane(EditorPane.Object);
            jsonTabButton.clicked += () => SetActivePane(EditorPane.Json);
            rootVisualElement.RegisterCallback<KeyDownEvent>(OnKeyDown);

            jsonEditorHost.Clear();
            jsonContainer = new IMGUIContainer(DrawJsonEditor);
            jsonContainer.AddToClassList("local-save-viewer__json-imgui");
            jsonEditorHost.Add(jsonContainer);

            SetActivePane(EditorPane.Object);
            SetStatus("Ready.", false);
            UpdateSelectedLabels();
            UpdateButtonStates();
        }

        private VisualElement MakeDataTypeRow()
        {
            var row = new VisualElement();
            row.AddToClassList("local-save-viewer__type-row");

            var text = new VisualElement();
            text.AddToClassList("local-save-viewer__type-row-text");
            row.Add(text);

            var name = new Label { name = "name" };
            name.AddToClassList("local-save-viewer__type-name");
            text.Add(name);

            var subtitle = new Label { name = "subtitle" };
            subtitle.AddToClassList("local-save-viewer__type-subtitle");
            text.Add(subtitle);

            var badge = new Label { name = "badge" };
            badge.AddToClassList("local-save-viewer__type-badge");
            row.Add(badge);

            return row;
        }

        private void BindDataTypeRow(VisualElement element, int index)
        {
            if (index < 0 || index >= filteredDataTypes.Count)
            {
                return;
            }

            var item = filteredDataTypes[index];
            element.Q<Label>("name").text = item.Type.Name;
            element.Q<Label>("subtitle").text = item.Type.Namespace ?? item.Type.Assembly.GetName().Name;

            var badge = element.Q<Label>("badge");
            badge.text = item.HasSavedData ? "Saved" : "New";
            badge.EnableInClassList("local-save-viewer__type-badge--saved", item.HasSavedData);
        }

        private void OnSelectionChanged(IEnumerable<object> selection)
        {
            selectedItem = selection.OfType<DataTypeItem>().FirstOrDefault();
            currentDataObject = null;
            ResetEditHistory();
            SetJsonWithoutNotify(string.Empty);
            BuildObjectEditor();
            UpdateSelectedLabels();
            UpdateButtonStates();

            if (selectedItem != null)
            {
                LoadSelectedJsonAsync().Forget();
            }
        }

        private async UniTask RefreshAsync(bool preserveSelection)
        {
            await RunBusyAsync(async () =>
            {
                var previousTypeName = preserveSelection ? selectedItem?.Type.AssemblyQualifiedName : null;

                config = config != null ? config : FindLocalSaveConfig();
                configField.SetValueWithoutNotify(config);

                await BuildProviderAsync();
                await LoadProfilesAsync();
                DiscoverDataTypes();
                await RefreshProfileDataAsync();
                ApplyFilters(previousTypeName);

                SetStatus($"Loaded {filteredDataTypes.Count} data type(s) for profile '{selectedProfileId}'.", false);
            });
        }

        private async UniTask RefreshProfileAsync(bool preserveSelection)
        {
            await RunBusyAsync(async () =>
            {
                var previousTypeName = preserveSelection ? selectedItem?.Type.AssemblyQualifiedName : null;
                await RefreshProfileDataAsync();
                ApplyFilters(previousTypeName);

                if (selectedItem != null)
                {
                    await LoadSelectedJsonAsync(false);
                }
            });
        }

        private async UniTask BuildProviderAsync()
        {
            var providerConfig = GetProviderConfig();
            var logService = new EditorLogService();

            provider = StorageProviderFactory.Create(providerConfig, logService);
            encryptionService = providerConfig.EnableEncryption ? new AesEncryptionService(config?.EncryptionSaltPrefix) : null;
            await provider.InitializeAsync();

            providerLabel.text = $"{provider.ProviderName}{(provider.UseEncryption ? " / encrypted" : " / plain")}";
            pathLabel.text = GetStorageLocationLabel(providerConfig);
            openFolderButton.SetEnabled(providerConfig.ProviderType == StorageProviderType.FileBased);
        }

        private async UniTask LoadProfilesAsync()
        {
            var registryJson = await provider.LoadAsync(null, ProfileRegistryKey);
            profileRegistry = DeserializeOrDefault<ProfileRegistry>(registryJson);

            var profileIds = new HashSet<string>();
            if (profileRegistry?.ProfileIds != null)
            {
                foreach (var profileId in profileRegistry.ProfileIds.Where(id => !string.IsNullOrWhiteSpace(id)))
                {
                    profileIds.Add(profileId);
                }
            }

            foreach (var profileId in ScanFileBasedProfiles())
            {
                profileIds.Add(profileId);
            }

            if (profileIds.Count == 0)
            {
                profileIds.Add(DefaultProfileId);
            }

            var sortedProfiles = profileIds.OrderBy(id => id, StringComparer.Ordinal).ToList();
            var preferredProfile = profileRegistry?.CurrentProfileId;
            if (!string.IsNullOrEmpty(selectedProfileId) && sortedProfiles.Contains(selectedProfileId))
            {
                preferredProfile = selectedProfileId;
            }

            selectedProfileId = !string.IsNullOrEmpty(preferredProfile) && sortedProfiles.Contains(preferredProfile)
                ? preferredProfile
                : sortedProfiles[0];

            profileDropdown.choices = sortedProfiles;
            profileDropdown.SetValueWithoutNotify(selectedProfileId);
        }

        private async UniTask RefreshProfileDataAsync()
        {
            var metadataJson = await provider.LoadAsync(selectedProfileId, ProfileMetadataKey);
            currentMetadata = DeserializeOrDefault<ProfileMetadata>(metadataJson);

            savedKeys.Clear();
            if (currentMetadata?.DataKeys != null)
            {
                foreach (var dataKey in currentMetadata.DataKeys.Where(IsUserDataKey))
                {
                    savedKeys.Add(dataKey);
                }
            }

            foreach (var dataKey in ScanFileBasedDataKeys(selectedProfileId))
            {
                savedKeys.Add(dataKey);
            }

            foreach (var item in allDataTypes)
            {
                item.HasSavedData = savedKeys.Contains(item.Key);
            }
        }

        private void DiscoverDataTypes()
        {
            allDataTypes.Clear();
            var systemTypes = new HashSet<Type>
            {
                typeof(ProfileMetadata),
                typeof(ProfileRegistry)
            };

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach (var type in GetLoadableTypes(assembly))
                {
                    if (!typeof(IUserData).IsAssignableFrom(type)
                        || type.IsInterface
                        || type.IsAbstract
                        || type.ContainsGenericParameters
                        || systemTypes.Contains(type))
                    {
                        continue;
                    }

                    allDataTypes.Add(new DataTypeItem
                    {
                        Type = type,
                        Key = DataKeyOf(type),
                        HasSavedData = savedKeys.Contains(DataKeyOf(type))
                    });
                }
            }

            allDataTypes.Sort((left, right) => string.Compare(left.Type.Name, right.Type.Name, StringComparison.OrdinalIgnoreCase));
        }

        private void ApplyFilters(string preferredTypeName = null)
        {
            filteredDataTypes.Clear();

            var query = searchField?.value?.Trim();
            foreach (var item in allDataTypes)
            {
                if (savedOnlyToggle.value && !item.HasSavedData)
                {
                    continue;
                }

                if (!string.IsNullOrEmpty(query) && !MatchesSearch(item, query))
                {
                    continue;
                }

                filteredDataTypes.Add(item);
            }

            dataTypeList.itemsSource = filteredDataTypes;
            dataTypeList.Rebuild();

            if (!string.IsNullOrEmpty(preferredTypeName))
            {
                var index = filteredDataTypes.FindIndex(item => item.Type.AssemblyQualifiedName == preferredTypeName);
                if (index >= 0)
                {
                    dataTypeList.SetSelection(index);
                    selectedItem = filteredDataTypes[index];
                    UpdateSelectedLabels();
                    return;
                }
            }

            if (selectedItem == null || !filteredDataTypes.Contains(selectedItem))
            {
                selectedItem = null;
                currentDataObject = null;
                ResetEditHistory();
                dataTypeList.ClearSelection();
                SetJsonWithoutNotify(string.Empty);
                BuildObjectEditor();
            }

            UpdateSelectedLabels();
            UpdateButtonStates();
        }

        private async UniTask LoadSelectedJsonAsync(bool wrapBusy = true)
        {
            if (selectedItem == null)
            {
                return;
            }

            async UniTask Body()
            {
                var json = await LoadPlainJsonAsync(selectedProfileId, selectedItem.Key);
                if (string.IsNullOrEmpty(json))
                {
                    ResetEditHistory();
                    currentDataObject = CreateDefaultData(selectedItem.Type);
                    SetCurrentDataObject(currentDataObject, true, true);
                    selectedItem.HasSavedData = false;
                    SetStatus($"No saved data for {selectedItem.Type.Name}. Showing a default instance.", false);
                }
                else
                {
                    ResetEditHistory();
                    currentDataObject = DeserializeUserData(json, selectedItem.Type);
                    SetCurrentDataObject(currentDataObject, true, true);
                    selectedItem.HasSavedData = true;
                    SetStatus($"Loaded {selectedItem.Key} from profile '{selectedProfileId}'.", false);
                }

                UpdateSelectedLabels();
                UpdateButtonStates();
            }

            if (wrapBusy)
            {
                await RunBusyAsync(Body);
            }
            else
            {
                await Body();
            }
        }

        private async UniTask SaveSelectedJsonAsync()
        {
            if (selectedItem == null)
            {
                return;
            }

            await RunBusyAsync(async () =>
            {
                var data = GetDataObjectForSave();
                var storageJson = JsonConvert.SerializeObject(data, Formatting.None, JsonSettings);
                var payload = MaybeEncrypt(storageJson, selectedItem.Key);

                await provider.SaveAsync(selectedProfileId, selectedItem.Key, payload);
                await SaveMetadataForDataKeyAsync(selectedItem.Key);

                selectedItem.HasSavedData = true;
                savedKeys.Add(selectedItem.Key);
                SetCurrentDataObject(data, true, false);
                ApplyFilters(selectedItem.Type.AssemblyQualifiedName);
                SetStatus($"Saved {selectedItem.Key} to profile '{selectedProfileId}'.", false);
            });
        }

        private async UniTask DeleteSelectedDataAsync()
        {
            if (selectedItem == null)
            {
                return;
            }

            var shouldDelete = EditorUtility.DisplayDialog(
                "Delete Local Data",
                $"Delete {selectedItem.Key} from profile '{selectedProfileId}'?",
                "Delete",
                "Cancel");

            if (!shouldDelete)
            {
                return;
            }

            await RunBusyAsync(async () =>
            {
                await provider.DeleteAsync(selectedProfileId, selectedItem.Key);
                await RemoveMetadataDataKeyAsync(selectedItem.Key);

                selectedItem.HasSavedData = false;
                savedKeys.Remove(selectedItem.Key);
                ResetEditHistory();
                currentDataObject = CreateDefaultData(selectedItem.Type);
                SetCurrentDataObject(currentDataObject, true, true);
                ApplyFilters(selectedItem.Type.AssemblyQualifiedName);
                SetStatus($"Deleted {selectedItem.Key} from profile '{selectedProfileId}'.", false);
            });
        }

        private void FormatEditorJson()
        {
            if (selectedItem == null || string.IsNullOrWhiteSpace(jsonText))
            {
                return;
            }

            try
            {
                var data = DeserializeUserData(jsonText, selectedItem.Type);
                SetCurrentDataObject(data, false, false);
                SetStatus("Formatted JSON.", false);
            }
            catch (Exception ex)
            {
                try
                {
                    RecordUndoSnapshot();
                    jsonText = JToken.Parse(jsonText).ToString(Formatting.Indented);
                    jsonEditSessionHasUndoSnapshot = false;
                    jsonContainer?.MarkDirtyRepaint();
                    currentDataObject = null;
                    BuildObjectEditor();
                    UpdateSelectedLabels();
                    SetStatus("Formatted JSON without type validation.", false);
                }
                catch
                {
                    SetStatus($"Invalid JSON: {ex.Message}", true);
                }
            }
        }

        private void ApplyJsonToObject()
        {
            if (TryApplyJsonToObject(true))
            {
                SetActivePane(EditorPane.Object);
            }
        }

        private bool TryApplyJsonToObject(bool showSuccessStatus)
        {
            if (selectedItem == null)
            {
                return false;
            }

            try
            {
                var data = DeserializeUserData(jsonText, selectedItem.Type);
                SetCurrentDataObject(data, false, false);
                if (showSuccessStatus)
                {
                    SetStatus($"Applied JSON to {selectedItem.Type.Name} object.", false);
                }

                return true;
            }
            catch (Exception ex)
            {
                SetStatus($"Cannot apply JSON: {ex.Message}", true);
                return false;
            }
        }

        private void SetActivePane(EditorPane pane)
        {
            if (pane == EditorPane.Object && selectedItem != null && currentDataObject == null && !TryApplyJsonToObject(false))
            {
                pane = EditorPane.Json;
            }

            if (activePane != pane)
            {
                jsonEditSessionHasUndoSnapshot = false;
            }

            activePane = pane;

            if (objectPanel != null)
            {
                objectPanel.style.display = pane == EditorPane.Object ? DisplayStyle.Flex : DisplayStyle.None;
            }

            if (jsonPanel != null)
            {
                jsonPanel.style.display = pane == EditorPane.Json ? DisplayStyle.Flex : DisplayStyle.None;
            }

            objectTabButton?.EnableInClassList("local-save-viewer__tab--active", pane == EditorPane.Object);
            jsonTabButton?.EnableInClassList("local-save-viewer__tab--active", pane == EditorPane.Json);
            if (jsonTabActions != null)
            {
                jsonTabActions.style.display = pane == EditorPane.Json ? DisplayStyle.Flex : DisplayStyle.None;
            }

            UpdateButtonStates();
        }

        private IUserData GetDataObjectForSave()
        {
            if (selectedItem == null)
            {
                throw new InvalidOperationException("No local data type selected.");
            }

            if (activePane == EditorPane.Json || currentDataObject == null)
            {
                currentDataObject = DeserializeUserData(jsonText, selectedItem.Type);
                BuildObjectEditor();
            }

            SyncJsonFromCurrentObject(false, false, false);
            return currentDataObject;
        }

        private void SetCurrentDataObject(IUserData data, bool resetBaseline, bool resetScroll, bool recordUndo = true)
        {
            currentDataObject = data;
            SyncJsonFromCurrentObject(resetBaseline, resetScroll, recordUndo);
            BuildObjectEditor();
        }

        private void SyncJsonFromCurrentObject(bool resetBaseline, bool resetScroll, bool recordUndo = true)
        {
            if (currentDataObject == null)
            {
                return;
            }

            if (!resetBaseline && recordUndo)
            {
                RecordUndoSnapshot();
            }

            jsonText = SerializeObject(currentDataObject, Formatting.Indented);
            jsonEditSessionHasUndoSnapshot = false;
            if (resetBaseline)
            {
                lastLoadedJson = jsonText;
            }

            if (resetScroll)
            {
                jsonScrollPosition = Vector2.zero;
            }

            jsonContainer?.MarkDirtyRepaint();
            UpdateSelectedLabels();
        }

        private void BuildObjectEditor()
        {
            if (objectEditorRoot == null)
            {
                return;
            }

            objectEditorRoot.Clear();

            if (selectedItem == null)
            {
                objectEditorRoot.Add(CreateObjectEditorMessage("Select a local data type to inspect its object."));
                return;
            }

            if (currentDataObject == null)
            {
                objectEditorRoot.Add(CreateObjectEditorMessage("JSON has not been applied to an object yet."));
                return;
            }

            var members = GetEditableMembers(currentDataObject.GetType()).ToList();
            if (members.Count == 0)
            {
                objectEditorRoot.Add(CreateObjectEditorMessage("No editable public fields or properties found."));
                return;
            }

            foreach (var member in members)
            {
                AddMemberEditor(objectEditorRoot, currentDataObject, member, 0);
            }
        }

        private static Label CreateObjectEditorMessage(string text)
        {
            var label = new Label(text);
            label.AddToClassList("local-save-viewer__object-message");
            return label;
        }

        private void AddMemberEditor(VisualElement parent, object owner, DataMember member, int depth, Action<object> ownerChanged = null)
        {
            object value;
            try
            {
                value = member.GetValue(owner);
            }
            catch (Exception ex)
            {
                parent.Add(CreateObjectEditorMessage($"Could not read {member.Name}: {ex.Message}"));
                return;
            }

            var editor = CreateValueEditor(member.Name, member.ValueType, value, member.CanWrite, nextValue =>
            {
                if (ownerChanged == null)
                {
                    CommitMemberValue(owner, member, nextValue);
                    return;
                }

                if (CommitMemberValue(owner, member, nextValue, false))
                {
                    ownerChanged(owner);
                }
            }, depth);
            parent.Add(editor);
        }

        private VisualElement CreateValueEditor(string label, Type valueType, object value, bool canWrite, Action<object> commitValue, int depth)
        {
            var effectiveType = Nullable.GetUnderlyingType(valueType) ?? valueType;

            if (IsSimpleEditableType(effectiveType))
            {
                return CreateSimpleValueEditor(label, valueType, value, canWrite, commitValue);
            }

            if (value is IDictionary || value == null && IsDictionaryLike(valueType))
            {
                return CreateDictionaryEditor(label, valueType, value, canWrite, commitValue, depth);
            }

            if (value is IList || value == null && IsListLike(valueType))
            {
                return CreateListEditor(label, valueType, value, canWrite, commitValue, depth);
            }

            if (value != null && depth < MaxStructuredObjectDepth && IsPlainEditableObject(effectiveType))
            {
                return CreateLazyFoldout(label, effectiveType, depth, foldout =>
                {
                    foreach (var childMember in GetEditableMembers(effectiveType))
                    {
                        AddMemberEditor(foldout, value, childMember, depth + 1, updatedOwner => commitValue(updatedOwner));
                    }
                });
            }

            if (value == null && canWrite && depth < MaxStructuredObjectDepth && IsPlainEditableObject(effectiveType))
            {
                var row = CreateObjectRow(label, GetFriendlyTypeName(effectiveType));
                var createButton = new Button(() =>
                {
                    var newValue = CreateRuntimeInstance(effectiveType);
                    commitValue(newValue);
                    BuildObjectEditor();
                }) { text = "Create" };
                row.Add(createButton);
                return row;
            }

            return CreateJsonValueEditor(label, valueType, value, canWrite, commitValue, depth);
        }

        private VisualElement CreateSimpleValueEditor(string label, Type valueType, object value, bool canWrite, Action<object> commitValue)
        {
            var effectiveType = Nullable.GetUnderlyingType(valueType) ?? valueType;
            var row = CreateObjectRow(label, GetFriendlyTypeName(valueType));
            VisualElement field;

            if (effectiveType == typeof(bool))
            {
                var toggle = new Toggle { value = value is bool boolValue && boolValue };
                toggle.RegisterValueChangedCallback(evt => commitValue(evt.newValue));
                field = toggle;
            }
            else if (effectiveType == typeof(int))
            {
                var intField = new IntegerField { value = value != null ? Convert.ToInt32(value) : 0 };
                intField.RegisterValueChangedCallback(evt => commitValue(Convert.ChangeType(evt.newValue, effectiveType)));
                field = intField;
            }
            else if (effectiveType == typeof(long))
            {
                var longField = new LongField { value = value != null ? Convert.ToInt64(value) : 0L };
                longField.RegisterValueChangedCallback(evt => commitValue(Convert.ChangeType(evt.newValue, effectiveType)));
                field = longField;
            }
            else if (effectiveType == typeof(float))
            {
                var floatField = new FloatField { value = value != null ? Convert.ToSingle(value) : 0f };
                floatField.RegisterValueChangedCallback(evt => commitValue(evt.newValue));
                field = floatField;
            }
            else if (effectiveType == typeof(double))
            {
                var doubleField = new DoubleField { value = value != null ? Convert.ToDouble(value) : 0d };
                doubleField.RegisterValueChangedCallback(evt => commitValue(evt.newValue));
                field = doubleField;
            }
            else if (effectiveType.IsEnum)
            {
                var enumValue = value as Enum ?? (Enum)Enum.GetValues(effectiveType).GetValue(0);
                var enumField = new EnumField(enumValue);
                enumField.RegisterValueChangedCallback(evt => commitValue(evt.newValue));
                field = enumField;
            }
            else
            {
                var textField = new TextField { value = ValueToEditorString(value, effectiveType) };
                textField.RegisterValueChangedCallback(evt =>
                {
                    if (TryParseEditorString(evt.newValue, effectiveType, out var parsedValue))
                    {
                        commitValue(parsedValue);
                    }
                });
                field = textField;
            }

            field.SetEnabled(canWrite);
            field.AddToClassList("local-save-viewer__object-value");
            row.Add(field);
            return row;
        }

        private VisualElement CreateDictionaryEditor(string label, Type valueType, object value, bool canWrite, Action<object> commitValue, int depth)
        {
            var foldout = CreateLazyFoldout(label, valueType, depth, target => BuildDictionaryContent(target, label, valueType, value, canWrite, commitValue, depth));

            return foldout;
        }

        private void BuildDictionaryContent(VisualElement parent, string label, Type valueType, object value, bool canWrite, Action<object> commitValue, int depth)
        {
            var dictionary = value as IDictionary;

            if (dictionary == null)
            {
                AddCreateComplexButton(parent, valueType, canWrite, commitValue);
                return;
            }

            var dictionaryTypes = GetDictionaryTypes(valueType, dictionary.GetType());
            var keyItemType = dictionaryTypes.keyType;
            var valueItemType = dictionaryTypes.valueType;
            var canMutate = !dictionary.IsReadOnly;

            AddDictionaryToolbar(parent, dictionary, keyItemType, valueItemType, canMutate);

            var searchField = CreateCollectionSearchField(GetCollectionSearchKey(label, valueType, value, depth));
            parent.Add(searchField);

            var content = new VisualElement();
            content.AddToClassList("local-save-viewer__collection-content");
            parent.Add(content);

            void RebuildContent()
            {
                content.Clear();
                BuildDictionaryItems(content, dictionary, valueItemType, canMutate, searchField.value, depth);
            }

            searchField.RegisterValueChangedCallback(evt =>
            {
                collectionSearches[searchField.name] = evt.newValue;
                RebuildContent();
            });
            RebuildContent();
        }

        private void BuildDictionaryItems(VisualElement parent, IDictionary dictionary, Type valueItemType, bool canMutate, string query, int depth)
        {
            var allEntries = EnumerateDictionary(dictionary).ToList();
            var matchedEntries = allEntries
                .Where((entry, index) => MatchesCollectionSearch(query, index, entry.key, entry.value))
                .ToList();
            var entries = matchedEntries.Take(MaxCollectionItemsInObjectView).ToList();
            foreach (var entry in entries)
            {
                var entryKey = entry.key;
                var entryValue = entry.value;
                var entryType = entryValue?.GetType() ?? valueItemType ?? typeof(object);
                var keyLabel = entryKey != null ? entryKey.ToString() : "null";

                var itemContainer = new VisualElement();
                itemContainer.AddToClassList("local-save-viewer__collection-item");

                var itemEditor = CreateValueEditor(keyLabel, entryType, entryValue, true, nextValue =>
                {
                    if (TrySetDictionaryValue(dictionary, entryKey, nextValue))
                    {
                        SyncJsonFromCurrentObject(false, false);
                    }
                }, depth + 1);
                itemContainer.Add(itemEditor);

                var removeButton = new Button(() =>
                {
                    if (TryRemoveDictionaryValue(dictionary, entryKey))
                    {
                        SyncJsonFromCurrentObject(false, false);
                        BuildObjectEditor();
                    }
                }) { text = "Remove" };
                removeButton.AddToClassList("local-save-viewer__collection-remove");
                removeButton.SetEnabled(canMutate);
                itemContainer.Add(removeButton);

                parent.Add(itemContainer);
            }

            if (allEntries.Count == 0)
            {
                parent.Add(CreateObjectEditorMessage("Empty dictionary."));
            }
            else if (matchedEntries.Count == 0)
            {
                parent.Add(CreateObjectEditorMessage("No dictionary entries match the search."));
            }

            if (matchedEntries.Count > MaxCollectionItemsInObjectView)
            {
                parent.Add(CreateObjectEditorMessage($"Showing first {MaxCollectionItemsInObjectView} of {matchedEntries.Count} matching entries. Refine the search or use JSON view for the rest."));
            }
        }

        private VisualElement CreateListEditor(string label, Type valueType, object value, bool canWrite, Action<object> commitValue, int depth)
        {
            var foldout = CreateLazyFoldout(label, valueType, depth, target => BuildListContent(target, label, valueType, value, canWrite, commitValue, depth));

            return foldout;
        }

        private void BuildListContent(VisualElement parent, string label, Type valueType, object value, bool canWrite, Action<object> commitValue, int depth)
        {
            var list = value as IList;

            if (list == null)
            {
                AddCreateComplexButton(parent, valueType, canWrite, commitValue);
                return;
            }

            var itemType = GetListItemType(valueType, list.GetType());
            var canResize = CanResizeList(list, canWrite);
            AddListToolbar(parent, list, valueType, itemType, canResize, commitValue);

            var searchField = CreateCollectionSearchField(GetCollectionSearchKey(label, valueType, value, depth));
            parent.Add(searchField);

            var content = new VisualElement();
            content.AddToClassList("local-save-viewer__collection-content");
            parent.Add(content);

            void RebuildContent()
            {
                content.Clear();
                BuildListItems(content, list, valueType, itemType, canResize, searchField.value, depth, commitValue);
            }

            searchField.RegisterValueChangedCallback(evt =>
            {
                collectionSearches[searchField.name] = evt.newValue;
                RebuildContent();
            });
            RebuildContent();
        }

        private void BuildListItems(VisualElement parent, IList list, Type valueType, Type itemType, bool canResize, string query, int depth, Action<object> commitValue)
        {
            var matchedIndices = Enumerable.Range(0, list.Count)
                .Where(index => MatchesCollectionSearch(query, index, index, list[index]))
                .ToList();
            foreach (var index in matchedIndices.Take(MaxCollectionItemsInObjectView))
            {
                var item = list[index];
                var itemValueType = item?.GetType() ?? itemType ?? typeof(object);

                var itemContainer = new VisualElement();
                itemContainer.AddToClassList("local-save-viewer__collection-item");

                var itemEditor = CreateValueEditor($"[{index}]", itemValueType, item, true, nextValue =>
                {
                    list[index] = nextValue;
                    SyncJsonFromCurrentObject(false, false);
                }, depth + 1);
                itemContainer.Add(itemEditor);

                var removeButton = new Button(() =>
                {
                    RemoveListItem(list, valueType, index, commitValue);
                    SyncJsonFromCurrentObject(false, false);
                    BuildObjectEditor();
                }) { text = "Remove" };
                removeButton.AddToClassList("local-save-viewer__collection-remove");
                removeButton.SetEnabled(canResize);
                itemContainer.Add(removeButton);

                parent.Add(itemContainer);
            }

            if (list.Count == 0)
            {
                parent.Add(CreateObjectEditorMessage("Empty list."));
            }
            else if (matchedIndices.Count == 0)
            {
                parent.Add(CreateObjectEditorMessage("No list items match the search."));
            }

            if (matchedIndices.Count > MaxCollectionItemsInObjectView)
            {
                parent.Add(CreateObjectEditorMessage($"Showing first {MaxCollectionItemsInObjectView} of {matchedIndices.Count} matching entries. Refine the search or use JSON view for the rest."));
            }
        }

        private VisualElement CreateJsonValueEditor(string label, Type valueType, object value, bool canWrite, Action<object> commitValue, int depth)
        {
            var foldout = CreateFoldout(label, valueType, depth);
            var json = SerializeObject(value, Formatting.Indented);
            var textField = new TextField { multiline = true, value = json };
            textField.AddToClassList("local-save-viewer__member-json");
            textField.SetEnabled(canWrite);
            foldout.Add(textField);

            var applyButton = new Button(() =>
            {
                try
                {
                    var nextValue = JsonConvert.DeserializeObject(textField.value, valueType, JsonSettings);
                    commitValue(nextValue);
                    SetStatus($"Applied {label}.", false);
                }
                catch (Exception ex)
                {
                    SetStatus($"Invalid {label} JSON: {ex.Message}", true);
                }
            }) { text = "Apply Member JSON" };
            applyButton.SetEnabled(canWrite);
            foldout.Add(applyButton);
            return foldout;
        }

        private VisualElement CreateObjectRow(string label, string typeName)
        {
            var row = new VisualElement();
            row.AddToClassList("local-save-viewer__object-row");

            var labelContainer = new VisualElement();
            labelContainer.AddToClassList("local-save-viewer__object-labels");
            var nameLabel = new Label(label);
            nameLabel.AddToClassList("local-save-viewer__object-name");
            var typeLabel = new Label(typeName);
            typeLabel.AddToClassList("local-save-viewer__object-type");
            labelContainer.Add(nameLabel);
            labelContainer.Add(typeLabel);
            row.Add(labelContainer);

            return row;
        }

        private Foldout CreateFoldout(string label, Type valueType, int depth)
        {
            var foldout = new Foldout
            {
                text = $"{label} ({GetFriendlyTypeName(valueType)})",
                value = depth < 1
            };
            foldout.AddToClassList("local-save-viewer__object-foldout");
            return foldout;
        }

        private Foldout CreateLazyFoldout(string label, Type valueType, int depth, Action<Foldout> buildContent)
        {
            var foldout = CreateFoldout(label, valueType, depth);
            var isBuilt = false;

            void BuildOnce()
            {
                if (isBuilt)
                {
                    return;
                }

                isBuilt = true;
                try
                {
                    buildContent(foldout);
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                    foldout.Add(CreateObjectEditorMessage($"Could not build this value: {ex.Message}"));
                    SetStatus($"Could not build {label}: {ex.Message}", true);
                }
            }

            if (foldout.value)
            {
                BuildOnce();
            }
            else
            {
                foldout.RegisterValueChangedCallback(evt =>
                {
                    if (evt.newValue)
                    {
                        BuildOnce();
                    }
                });
            }

            return foldout;
        }

        private void AddDictionaryToolbar(VisualElement parent, IDictionary dictionary, Type keyType, Type valueType, bool canMutate)
        {
            var toolbar = new VisualElement();
            toolbar.AddToClassList("local-save-viewer__collection-toolbar");

            var keyField = new TextField { label = "Key" };
            keyField.AddToClassList("local-save-viewer__collection-key");
            toolbar.Add(keyField);

            var addButton = new Button(() =>
            {
                if (!TryParseEditorString(keyField.value, keyType, out var key))
                {
                    SetStatus($"Invalid dictionary key for {GetFriendlyTypeName(keyType)}.", true);
                    return;
                }

                if (DictionaryContainsKey(dictionary, key))
                {
                    SetStatus($"Dictionary already contains key '{key}'.", true);
                    return;
                }

                if (!TrySetDictionaryValue(dictionary, key, CreateDefaultElementValue(valueType)))
                {
                    return;
                }

                SyncJsonFromCurrentObject(false, false);
                BuildObjectEditor();
            }) { text = "Add Entry" };
            addButton.SetEnabled(canMutate);
            toolbar.Add(addButton);

            parent.Add(toolbar);
        }

        private void AddListToolbar(VisualElement parent, IList list, Type listType, Type itemType, bool canResize, Action<object> commitValue)
        {
            var toolbar = new VisualElement();
            toolbar.AddToClassList("local-save-viewer__collection-toolbar");

            var addButton = new Button(() =>
            {
                AddListItem(list, listType, CreateDefaultElementValue(itemType), commitValue);
                SyncJsonFromCurrentObject(false, false);
                BuildObjectEditor();
            }) { text = $"Add {GetFriendlyTypeName(itemType)}" };
            addButton.SetEnabled(canResize);
            toolbar.Add(addButton);

            parent.Add(toolbar);
        }

        private TextField CreateCollectionSearchField(string key)
        {
            collectionSearches.TryGetValue(key, out var currentSearch);
            var searchField = new TextField
            {
                name = key,
                label = "Search items",
                value = currentSearch ?? string.Empty,
                tooltip = "Filter this collection by index, dictionary key, simple value, or compact JSON."
            };
            searchField.AddToClassList("local-save-viewer__collection-search");
            return searchField;
        }

        private static string GetCollectionSearchKey(string label, Type valueType, object value, int depth)
        {
            var valueId = value != null ? System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(value) : 0;
            return $"collection-search-{depth}-{label}-{valueType.AssemblyQualifiedName}-{valueId}";
        }

        private static bool MatchesCollectionSearch(string query, int index, object key, object value)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return true;
            }

            return ContainsSearchText(index.ToString(), query)
                   || ContainsSearchText(Convert.ToString(key, System.Globalization.CultureInfo.InvariantCulture), query)
                   || ContainsSearchText(ValueToSearchText(value), query);
        }

        private static bool ContainsSearchText(string value, string query)
        {
            return !string.IsNullOrEmpty(value) && value.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static string ValueToSearchText(object value)
        {
            if (value == null)
            {
                return "null";
            }

            var valueType = value.GetType();
            if (IsSimpleEditableType(Nullable.GetUnderlyingType(valueType) ?? valueType))
            {
                return ValueToEditorString(value, valueType);
            }

            try
            {
                var json = SerializeObject(value, Formatting.None);
                return json.Length <= 512 ? json : json.Substring(0, 512);
            }
            catch
            {
                return value.ToString();
            }
        }

        private void AddCreateComplexButton(VisualElement parent, Type valueType, bool canWrite, Action<object> commitValue)
        {
            if (!canWrite)
            {
                parent.Add(CreateObjectEditorMessage("Value is null and cannot be assigned."));
                return;
            }

            var button = new Button(() =>
            {
                var value = CreateRuntimeInstance(valueType);
                commitValue(value);
                BuildObjectEditor();
            }) { text = "Create Value" };
            parent.Add(button);
        }

        private bool CommitMemberValue(object owner, DataMember member, object nextValue, bool syncAfterCommit = true)
        {
            if (member.CanWrite)
            {
                member.SetValue(owner, nextValue);
            }
            else
            {
                var currentValue = member.GetValue(owner);
                if (!TryPopulateExistingValue(currentValue, nextValue))
                {
                    SetStatus($"{member.Name} is read-only.", true);
                    return false;
                }
            }

            if (syncAfterCommit)
            {
                SyncJsonFromCurrentObject(false, false);
            }

            return true;
        }

        private async UniTask SaveMetadataForDataKeyAsync(string dataKey)
        {
            if (currentMetadata == null || currentMetadata.ProfileId != selectedProfileId)
            {
                var metadataJson = await provider.LoadAsync(selectedProfileId, ProfileMetadataKey);
                currentMetadata = DeserializeOrDefault<ProfileMetadata>(metadataJson);
            }

            currentMetadata ??= ProfileMetadata.CreateNew(
                selectedProfileId,
                selectedProfileId,
                Application.version,
                SystemInfo.deviceUniqueIdentifier);

            currentMetadata.SetDataKey(dataKey);
            currentMetadata.RecordSave();
            await SaveSystemJsonAsync(selectedProfileId, ProfileMetadataKey, currentMetadata);

            await EnsureProfileRegisteredAsync(selectedProfileId);
        }

        private async UniTask RemoveMetadataDataKeyAsync(string dataKey)
        {
            if (currentMetadata == null || currentMetadata.ProfileId != selectedProfileId)
            {
                var metadataJson = await provider.LoadAsync(selectedProfileId, ProfileMetadataKey);
                currentMetadata = DeserializeOrDefault<ProfileMetadata>(metadataJson);
            }

            if (currentMetadata == null)
            {
                return;
            }

            currentMetadata.RemoveDataKey(dataKey);
            currentMetadata.RecordSave();
            await SaveSystemJsonAsync(selectedProfileId, ProfileMetadataKey, currentMetadata);
        }

        private async UniTask EnsureProfileRegisteredAsync(string profileId)
        {
            var changed = false;
            if (profileRegistry == null)
            {
                profileRegistry = ProfileRegistry.CreateDefault(profileId);
                changed = true;
            }

            if (profileRegistry.ProfileIds == null)
            {
                profileRegistry.ProfileIds = new HashSet<string>();
                changed = true;
            }

            if (string.IsNullOrEmpty(profileRegistry.CurrentProfileId))
            {
                profileRegistry.CurrentProfileId = profileId;
                changed = true;
            }

            if (!profileRegistry.HasProfile(profileId))
            {
                profileRegistry.AddProfile(profileId);
                changed = true;
            }

            if (changed)
            {
                await SaveSystemJsonAsync(null, ProfileRegistryKey, profileRegistry);
                await LoadProfilesAsync();
            }
        }

        private async UniTask SaveSystemJsonAsync(string profileId, string key, object value)
        {
            var json = JsonConvert.SerializeObject(value, Formatting.None, JsonSettings);
            await provider.SaveAsync(profileId, key, json);
        }

        private async UniTask<string> LoadPlainJsonAsync(string profileId, string key)
        {
            var json = await provider.LoadAsync(profileId, key);
            if (string.IsNullOrEmpty(json))
            {
                return json;
            }

            if (ShouldEncrypt(key) && provider.UseEncryption)
            {
                if (encryptionService == null)
                {
                    throw new InvalidOperationException("Save data is encrypted, but no encryption service is configured.");
                }

                json = encryptionService.Decrypt(json);
            }

            return json;
        }

        private string MaybeEncrypt(string json, string key)
        {
            if (!ShouldEncrypt(key) || !provider.UseEncryption)
            {
                return json;
            }

            if (encryptionService == null)
            {
                throw new InvalidOperationException("Save data is encrypted, but no encryption service is configured.");
            }

            return encryptionService.Encrypt(json);
        }

        private static IUserData DeserializeUserData(string json, Type type)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                throw new InvalidOperationException("JSON is empty.");
            }

            var data = JsonConvert.DeserializeObject(json, type, JsonSettings) as IUserData;
            if (data == null)
            {
                throw new InvalidOperationException($"JSON did not deserialize to {type.Name}.");
            }

            return data;
        }

        private static T DeserializeOrDefault<T>(string json) where T : class
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return null;
            }

            try
            {
                return JsonConvert.DeserializeObject<T>(json, JsonSettings);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[LocalSaveViewer] Failed to deserialize {typeof(T).Name}: {ex.Message}");
                return null;
            }
        }

        private static string PrettyPrintJson(string json, Type type)
        {
            try
            {
                var data = JsonConvert.DeserializeObject(json, type, JsonSettings);
                return JsonConvert.SerializeObject(data, Formatting.Indented, JsonSettings);
            }
            catch
            {
                return JToken.Parse(json).ToString(Formatting.Indented);
            }
        }

        private static string CreateDefaultJson(Type type)
        {
            try
            {
                var data = Activator.CreateInstance(type);
                return JsonConvert.SerializeObject(data, Formatting.Indented, JsonSettings);
            }
            catch (Exception ex)
            {
                return $"{{\n  \"error\": \"Could not create default {type.Name}: {ex.Message}\"\n}}";
            }
        }

        private static IUserData CreateDefaultData(Type type)
        {
            return (IUserData)CreateRuntimeInstance(type);
        }

        private static object CreateRuntimeInstance(Type type)
        {
            if (type.IsInterface || type.IsAbstract)
            {
                if (type == typeof(IDictionary))
                {
                    return new Dictionary<object, object>();
                }

                var dictionaryType = FindGenericInterface(type, typeof(IDictionary<,>))
                                     ?? FindGenericInterface(type, typeof(IReadOnlyDictionary<,>));
                if (dictionaryType != null)
                {
                    var args = dictionaryType.GetGenericArguments();
                    return Activator.CreateInstance(typeof(Dictionary<,>).MakeGenericType(args));
                }

                if (type == typeof(IList))
                {
                    return new List<object>();
                }

                var listType = FindGenericInterface(type, typeof(IList<>))
                               ?? FindGenericInterface(type, typeof(IReadOnlyList<>));
                if (listType != null)
                {
                    var args = listType.GetGenericArguments();
                    return Activator.CreateInstance(typeof(List<>).MakeGenericType(args));
                }

                throw new InvalidOperationException($"Cannot create {GetFriendlyTypeName(type)}.");
            }

            return Activator.CreateInstance(type);
        }

        private static object CreateDefaultElementValue(Type type)
        {
            type = Nullable.GetUnderlyingType(type) ?? type;

            if (type == typeof(string))
            {
                return string.Empty;
            }

            if (type.IsEnum)
            {
                return Enum.GetValues(type).GetValue(0);
            }

            if (type.IsValueType)
            {
                return Activator.CreateInstance(type);
            }

            try
            {
                return CreateRuntimeInstance(type);
            }
            catch
            {
                return null;
            }
        }

        private static bool CanResizeList(IList list, bool canAssignParent)
        {
            return list != null && (!list.IsFixedSize && !list.IsReadOnly || canAssignParent && list.GetType().IsArray);
        }

        private static void AddListItem(IList list, Type listType, object item, Action<object> commitValue)
        {
            if (list.GetType().IsArray)
            {
                var itemType = GetListItemType(listType, list.GetType());
                var nextArray = Array.CreateInstance(itemType, list.Count + 1);
                for (var i = 0; i < list.Count; i++)
                {
                    nextArray.SetValue(list[i], i);
                }

                nextArray.SetValue(item, list.Count);
                commitValue(nextArray);
                return;
            }

            list.Add(item);
        }

        private static void RemoveListItem(IList list, Type listType, int index, Action<object> commitValue)
        {
            if (list.GetType().IsArray)
            {
                var itemType = GetListItemType(listType, list.GetType());
                var nextArray = Array.CreateInstance(itemType, list.Count - 1);
                var targetIndex = 0;
                for (var i = 0; i < list.Count; i++)
                {
                    if (i == index)
                    {
                        continue;
                    }

                    nextArray.SetValue(list[i], targetIndex++);
                }

                commitValue(nextArray);
                return;
            }

            list.RemoveAt(index);
        }

        private static string SerializeObject(object value, Formatting formatting)
        {
            return JsonConvert.SerializeObject(value, formatting, JsonSettings);
        }

        private static IEnumerable<DataMember> GetEditableMembers(Type type)
        {
            const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Public;

            foreach (var field in type.GetFields(Flags))
            {
                if (field.IsLiteral || field.GetCustomAttribute<JsonIgnoreAttribute>() != null)
                {
                    continue;
                }

                yield return new DataMember(
                    field.Name,
                    field.FieldType,
                    !field.IsInitOnly,
                    owner => field.GetValue(owner),
                    (owner, value) => field.SetValue(owner, value));
            }

            foreach (var property in type.GetProperties(Flags))
            {
                if (property.GetIndexParameters().Length > 0
                    || property.GetMethod == null
                    || property.GetCustomAttribute<JsonIgnoreAttribute>() != null)
                {
                    continue;
                }

                yield return new DataMember(
                    property.Name,
                    property.PropertyType,
                    property.SetMethod != null,
                    owner => property.GetValue(owner),
                    (owner, value) => property.SetValue(owner, value));
            }
        }

        private static bool IsSimpleEditableType(Type type)
        {
            return type == typeof(string)
                   || type == typeof(bool)
                   || type == typeof(byte)
                   || type == typeof(sbyte)
                   || type == typeof(short)
                   || type == typeof(ushort)
                   || type == typeof(int)
                   || type == typeof(uint)
                   || type == typeof(long)
                   || type == typeof(ulong)
                   || type == typeof(float)
                   || type == typeof(double)
                   || type == typeof(decimal)
                   || type == typeof(DateTime)
                   || type.IsEnum;
        }

        private static bool IsPlainEditableObject(Type type)
        {
            var namespaceName = type.Namespace ?? string.Empty;
            return (type.IsClass || type.IsValueType)
                   && !IsSimpleEditableType(type)
                   && !typeof(UnityEngine.Object).IsAssignableFrom(type)
                   && !typeof(Delegate).IsAssignableFrom(type)
                   && !type.IsPointer
                   && !type.IsGenericParameter
                   && !namespaceName.StartsWith("System", StringComparison.Ordinal)
                   && !namespaceName.StartsWith("Unity", StringComparison.Ordinal)
                   && !namespaceName.StartsWith("UniRx", StringComparison.Ordinal);
        }

        private static string ValueToEditorString(object value, Type type)
        {
            if (value == null)
            {
                return string.Empty;
            }

            if (type == typeof(DateTime))
            {
                return ((DateTime)value).ToString("O");
            }

            return Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture);
        }

        private static bool TryParseEditorString(string text, Type type, out object value)
        {
            try
            {
                if (type == typeof(string))
                {
                    value = text;
                    return true;
                }

                if (type == typeof(DateTime))
                {
                    value = DateTime.Parse(text, null, System.Globalization.DateTimeStyles.RoundtripKind);
                    return true;
                }

                value = Convert.ChangeType(text, type, System.Globalization.CultureInfo.InvariantCulture);
                return true;
            }
            catch
            {
                value = null;
                return false;
            }
        }

        private static bool IsDictionaryLike(Type type)
        {
            return type != null
                   && (typeof(IDictionary).IsAssignableFrom(type)
                       || FindGenericInterface(type, typeof(IDictionary<,>)) != null
                       || FindGenericInterface(type, typeof(IReadOnlyDictionary<,>)) != null);
        }

        private static bool IsListLike(Type type)
        {
            return type != null
                   && type != typeof(string)
                   && (type.IsArray
                       || typeof(IList).IsAssignableFrom(type)
                       || FindGenericInterface(type, typeof(IList<>)) != null
                       || FindGenericInterface(type, typeof(IReadOnlyList<>)) != null);
        }

        private static Type FindGenericInterface(Type type, Type genericTypeDefinition)
        {
            if (type == null)
            {
                return null;
            }

            if (type.IsGenericType && type.GetGenericTypeDefinition() == genericTypeDefinition)
            {
                return type;
            }

            return type.GetInterfaces()
                .FirstOrDefault(interfaceType => interfaceType.IsGenericType
                                                 && interfaceType.GetGenericTypeDefinition() == genericTypeDefinition);
        }

        private static (Type keyType, Type valueType) GetDictionaryTypes(params Type[] dictionaryTypes)
        {
            foreach (var dictionaryType in dictionaryTypes.Where(type => type != null))
            {
                var genericDictionary = FindGenericInterface(dictionaryType, typeof(IDictionary<,>))
                                        ?? FindGenericInterface(dictionaryType, typeof(IReadOnlyDictionary<,>));

                if (genericDictionary == null)
                {
                    continue;
                }

                var args = genericDictionary.GetGenericArguments();
                return (args[0], args[1]);
            }

            return (typeof(object), typeof(object));
        }

        private static IEnumerable<(object key, object value)> EnumerateDictionary(IDictionary dictionary)
        {
            foreach (var item in dictionary)
            {
                if (item is DictionaryEntry entry)
                {
                    yield return (entry.Key, entry.Value);
                    continue;
                }

                if (TryReadKeyValuePair(item, out var key, out var value))
                {
                    yield return (key, value);
                }
            }
        }

        private static bool TryReadKeyValuePair(object item, out object key, out object value)
        {
            key = null;
            value = null;

            if (item == null)
            {
                return false;
            }

            var itemType = item.GetType();
            var keyProperty = itemType.GetProperty("Key");
            var valueProperty = itemType.GetProperty("Value");
            if (keyProperty == null || valueProperty == null)
            {
                return false;
            }

            key = keyProperty.GetValue(item);
            value = valueProperty.GetValue(item);
            return true;
        }

        private static bool DictionaryContainsKey(IDictionary dictionary, object key)
        {
            try
            {
                return dictionary.Contains(key);
            }
            catch (Exception ex) when (IsDictionaryAccessException(ex))
            {
                return EnumerateDictionary(dictionary).Any(entry => Equals(entry.key, key));
            }
        }

        private bool TrySetDictionaryValue(IDictionary dictionary, object key, object value)
        {
            try
            {
                dictionary[key] = value;
                return true;
            }
            catch (Exception ex) when (IsDictionaryAccessException(ex))
            {
                SetStatus($"Could not update dictionary entry '{key}': {ex.Message}", true);
                return false;
            }
        }

        private bool TryRemoveDictionaryValue(IDictionary dictionary, object key)
        {
            try
            {
                dictionary.Remove(key);
                return true;
            }
            catch (Exception ex) when (IsDictionaryAccessException(ex))
            {
                SetStatus($"Could not remove dictionary entry '{key}': {ex.Message}", true);
                return false;
            }
        }

        private static bool IsDictionaryAccessException(Exception ex)
        {
            return ex is ArgumentException
                   || ex is InvalidCastException
                   || ex is NotSupportedException
                   || ex is NotImplementedException;
        }

        private static Type GetListItemType(params Type[] listTypes)
        {
            foreach (var listType in listTypes.Where(type => type != null))
            {
                if (listType.IsArray)
                {
                    return listType.GetElementType();
                }

                var genericList = FindGenericInterface(listType, typeof(IList<>))
                                  ?? FindGenericInterface(listType, typeof(IReadOnlyList<>));

                if (genericList != null)
                {
                    return genericList.GetGenericArguments()[0];
                }
            }

            return typeof(object);
        }

        private static string GetFriendlyTypeName(Type type)
        {
            if (!type.IsGenericType)
            {
                return type.Name;
            }

            var name = type.Name;
            var tickIndex = name.IndexOf('`');
            if (tickIndex >= 0)
            {
                name = name.Substring(0, tickIndex);
            }

            return $"{name}<{string.Join(", ", type.GetGenericArguments().Select(GetFriendlyTypeName))}>";
        }

        private static bool TryPopulateExistingValue(object currentValue, object nextValue)
        {
            if (currentValue == null || nextValue == null)
            {
                return false;
            }

            if (currentValue is IDictionary currentDictionary && nextValue is IDictionary nextDictionary)
            {
                var entries = EnumerateDictionary(nextDictionary).ToList();
                currentDictionary.Clear();
                foreach (var entry in entries)
                {
                    currentDictionary[entry.key] = entry.value;
                }

                return true;
            }

            if (currentValue is IList currentList && nextValue is IList nextList && !currentValue.GetType().IsArray)
            {
                currentList.Clear();
                foreach (var item in nextList)
                {
                    currentList.Add(item);
                }

                return true;
            }

            JsonConvert.PopulateObject(SerializeObject(nextValue, Formatting.None), currentValue, JsonSettings);
            return true;
        }

        private void OnKeyDown(KeyDownEvent evt)
        {
            if (!evt.ctrlKey && !evt.commandKey)
            {
                return;
            }

            if (evt.keyCode == KeyCode.Z)
            {
                UndoEdit();
                evt.StopPropagation();
            }
            else if (evt.keyCode == KeyCode.Y || evt.shiftKey && evt.keyCode == KeyCode.Z)
            {
                RedoEdit();
                evt.StopPropagation();
            }
        }

        private void UndoEdit()
        {
            RestoreHistorySnapshot(undoHistory, redoHistory, "Undid edit.");
        }

        private void RedoEdit()
        {
            RestoreHistorySnapshot(redoHistory, undoHistory, "Redid edit.");
        }

        private void RestoreHistorySnapshot(List<HistoryEntry> source, List<HistoryEntry> destination, string status)
        {
            if (selectedItem == null || !CanRestoreSnapshot(source))
            {
                return;
            }

            var snapshot = source[source.Count - 1];
            source.RemoveAt(source.Count - 1);
            AddHistorySnapshot(destination, CreateHistoryEntry(jsonText));

            suppressHistory = true;
            try
            {
                jsonText = snapshot.Json ?? string.Empty;
                currentDataObject = TryDeserializeCurrentJson();
                BuildObjectEditor();
                jsonContainer?.MarkDirtyRepaint();
                jsonEditSessionHasUndoSnapshot = false;

                if (activePane == EditorPane.Object && currentDataObject == null)
                {
                    SetActivePane(EditorPane.Json);
                }

                SetStatus(status, false);
                UpdateSelectedLabels();
            }
            finally
            {
                suppressHistory = false;
                UpdateButtonStates();
            }
        }

        private void RecordUndoSnapshot()
        {
            if (suppressHistory || selectedItem == null)
            {
                return;
            }

            AddHistorySnapshot(undoHistory, CreateHistoryEntry(jsonText));
            redoHistory.Clear();
            UpdateButtonStates();
        }

        private HistoryEntry CreateHistoryEntry(string json)
        {
            return new HistoryEntry
            {
                ProfileId = selectedProfileId,
                DataKey = selectedItem?.Key,
                TypeName = selectedItem?.Type.AssemblyQualifiedName,
                Json = json ?? string.Empty
            };
        }

        private static void AddHistorySnapshot(List<HistoryEntry> history, HistoryEntry entry)
        {
            if (entry == null)
            {
                return;
            }

            if (history.Count > 0 && history[history.Count - 1].Json == entry.Json)
            {
                return;
            }

            history.Add(entry);
            if (history.Count > MaxHistoryEntries)
            {
                history.RemoveAt(0);
            }
        }

        private void ResetEditHistory()
        {
            undoHistory.Clear();
            redoHistory.Clear();
            jsonEditSessionHasUndoSnapshot = false;
            UpdateButtonStates();
        }

        private bool CanRestoreSnapshot(List<HistoryEntry> history)
        {
            if (selectedItem == null || history.Count == 0)
            {
                return false;
            }

            var snapshot = history[history.Count - 1];
            return snapshot.ProfileId == selectedProfileId
                   && snapshot.DataKey == selectedItem.Key
                   && snapshot.TypeName == selectedItem.Type.AssemblyQualifiedName;
        }

        private IUserData TryDeserializeCurrentJson()
        {
            if (selectedItem == null || string.IsNullOrWhiteSpace(jsonText))
            {
                return null;
            }

            try
            {
                return DeserializeUserData(jsonText, selectedItem.Type);
            }
            catch
            {
                return null;
            }
        }

        private void SetJsonWithoutNotify(string value, bool resetBaseline = true)
        {
            jsonText = value ?? string.Empty;
            if (resetBaseline)
            {
                lastLoadedJson = jsonText;
            }

            jsonScrollPosition = Vector2.zero;
            jsonEditSessionHasUndoSnapshot = false;
            jsonContainer?.MarkDirtyRepaint();
            UpdateSelectedLabels();
        }

        private void DrawJsonEditor()
        {
            jsonTextAreaStyle ??= CreateJsonTextAreaStyle();

            EditorGUI.BeginDisabledGroup(isBusy || selectedItem == null);
            jsonScrollPosition = EditorGUILayout.BeginScrollView(
                jsonScrollPosition,
                GUILayout.ExpandWidth(true),
                GUILayout.ExpandHeight(true));

            EditorGUI.BeginChangeCheck();
            var availableHeight = jsonContainer != null ? jsonContainer.contentRect.height - 20f : 200f;
            var lineCount = string.IsNullOrEmpty(jsonText) ? 1 : jsonText.Count(c => c == '\n') + 1;
            var contentHeight = Mathf.Max(200f, availableHeight, lineCount * EditorGUIUtility.singleLineHeight + 32f);
            var nextValue = EditorGUILayout.TextArea(
                jsonText ?? string.Empty,
                jsonTextAreaStyle,
                GUILayout.ExpandWidth(true),
                GUILayout.Height(contentHeight));

            if (EditorGUI.EndChangeCheck())
            {
                if (!jsonEditSessionHasUndoSnapshot)
                {
                    RecordUndoSnapshot();
                    jsonEditSessionHasUndoSnapshot = true;
                }

                jsonText = nextValue;
                if (activePane == EditorPane.Json)
                {
                    currentDataObject = null;
                    BuildObjectEditor();
                }

                UpdateSelectedLabels();
            }

            EditorGUILayout.EndScrollView();
            EditorGUI.EndDisabledGroup();
        }

        private static GUIStyle CreateJsonTextAreaStyle()
        {
            var style = new GUIStyle(EditorStyles.textArea)
            {
                wordWrap = false,
                richText = false,
                padding = new RectOffset(10, 10, 9, 9),
                stretchHeight = false
            };

            if (EditorGUIUtility.isProSkin)
            {
                style.normal.background = GetJsonTextAreaBackground();
                style.focused.background = style.normal.background;
                style.hover.background = style.normal.background;
                style.active.background = style.normal.background;
                style.onNormal.background = style.normal.background;
                style.onFocused.background = style.normal.background;
                style.onHover.background = style.normal.background;
                style.onActive.background = style.normal.background;
                style.normal.textColor = new Color(0.9f, 0.94f, 1f);
                style.focused.textColor = style.normal.textColor;
                style.hover.textColor = style.normal.textColor;
                style.active.textColor = style.normal.textColor;
                style.onNormal.textColor = style.normal.textColor;
                style.onFocused.textColor = style.normal.textColor;
                style.onHover.textColor = style.normal.textColor;
                style.onActive.textColor = style.normal.textColor;
            }

            return style;
        }

        private static Texture2D GetJsonTextAreaBackground()
        {
            if (jsonTextAreaBackground != null)
            {
                return jsonTextAreaBackground;
            }

            jsonTextAreaBackground = new Texture2D(1, 1)
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            jsonTextAreaBackground.SetPixel(0, 0, new Color(0.105f, 0.113f, 0.133f));
            jsonTextAreaBackground.Apply();
            return jsonTextAreaBackground;
        }

        private void UpdateSelectedLabels()
        {
            var hasSelection = selectedItem != null;
            selectedTypeLabel.text = hasSelection ? selectedItem.Type.Name : "Select a data type";
            selectedKeyLabel.text = hasSelection ? selectedItem.Key : string.Empty;

            if (!hasSelection)
            {
                selectedStatusLabel.text = string.Empty;
                selectedStatusLabel.tooltip = string.Empty;
            }
            else
            {
                var dirty = jsonText != lastLoadedJson;
                selectedStatusLabel.text = selectedItem.HasSavedData
                    ? dirty ? "Saved + edits" : "Saved"
                    : dirty ? "New + edits" : "New";
                selectedStatusLabel.tooltip = selectedItem.HasSavedData
                    ? dirty ? "This data exists in storage and has unsaved edits in the editor." : "This data exists in storage for the selected profile."
                    : dirty ? "This data has not been saved yet and has unsaved edits in the editor." : "This data has not been saved yet for the selected profile.";
            }

            UpdateButtonStates();
        }

        private void UpdateButtonStates()
        {
            var hasSelection = selectedItem != null;
            refreshButton?.SetEnabled(!isBusy);
            openFolderButton?.SetEnabled(!isBusy && GetProviderConfig().ProviderType == StorageProviderType.FileBased);
            profileDropdown?.SetEnabled(!isBusy);
            savedOnlyToggle?.SetEnabled(!isBusy);
            loadButton?.SetEnabled(!isBusy && hasSelection);
            saveButton?.SetEnabled(!isBusy && hasSelection);
            deleteButton?.SetEnabled(!isBusy && hasSelection && selectedItem.HasSavedData);
            undoButton?.SetEnabled(!isBusy && CanRestoreSnapshot(undoHistory));
            redoButton?.SetEnabled(!isBusy && CanRestoreSnapshot(redoHistory));
            formatButton?.SetEnabled(!isBusy && hasSelection && activePane == EditorPane.Json);
            applyJsonButton?.SetEnabled(!isBusy && hasSelection && activePane == EditorPane.Json);
            dataTypeList?.SetEnabled(!isBusy);
            jsonContainer?.SetEnabled(!isBusy && hasSelection);
        }

        private async UniTask RunBusyAsync(Func<UniTask> action)
        {
            if (isBusy)
            {
                return;
            }

            isBusy = true;
            UpdateButtonStates();
            try
            {
                await action();
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                SetStatus(ex.Message, true);
            }
            finally
            {
                isBusy = false;
                UpdateButtonStates();
            }
        }

        private void SetStatus(string message, bool isError)
        {
            if (statusLabel == null)
            {
                return;
            }

            statusLabel.text = message;
            statusLabel.EnableInClassList("local-save-viewer__status--error", isError);
        }

        private void OpenSaveFolder()
        {
            var providerConfig = GetProviderConfig();
            if (providerConfig.ProviderType != StorageProviderType.FileBased)
            {
                SetStatus("The selected provider stores data in PlayerPrefs.", false);
                return;
            }

            var rootPath = GetFileBasedRootPath(providerConfig);
            Directory.CreateDirectory(rootPath);
            EditorUtility.RevealInFinder(rootPath);
        }

        private StorageProviderConfig GetProviderConfig()
        {
            return config?.PrimaryProvider ?? StorageProviderConfig.DefaultFileBased();
        }

        private string GetStorageLocationLabel(StorageProviderConfig providerConfig)
        {
            return providerConfig.ProviderType == StorageProviderType.FileBased
                ? GetFileBasedRootPath(providerConfig)
                : "Unity PlayerPrefs";
        }

        private static string GetFileBasedRootPath(StorageProviderConfig providerConfig)
        {
            var folderName = string.IsNullOrEmpty(providerConfig.SaveDataFolderName)
                ? "SaveData"
                : providerConfig.SaveDataFolderName;
            return Path.Combine(Application.persistentDataPath, folderName);
        }

        private IEnumerable<string> ScanFileBasedProfiles()
        {
            var providerConfig = GetProviderConfig();
            if (providerConfig.ProviderType != StorageProviderType.FileBased)
            {
                yield break;
            }

            var rootPath = GetFileBasedRootPath(providerConfig);
            if (!Directory.Exists(rootPath))
            {
                yield break;
            }

            foreach (var directory in Directory.EnumerateDirectories(rootPath))
            {
                var profileId = Path.GetFileName(directory);
                if (!string.IsNullOrEmpty(profileId) && profileId != FileGlobalFolderName)
                {
                    yield return profileId;
                }
            }
        }

        private IEnumerable<string> ScanFileBasedDataKeys(string profileId)
        {
            var providerConfig = GetProviderConfig();
            if (providerConfig.ProviderType != StorageProviderType.FileBased)
            {
                yield break;
            }

            var profilePath = Path.Combine(GetFileBasedRootPath(providerConfig), profileId);
            if (!Directory.Exists(profilePath))
            {
                yield break;
            }

            foreach (var file in Directory.EnumerateFiles(profilePath, "*.json", SearchOption.TopDirectoryOnly))
            {
                var key = Path.GetFileNameWithoutExtension(file);
                if (IsUserDataKey(key))
                {
                    yield return key;
                }
            }
        }

        private static T LoadEditorAsset<T>(string defaultPath, string assetName, string typeFilter) where T : UnityEngine.Object
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(defaultPath);
            if (asset != null)
            {
                return asset;
            }

            var assetNameWithoutExtension = Path.GetFileNameWithoutExtension(assetName);
            var paths = AssetDatabase.FindAssets($"{assetNameWithoutExtension} t:{typeFilter}")
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(path => path.Replace('\\', '/'))
                .Where(path => path.EndsWith(assetName, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(path => path.IndexOf(EditorAssetFolderSuffix, StringComparison.OrdinalIgnoreCase) >= 0);

            foreach (var path in paths)
            {
                asset = AssetDatabase.LoadAssetAtPath<T>(path);
                if (asset != null)
                {
                    return asset;
                }
            }

            return null;
        }

        private static LocalSaveConfig FindLocalSaveConfig()
        {
            var gdkConfig = Resources.Load<GDKConfig>("GameConfigs/GDKConfig");
            if (gdkConfig != null && gdkConfig.HasGameConfig<LocalSaveConfig>())
            {
                return gdkConfig.GetGameConfig<LocalSaveConfig>();
            }

            var guid = AssetDatabase.FindAssets("t:LocalSaveConfig").FirstOrDefault();
            if (string.IsNullOrEmpty(guid))
            {
                return null;
            }

            return AssetDatabase.LoadAssetAtPath<LocalSaveConfig>(AssetDatabase.GUIDToAssetPath(guid));
        }

        private static IEnumerable<Type> GetLoadableTypes(Assembly assembly)
        {
            try
            {
                return assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException ex)
            {
                return ex.Types.Where(type => type != null);
            }
            catch
            {
                return Array.Empty<Type>();
            }
        }

        private static bool MatchesSearch(DataTypeItem item, string query)
        {
            return item.Type.Name.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0
                   || item.Key.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0
                   || (item.Type.Namespace?.IndexOf(query, StringComparison.OrdinalIgnoreCase) ?? -1) >= 0;
        }

        private static string DataKeyOf(Type type) => $"{UserDataPrefix}{type.Name}";

        private static bool IsUserDataKey(string key)
        {
            return !string.IsNullOrEmpty(key)
                   && key.StartsWith(UserDataPrefix, StringComparison.Ordinal)
                   && key != ProfileMetadataKey
                   && key != ProfileRegistryKey;
        }

        private static bool ShouldEncrypt(string key)
        {
            return key != ProfileRegistryKey && key != ProfileMetadataKey;
        }

        private sealed class DataTypeItem
        {
            public Type Type { get; set; }
            public string Key { get; set; }
            public bool HasSavedData { get; set; }
        }

        private sealed class HistoryEntry
        {
            public string ProfileId { get; set; }
            public string DataKey { get; set; }
            public string TypeName { get; set; }
            public string Json { get; set; }
        }

        private enum EditorPane
        {
            Object,
            Json
        }

        private sealed class DataMember
        {
            private readonly Func<object, object> getter;
            private readonly Action<object, object> setter;

            public DataMember(string name, Type valueType, bool canWrite, Func<object, object> getter, Action<object, object> setter)
            {
                this.Name = name;
                this.ValueType = valueType;
                this.CanWrite = canWrite;
                this.getter = getter;
                this.setter = setter;
            }

            public string Name { get; }
            public Type ValueType { get; }
            public bool CanWrite { get; }

            public object GetValue(object owner)
            {
                return this.getter(owner);
            }

            public void SetValue(object owner, object value)
            {
                this.setter(owner, value);
            }
        }

        private sealed class EditorLogService : ILogService
        {
            public void Log(string logContent, LogLevel logLevel = LogLevel.LOG)
            {
                switch (logLevel)
                {
                    case LogLevel.WARNING:
                        Debug.LogWarning(logContent);
                        break;
                    case LogLevel.ERROR:
                        Debug.LogError(logContent);
                        break;
                    case LogLevel.EXCEPTION:
                        Debug.LogError(logContent);
                        break;
                    default:
                        Debug.Log(logContent);
                        break;
                }
            }

            public void LogWithColor(string logContent, Color? c = null) => Debug.Log(logContent);
            public void Warning(string logContent) => Debug.LogWarning(logContent);
            public void Error(string logContent) => Debug.LogError(logContent);
            public void Exception(Exception exception) => Debug.LogException(exception);

            public void Exception(Exception exception, string message)
            {
                Debug.LogError(message);
                Debug.LogException(exception);
            }
        }
    }
}
