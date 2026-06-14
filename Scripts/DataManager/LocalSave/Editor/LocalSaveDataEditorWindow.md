# Local Save Data Editor Window

## Purpose

`LocalSaveDataEditorWindow` is a reusable Unity Editor tool for inspecting and editing local save data owned by the `DataManager.LocalSave` module in `com.gdk.core`.

The tool is intentionally package-level and project-agnostic. It discovers `IUserData` implementations from loaded assemblies, loads their saved JSON through the local save provider, deserializes the JSON into the matching runtime model type, and lets developers edit data from either a structured Object View or a raw JSON View.

Open it from either menu path:

- `GDK/Local Save Viewer`
- `Tools/GDK/Local Save Viewer`

## Files

- `LocalSaveDataEditorWindow.cs`: editor logic, local save integration, dynamic object inspector, JSON editor, undo/redo history.
- `LocalSaveDataEditorWindow.uxml`: static UI structure and named query targets.
- `LocalSaveDataEditorWindow.uss`: visual styling for the window.
- `LocalSaveDataEditorWindow.md`: this context document.

## High-Level Flow

1. `CreateGUI()` loads UXML/USS, discovers data types, and starts a refresh.
2. `BuildViewFromUxml()` queries named controls from UXML and wires callbacks.
3. `RefreshAsync()` resolves `LocalSaveConfig`, builds the storage provider, loads profiles, scans saved keys, and refreshes the type list.
4. Selecting a data type auto-loads it through `LoadSelectedJsonAsync()`.
5. JSON is deserialized into `currentDataObject` using the selected `IUserData` type.
6. `BuildObjectEditor()` renders public editable members in Object View.
7. Edits update the object, then `SyncJsonFromCurrentObject()` keeps JSON View and dirty state in sync.
8. `SaveSelectedJsonAsync()` serializes and saves through the configured provider, including metadata updates.

## UI Architecture

The window follows the usual UI Toolkit split:

- UXML owns static layout and element names.
- USS owns styling using `local-save-viewer__...` classes.
- C# owns dynamic data, reflection, and all callback wiring.

The JSON editor is an `IMGUIContainer` instead of a UI Toolkit multiline `TextField`. This is deliberate: large local save JSON is easier to scroll and edit reliably with `EditorGUILayout.BeginScrollView()` and `EditorGUILayout.TextArea()`.

UXML and USS are loaded from the default package paths first:

- `Packages/com.gdk.core/Scripts/DataManager/LocalSave/Editor/LocalSaveDataEditorWindow.uxml`
- `Packages/com.gdk.core/Scripts/DataManager/LocalSave/Editor/LocalSaveDataEditorWindow.uss`

If those paths do not resolve, `LoadEditorAsset<T>()` falls back to `AssetDatabase.FindAssets()` by asset name and type. Keep this fallback when moving or embedding the package in another project.

## Data Discovery

`DiscoverDataTypes()` scans all loaded assemblies for concrete `IUserData` types. It skips interfaces, abstract types, open generic types, and the module's system data types such as `ProfileMetadata` and `ProfileRegistry`.

Each discovered type is mapped to its local save key with:

```csharp
LD-{TypeName}
```

where `LD-` comes from `HandleLocalDataServices.UserDataPrefix`.

Saved state is detected from profile metadata and, for file-based providers, by scanning the profile folder for matching JSON files.

## Object View

Object View is a reflection-driven editor for the deserialized object graph.

Important entry points:

- `BuildObjectEditor()` clears and rebuilds the Object View root.
- `GetEditableMembers()` returns public instance fields and public gettable properties, excluding `[JsonIgnore]` members.
- `CreateValueEditor()` chooses the correct editor for each value.
- `CommitMemberValue()` writes edited values back to the owner and syncs JSON.

Value rendering rules:

- Simple values render as fields: strings, booleans, numeric values, `DateTime`, and enums.
- Dictionaries render as foldouts with add/remove controls and search.
- Lists and arrays render as foldouts with add/remove controls and search.
- Custom classes and structs render as nested foldouts up to `MaxStructuredObjectDepth`.
- Unsupported values fall back to an editable member-level JSON block.

The current depth limit is:

```csharp
private const int MaxStructuredObjectDepth = 8;
```

Large collections are capped in Object View:

```csharp
private const int MaxCollectionItemsInObjectView = 200;
```

When a collection has more matching entries than the cap, Object View shows the first matching items and asks the user to refine the search or use JSON View.

## Nested Objects, Lists, And Dictionaries

Nested custom data should display as foldouts rather than one giant JSON block. For example, a field like `List<Preset> AllPresets` should render like:

```text
AllPresets (List<Preset>)
  [0] (Preset)
    Name (String)
    Items (Dictionary<String, BackpackItemData>)
      item-key (BackpackItemData)
        Id (String)
        BlueprintId (String)
        ...
```

The renderer also handles interface-typed collections where possible:

- `IList<T>` and `IReadOnlyList<T>` are treated as list-like when a concrete value is available or when a null value can be created as `List<T>`.
- `IDictionary<TKey, TValue>` and `IReadOnlyDictionary<TKey, TValue>` are treated as dictionary-like when a concrete value is available or when a null value can be created as `Dictionary<TKey, TValue>`.

Do not assume dictionary enumeration yields `DictionaryEntry`. Generic dictionaries usually enumerate as `KeyValuePair<TKey, TValue>`. Use `EnumerateDictionary()` and `TryReadKeyValuePair()` for dictionary display logic.

For value types inside nested objects or collections, edits must be propagated back to the parent object or parent collection. The `ownerChanged` callback path in `AddMemberEditor()` exists for this reason. Be careful when changing this area, because boxed structs can otherwise appear to edit successfully while the parent object keeps the old value.

## JSON View

JSON View is the raw editing escape hatch.

Actions:

- `Format JSON`: validates and reformats the current JSON text.
- `Apply to Object`: parses the JSON into the selected `IUserData` object and rebuilds Object View. This does not save to disk.

Saving always goes through `SaveSelectedJsonAsync()`. If Object View is active, the current object is serialized first. If JSON View is active, JSON is parsed into the correct selected data type before saving.

## Undo And Redo

The tool has local in-window undo/redo based on JSON snapshots.

Important details:

- History is not Unity's global undo stack.
- History is scoped to selected profile, data key, and data type.
- History is cleared when changing selection.
- Undo/redo restores JSON, deserializes it, and rebuilds Object View.
- `Ctrl+Z`, `Ctrl+Y`, and `Ctrl+Shift+Z` are handled by `OnKeyDown()`.

This design keeps undo predictable for local save editing and avoids touching unrelated Unity objects.

## Storage And Profiles

The tool uses the same Local Save provider setup as runtime code:

- `LocalSaveConfig.PrimaryProvider` defines provider type and encryption.
- `StorageProviderFactory.Create()` creates file-based or PlayerPrefs providers.
- `AesEncryptionService` is used when provider encryption is enabled.
- Profile metadata is loaded and updated through `ProfileMetadataKey`.
- Profile registry data is loaded through `ProfileRegistryKey`.

The selected profile controls which saved data is loaded, saved, or deleted. File-based profiles are also discovered by scanning provider folders, which helps when metadata is incomplete or stale.

## Current Limitations

- Object View only shows public instance fields and public gettable properties. Private fields marked with Newtonsoft attributes may serialize but will not appear unless Object View support is expanded.
- Arbitrary abstract classes and non-collection interfaces cannot be created automatically.
- Read-only members can only be edited when the existing value can be populated or mutated safely.
- Huge collections are intentionally capped in Object View for editor responsiveness.
- JSON View remains the fallback for unsupported shapes, polymorphic edge cases, or very deep object graphs.
- Script changes made during Play Mode may not be visible until Unity exits Play Mode and recompiles editor assemblies.

## Extending The Tool

When adding features, keep the package reusable across projects that consume `com.gdk.core`.

Recommended workflow:

1. Add static controls to UXML and style them in USS.
2. Query new controls in `BuildViewFromUxml()`.
3. Wire callbacks in C# and keep dynamic content out of UXML.
4. If adding a new value editor, extend `CreateValueEditor()` and keep the JSON fallback intact.
5. If changing list or dictionary behavior, update `BuildListItems()`, `BuildDictionaryItems()`, and the helper methods near `GetListItemType()` / `GetDictionaryTypes()`.
6. Preserve `LoadEditorAsset<T>()` fallback behavior for package portability.
7. Avoid references to project-specific model classes, assets, scenes, or services.
8. Refresh Unity, check Console errors/exceptions, and run a focused smoke test.

Useful smoke checks:

- The window opens from both menu paths.
- Selecting a saved data type auto-loads it.
- Object View renders simple fields, nested custom objects, lists, arrays, and dictionaries.
- A nested list item does not fall back to JSON when it is a normal custom class or struct.
- Add/remove works for lists and dictionaries.
- JSON View can format, apply, save, and reload the same data.
- Undo/redo works before saving.
- No fresh Console errors or exceptions appear after the tool action.

## Troubleshooting

If clicking a data type appears to do nothing:

- Selection already auto-loads data. Use `Reload` only to force a storage reload.
- Check whether the selected profile actually has saved data.
- Check Console for provider, encryption, or JSON deserialization errors.

If Object View shows a large JSON block:

- Confirm the value type is supported by `CreateValueEditor()`.
- Confirm custom object types pass `IsPlainEditableObject()`.
- Confirm the type has public fields or public gettable properties.
- Confirm the object graph is not beyond `MaxStructuredObjectDepth`.

If dictionary entries throw cast exceptions:

- Use `EnumerateDictionary()` instead of directly casting entries to `DictionaryEntry`.
- Generic dictionaries enumerate `KeyValuePair<TKey, TValue>`.

If edits to nested struct values do not persist:

- Check the `ownerChanged` propagation path in `AddMemberEditor()`.
- Make sure the updated boxed value is committed back to the parent object, list, or dictionary.

If a new UI control does not respond:

- Confirm its UXML `name` matches the query in `BuildViewFromUxml()`.
- Confirm the USS class only controls styling and no logic is hidden in styling assumptions.
- Confirm callbacks are registered after `visualTree.CloneTree(rootVisualElement)`.
