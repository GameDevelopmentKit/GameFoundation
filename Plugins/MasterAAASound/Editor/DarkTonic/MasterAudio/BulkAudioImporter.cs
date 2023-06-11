using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace DarkTonic.MasterAudio.EditorScripts
{
    // ReSharper disable once CheckNamespace
    public class BulkAudioImporter : EditorWindow
    {
        private const string NoClipsSelected = "There are no clips selected.";
        private const string AllFoldersKey = "[All]";
        private const int MaxPageSize = 200;

        private readonly AudioInfoData _clipList = new AudioInfoData();

        private int _pageNumber;
        private bool isBulkMode;

        private List<AudioInformation> _filterClips;
        private List<AudioInformation> _filteredOut;
        private Vector2 _scrollPos;
        private Vector2 _outsideScrollPos;
        private readonly List<string> _folderPaths = new List<string>();
        private string _selectedFolderPath = AllFoldersKey;

        private readonly List<int> _sampleRates = new List<int> { 8000, 11025, 22050, 44100, 48000, 96000, 192000 };
        private readonly string[] _sampleRateDisplays = new[] { "8000", "11025", "22050", "44100", "48000", "96000", "192000" };

        [MenuItem("Window/Master Audio/Bulk Audio Importer")]
        // ReSharper disable once UnusedMember.Local
        static void Init()
        {
            var window = GetWindow(typeof(BulkAudioImporter));

#if UNITY_2019_3_OR_NEWER
        window.minSize = new Vector2(949, 610);
        window.maxSize = new Vector2(949, 610);
#else
            window.minSize= new Vector2(954, 610);
            window.maxSize = new Vector2(954, 610);
#endif
            window.minSize = window.maxSize;
        }

        // ReSharper disable once UnusedMember.Local
        // ReSharper disable once InconsistentNaming
        void OnGUI()
        {
            _outsideScrollPos = GUI.BeginScrollView(new Rect(0, 0, position.width, position.height), _outsideScrollPos, new Rect(0, 0, 936, 610));

            if (MasterAudioInspectorResources.BAILogoTexture != null)
            {
                DTGUIHelper.ShowHeaderTexture(MasterAudioInspectorResources.BAILogoTexture);
            }

            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            GUI.contentColor = DTGUIHelper.BrightButtonColor;
            if (GUILayout.Button(new GUIContent("Scan Project"), EditorStyles.toolbarButton, GUILayout.Width(100)))
            {
                BuildCache();
                EditorGUILayout.EndHorizontal();
                return;
            }

            GUILayout.Space(10);
            if (GUILayout.Button(new GUIContent("Revert Selected"), EditorStyles.toolbarButton, GUILayout.Width(100)))
            {
                RevertSelected();
                EditorGUILayout.EndHorizontal();
                return;
            }

            GUILayout.Space(10);
            if (GUILayout.Button(new GUIContent("Apply Selected"), EditorStyles.toolbarButton, GUILayout.Width(100)))
            {
                ApplySelected();
                EditorGUILayout.EndHorizontal();
                return;
            }
            GUILayout.Space(10);
            GUILayout.Label("Bulk Mode");
            isBulkMode = EditorGUILayout.Toggle("", isBulkMode, GUILayout.Width(30));

            GUILayout.Space(10);
            RevertColor();

            GUILayout.Label("Full Path Filter");
            var oldFilter = _clipList.SearchFilter;
            var newFilter = GUILayout.TextField(_clipList.SearchFilter, EditorStyles.toolbarTextField, GUILayout.Width(200));
            if (newFilter != oldFilter)
            {
                _clipList.SearchFilter = newFilter;
                RebuildFilteredList();
            }

            var myPosition = GUILayoutUtility.GetRect(10, 10, ToolbarSeachCancelButton);
            myPosition.x -= 5;
            if (GUI.Button(myPosition, "", ToolbarSeachCancelButton))
            {
                _clipList.SearchFilter = string.Empty;
                RebuildFilteredList();
            }

            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            if (!File.Exists(CacheFilePath))
            {
                DTGUIHelper.ShowColorWarning("Click 'Scan Project' to generate list of Audio Clips.");
                GUI.EndScrollView();
                return;
            }

            if (_clipList.AudioInfor.Count == 0 || _clipList.NeedsRefresh)
            {
                if (!LoadAndTranslateFile())
                {
                    GUI.EndScrollView();
                    return;
                }
            }

            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Folder");
            var selectedIndex = _folderPaths.IndexOf(_selectedFolderPath);
            var newIndex = EditorGUILayout.Popup(selectedIndex, _folderPaths.ToArray(), GUILayout.Width(800));
            if (newIndex != selectedIndex)
            {
                _selectedFolderPath = _folderPaths[newIndex];
                RebuildFilteredList();
            }
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            var totalClips = _clipList.AudioInfor.Count;
            var dynamicText = string.Format("{0}/{1} clips selected.", SelectedClips.Count, FilteredClips.Count);
            dynamicText += " Total clips: " + totalClips;

            double clipCount = totalClips;
            if (_filteredOut != null)
            {
                clipCount = _filteredOut.Count;
            }

            var pageCount = (int)Math.Ceiling(clipCount / MaxPageSize);

            var pageNames = new string[pageCount];
            var pageNums = new int[pageCount];
            for (var i = 0; i < pageCount; i++)
            {
                pageNames[i] = "Page " + (i + 1);
                pageNums[i] = i;
            }


            EditorGUILayout.LabelField(dynamicText);

            var oldPage = _pageNumber;

            EditorGUILayout.BeginHorizontal();
            _pageNumber = EditorGUILayout.IntPopup("", _pageNumber, pageNames, pageNums, GUILayout.Width(100));
            if (oldPage != _pageNumber)
            {
                RebuildFilteredList(true);
            }
            GUILayout.Label("of " + pageCount);

            EditorGUILayout.EndHorizontal();

            // display
            DisplayClips();

            GUI.EndScrollView();
        }

        private void RebuildFilteredList(bool keepPageNumber = false)
        {
            if (!keepPageNumber)
            {
                _pageNumber = 0;
            }

            _filterClips = null;
            _filteredOut = null;
        }

        private bool LoadAndTranslateFile()
        {
            XmlDocument xFiles;
            try
            {
                xFiles = new XmlDocument();
                xFiles.Load(CacheFilePath);
            }
            catch
            {
                DTGUIHelper.ShowRedError("Cache file is malformed. Click 'Scan Project' to regenerate it.");
                return false;
            }

            if (_clipList.AudioInfor.Count == 0)
            {
                _clipList.AudioInfor.Clear();
            }

            // translate
            var success = TranslateFromXml(xFiles);
            if (!success)
            {
                return false;
            }

            return true;
        }

        private void ApplySelected()
        {
            if (SelectedClips.Count == 0)
            {
                DTGUIHelper.ShowAlert(NoClipsSelected);
                return;
            }

            foreach (var aClip in SelectedClips)
            {
                ApplyClipChanges(aClip, false);
                aClip.HasChanged = true;
            }

            _clipList.NeedsRefresh = true;

            WriteFile(_clipList);
        }

        private void RevertSelected()
        {
            if (SelectedClips.Count == 0)
            {
                DTGUIHelper.ShowAlert(NoClipsSelected);
                return;
            }

            foreach (var aClip in SelectedClips)
            {
                RevertChanges(aClip);
            }
        }

        private List<AudioInformation> SelectedClips {
            get {
                var selected = new List<AudioInformation>();

                foreach (var t in FilteredClips)
                {
                    if (!t.IsSelected)
                    {
                        continue;
                    }

                    selected.Add(t);
                }

                return selected;
            }
        }

        private List<AudioInformation> FilteredClips {
            get {
                if (_filterClips != null)
                {
                    return _filterClips;
                }

                _filterClips = new List<AudioInformation>();

                if (!string.IsNullOrEmpty(_clipList.SearchFilter))
                {
                    if (_filteredOut == null)
                    {
                        _filteredOut = new List<AudioInformation>();
                        _filteredOut.AddRange(_clipList.AudioInfor);
                    }

                    _filteredOut.RemoveAll(delegate (AudioInformation obj)
                    {
                        return !obj.FullPath.ToLower().Contains(_clipList.SearchFilter.ToLower());
                    });
                }

                if (_selectedFolderPath != AllFoldersKey)
                {
                    if (_filteredOut == null)
                    {
                        _filteredOut = new List<AudioInformation>();
                        _filteredOut.AddRange(_clipList.AudioInfor);
                    }

                    _filteredOut.RemoveAll(delegate (AudioInformation obj)
                    {
                        // ReSharper disable once StringLastIndexOfIsCultureSpecific.1
                        var index = obj.FullPath.ToLower().LastIndexOf(_selectedFolderPath.ToLower());
                        if (index <= -1)
                        {
                            return
                                !obj.FullPath.ToLower()
                                    .Contains(_selectedFolderPath.ToLower());
                        }
                        var endPart = obj.FullPath.Substring(index + _selectedFolderPath.Length + 1);
                        if (endPart.Contains("/"))
                        {
                            return true; // don't show sub-folders
                        }
                        return !obj.FullPath.ToLower().Contains(_selectedFolderPath.ToLower());
                    });
                }

                var arrayToAddFrom = _clipList.AudioInfor;
                if (_filteredOut != null)
                {
                    arrayToAddFrom = _filteredOut;
                }

                var firstResultNum = MaxPageSize * _pageNumber;
                var lastResultNum = firstResultNum + MaxPageSize - 1;
                if (lastResultNum > arrayToAddFrom.Count)
                {
                    lastResultNum = arrayToAddFrom.Count;
                }

                if (arrayToAddFrom.Count > 0)
                {
                    var isAsc = _clipList.SortDir == ClipSortDirection.Ascending;

                    arrayToAddFrom.Sort(delegate (AudioInformation x, AudioInformation y)
                    {
                        switch (_clipList.SortColumn)
                        {
                            case ClipSortColumn.Name:
                                if (isAsc)
                                {
                                    return x.Name.CompareTo(y.Name);
                                }
                                return y.Name.CompareTo(x.Name);
                            case ClipSortColumn.ForceMono:
                                if (isAsc)
                                {
                                    return x.ForceMono.CompareTo(y.ForceMono);
                                }
                                return y.ForceMono.CompareTo(x.ForceMono);
                            case ClipSortColumn.LoadInBackground:
                                if (isAsc)
                                {
                                    return x.LoadBG.CompareTo(y.LoadBG);
                                }
                                return y.LoadBG.CompareTo(x.LoadBG);
                            case ClipSortColumn.PreloadAudio:
                                if (isAsc)
                                {
                                    return x.Preload.CompareTo(y.Preload);
                                }
                                return y.Preload.CompareTo(x.Preload);
                            case ClipSortColumn.LoadType:
                                if (isAsc)
                                {
                                    return x.LoadType.CompareTo(y.LoadType);
                                }
                                return y.LoadType.CompareTo(x.LoadType);
                            case ClipSortColumn.CompressionFormat:
                                if (isAsc)
                                {
                                    return x.CompressionFormat.CompareTo(y.CompressionFormat);
                                }
                                return y.CompressionFormat.CompareTo(x.CompressionFormat);
                            case ClipSortColumn.Quality:
                                if (isAsc)
                                {
                                    return x.Quality.CompareTo(y.Quality);
                                }
                                return y.Quality.CompareTo(x.Quality);
                            case ClipSortColumn.SampleRateSetting:
                                if (isAsc)
                                {
                                    return x.SampleRateSetting.CompareTo(y.SampleRateSetting);
                                }
                                return y.SampleRateSetting.CompareTo(x.SampleRateSetting);
                            case ClipSortColumn.SampleRate:
                                if (isAsc)
                                {
                                    return x.SampleRateOverride.CompareTo(y.SampleRateOverride);
                                }
                                return y.SampleRateOverride.CompareTo(x.SampleRateOverride);
                        }

                        return x.Name.CompareTo(y.Name);
                    });
                }

                // de-select filtered out clips 
                foreach (var aClip in _clipList.AudioInfor)
                {
                    if (!arrayToAddFrom.Contains(aClip))
                    {
                        aClip.IsSelected = false;
                    }
                }

                for (var i = firstResultNum; i < lastResultNum; i++)
                {
                    _filterClips.Add(arrayToAddFrom[i]);
                }

                return _filterClips;
            }
        }

        private void ChangeSortColumn(ClipSortColumn col)
        {
            var oldCol = _clipList.SortColumn;
            _clipList.SortColumn = col;
            if (oldCol != _clipList.SortColumn)
            {
                _clipList.SortDir = ClipSortDirection.Ascending;
            }
            else
            {
                _clipList.SortDir = _clipList.SortDir == ClipSortDirection.Ascending ? ClipSortDirection.Descending : ClipSortDirection.Ascending;
            }

            RebuildFilteredList();
        }

        private string ColumnPrefix(ClipSortColumn col)
        {
            if (col != _clipList.SortColumn)
            {
                return " ";
            }

            return _clipList.SortDir == ClipSortDirection.Ascending ? DTGUIHelper.UpArrow : DTGUIHelper.DownArrow;
        }

        private void DisplayClips()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            GUI.contentColor = DTGUIHelper.BrightButtonColor;
            if (GUILayout.Button("All", EditorStyles.toolbarButton, GUILayout.Width(32)))
            {
                foreach (var t in FilteredClips)
                {
                    t.IsSelected = true;
                }
            }

            if (GUILayout.Button("None", EditorStyles.toolbarButton, GUILayout.Width(36)))
            {
                foreach (var t in _clipList.AudioInfor)
                {
                    t.IsSelected = false;
                }
            }

            GUI.contentColor = DTGUIHelper.BrightButtonColor;
            var columnPrefix = ColumnPrefix(ClipSortColumn.Name);
            if (GUILayout.Button(new GUIContent(columnPrefix + "Clip Name", "Click to sort by Clip Name"), EditorStyles.toolbarButton, GUILayout.Width(160)))
            {
                ChangeSortColumn(ClipSortColumn.Name);
            }

            columnPrefix = ColumnPrefix(ClipSortColumn.ForceMono);
            if (GUILayout.Button(new GUIContent(columnPrefix + "Force Mono", "Click to sort by Force Mono"), EditorStyles.toolbarButton, GUILayout.Width(86)))
            {
                ChangeSortColumn(ClipSortColumn.ForceMono);
            }

            columnPrefix = ColumnPrefix(ClipSortColumn.LoadInBackground);
            if (GUILayout.Button(new GUIContent(columnPrefix + "Load In BG", "Click to sort by Load In BG"), EditorStyles.toolbarButton, GUILayout.Width(80)))
            {
                ChangeSortColumn(ClipSortColumn.LoadInBackground);
            }

            columnPrefix = ColumnPrefix(ClipSortColumn.PreloadAudio);
            if (GUILayout.Button(new GUIContent(columnPrefix + "Preload Aud.", "Click to sort by Preload Audio Data"), EditorStyles.toolbarButton, GUILayout.Width(90)))
            {
                ChangeSortColumn(ClipSortColumn.PreloadAudio);
            }

            columnPrefix = ColumnPrefix(ClipSortColumn.LoadType);
            if (GUILayout.Button(new GUIContent(columnPrefix + "Load Type", "Click to sort by Load Type"), EditorStyles.toolbarButton, GUILayout.Width(90)))
            {
                ChangeSortColumn(ClipSortColumn.LoadType);
            }

            columnPrefix = ColumnPrefix(ClipSortColumn.CompressionFormat);
            if (GUILayout.Button(new GUIContent(columnPrefix + "Comp. Format", "Click to sort by Compression Format"), EditorStyles.toolbarButton, GUILayout.Width(96)))
            {
                ChangeSortColumn(ClipSortColumn.CompressionFormat);
            }

            columnPrefix = ColumnPrefix(ClipSortColumn.Quality);
            if (GUILayout.Button(new GUIContent(columnPrefix + "Quality", "Click to sort by Quality"), EditorStyles.toolbarButton, GUILayout.Width(65)))
            {
                ChangeSortColumn(ClipSortColumn.Quality);
            }

            columnPrefix = ColumnPrefix(ClipSortColumn.SampleRateSetting);
            if (GUILayout.Button(new GUIContent(columnPrefix + "Sample Rt. Setting", "Click to sort by Sample Rate Setting"), EditorStyles.toolbarButton, GUILayout.Width(122)))
            {
                ChangeSortColumn(ClipSortColumn.SampleRateSetting);
            }

            columnPrefix = ColumnPrefix(ClipSortColumn.SampleRate);
            if (GUILayout.Button(new GUIContent(columnPrefix + "Sample Rate", "Click to sort by Sample Rate"), EditorStyles.toolbarButton, GUILayout.Width(90)))
            {
                ChangeSortColumn(ClipSortColumn.SampleRate);
            }

            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            if (FilteredClips.Count == 0)
            {
                DTGUIHelper.ShowLargeBarAlert("You have filtered all clips out.");
                return;
            }

#if UNITY_2019_3_OR_NEWER
        _scrollPos = GUI.BeginScrollView(new Rect(0, 137, 947, 475), _scrollPos, new Rect(0, 138, 880, 24 * FilteredClips.Count - 2));
#else
            _scrollPos = GUI.BeginScrollView(new Rect(0, 123, 953, 485), _scrollPos, new Rect(0, 124, 880, 24 * FilteredClips.Count + 4));
#endif

            foreach (var aClip in FilteredClips)
            {
                // ReSharper disable once ConvertIfStatementToConditionalTernaryExpression
                if (aClip.IsSelected)
                {
                    GUI.backgroundColor = DTGUIHelper.BrightButtonColor;
                }
                else
                {
                    GUI.backgroundColor = Color.white;
                }

                GUIStyle style = new GUIStyle(EditorStyles.miniButtonMid)
                {
                    fixedHeight = 22
                };
                EditorGUILayout.BeginHorizontal(style); // miniButtonMid, numberField, textField
                EditorGUILayout.BeginHorizontal();

                var wasSelected = aClip.IsSelected;
                aClip.IsSelected = GUILayout.Toggle(aClip.IsSelected, "");

                if (aClip.IsSelected)
                {
                    if (!wasSelected)
                    {
                        SelectClip(aClip);
                    }
                }

                var usesSampleRateOverride = aClip.SampleRateSetting == AudioSampleRateSetting.OverrideSampleRate;
                var usesQuality = aClip.CompressionFormat == AudioCompressionFormat.Vorbis;

                var isMonoChanged = !aClip.OrigForceMono.Equals(aClip.ForceMono);
                var isLoadInBGChanged = !aClip.OrigLoadBG.Equals(aClip.LoadBG);
                var isLoadTypeChanged = !aClip.OrigLoadType.Equals(aClip.LoadType);
                var isPreloadAudioChanged = !aClip.OrigPreload.Equals(aClip.Preload);
                var isCompFormatChanged = !aClip.OrigCompressionFormat.Equals(aClip.CompressionFormat);
                var isSampleRateSettingChanged = !aClip.OrigSampleRateSetting.Equals(aClip.SampleRateSetting);
                var isQualityChanged = usesQuality && !aClip.OrigQuality.Equals(aClip.Quality);
                var isSampleRateChanged = usesSampleRateOverride && !aClip.OrigSampleRateOverride.Equals(aClip.SampleRateOverride);

                var hasChanged = isMonoChanged || isLoadInBGChanged || isPreloadAudioChanged || isLoadTypeChanged || isCompFormatChanged || isSampleRateSettingChanged || isQualityChanged || isSampleRateChanged;

                if (!hasChanged)
                {
                    ShowDisabledColors();
                }
                else
                {
                    GUI.contentColor = DTGUIHelper.BrightButtonColor;
                }
                if (GUILayout.Button(new GUIContent("Revert"), EditorStyles.toolbarButton, GUILayout.Width(45)))
                {
                    if (!hasChanged)
                    {
                        DTGUIHelper.ShowAlert("This clip's properties have not changed.");
                    }
                    else
                    {
                        RevertChanges(aClip);
                    }
                }

                RevertColor();

                GUILayout.Space(10);
                GUILayout.Label(new GUIContent(aClip.Name, aClip.FullPath), GUILayout.Width(150));

                GUILayout.Space(28);
                MaybeShowChangedColors(isMonoChanged);
                var newMono = GUILayout.Toggle(aClip.ForceMono, "", GUILayout.Width(40));
                if (newMono != aClip.ForceMono)
                {
                    SelectClip(aClip);
                    aClip.IsSelected = true;
                    if (isBulkMode)
                    {
                        CopyForceMonoToSelected(newMono);
                    }
                    else
                    {
                        aClip.ForceMono = newMono;
                    }
                }
                RevertColor();

                GUILayout.Space(42);
                MaybeShowChangedColors(isLoadInBGChanged);
                var newLoadBG = GUILayout.Toggle(aClip.LoadBG, "", GUILayout.Width(40));
                if (newLoadBG != aClip.LoadBG)
                {
                    aClip.IsSelected = true;
                    SelectClip(aClip);
                    if (isBulkMode)
                    {
                        CopyLoadInBGToSelected(newLoadBG);
                    }
                    else
                    {
                        aClip.LoadBG = newLoadBG;
                    }
                }
                RevertColor();

                GUILayout.Space(40);
                MaybeShowChangedColors(isPreloadAudioChanged);
                var newPreload = GUILayout.Toggle(aClip.Preload, "", GUILayout.Width(40));
                if (newPreload != aClip.Preload)
                {
                    aClip.IsSelected = true;
                    SelectClip(aClip);
                    if (isBulkMode)
                    {
                        CopyPreloadAudioToSelected(newPreload);
                    }
                    else
                    {
                        aClip.Preload = newPreload;
                    }
                }
                RevertColor();

                GUILayout.Space(12);
                MaybeShowChangedColors(isLoadTypeChanged);
                var newLoad = (AudioClipLoadType)EditorGUILayout.EnumPopup(aClip.LoadType, GUILayout.Width(84));
                if (newLoad != aClip.LoadType)
                {
                    aClip.IsSelected = true;
                    SelectClip(aClip);
                    if (isBulkMode)
                    {
                        CopyLoadTypeToSelected(newLoad);
                    }
                    else
                    {
                        aClip.LoadType = newLoad;
                    }
                }
                RevertColor();

                GUILayout.Space(8);
                MaybeShowChangedColors(isCompFormatChanged);
                var newComp = (AudioCompressionFormat)EditorGUILayout.EnumPopup(aClip.CompressionFormat, GUILayout.Width(82));
                if (newComp != aClip.CompressionFormat)
                {
                    aClip.IsSelected = true;
                    SelectClip(aClip);
                    if (isBulkMode)
                    {
                        CopyCompFormatToSelected(newComp);
                    }
                    else
                    {
                        aClip.CompressionFormat = newComp;
                    }
                }
                RevertColor();

                GUILayout.Space(8);
                if (usesQuality)
                {
                    MaybeShowChangedColors(isQualityChanged);
                    var newQuality = EditorGUILayout.FloatField(aClip.Quality, GUILayout.Width(57));
                    newQuality = Math.Max(0f, newQuality);
                    newQuality = Math.Min(1f, newQuality);

                    if (newQuality != aClip.Quality)
                    {
                        aClip.IsSelected = true;
                        SelectClip(aClip);
                        if (isBulkMode)
                        {
                            CopyQualityToSelected(newQuality);
                        }
                        else
                        {
                            aClip.Quality = newQuality;
                        }
                    }
                    RevertColor();
                }
                else
                {
                    GUILayout.Space(61);
                }

                GUILayout.Space(4);
                MaybeShowChangedColors(isSampleRateSettingChanged);
                var newSample = (AudioSampleRateSetting)EditorGUILayout.EnumPopup(aClip.SampleRateSetting, GUILayout.Width(112));
                if (newSample != aClip.SampleRateSetting)
                {
                    aClip.IsSelected = true;
                    SelectClip(aClip);
                    if (isBulkMode)
                    {
                        CopySampleRateSettingToSelected(newSample);
                    }
                    else
                    {
                        aClip.SampleRateSetting = newSample;
                    }
                }
                RevertColor();

                GUILayout.Space(4);
                if (usesSampleRateOverride)
                {
                    MaybeShowChangedColors(isSampleRateChanged);
                    var selectedIndex = _sampleRates.IndexOf(aClip.SampleRateOverride);
                    if (selectedIndex < 0)
                    {
                        selectedIndex = 0;
                    }
                    var selectedValue = _sampleRates[selectedIndex];
                    var newSampleRate = EditorGUILayout.IntPopup(selectedValue, _sampleRateDisplays, _sampleRates.ToArray(), GUILayout.Width(82));
                    if (newSampleRate != aClip.SampleRateOverride)
                    {
                        aClip.IsSelected = true;
                        SelectClip(aClip);
                        if (isBulkMode)
                        {
                            CopySampleRateToSelected(newSampleRate);
                        }
                        else
                        {
                            aClip.SampleRateOverride = newSampleRate;
                        }
                    }
                    RevertColor();
                }
                else
                {
                    GUILayout.Space(78);
                }

                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndHorizontal();

                RevertColor();
            }

            GUI.EndScrollView();
        }

        private static void ShowDisabledColors()
        {
            GUI.color = Color.gray;
            GUI.contentColor = Color.white;
        }

        private static void MaybeShowChangedColors(bool areChanged)
        {
            if (!areChanged)
            {
                return;
            }

            GUI.backgroundColor = DTGUIHelper.BrightButtonColor;
            GUI.color = DTGUIHelper.BrightButtonColor;
        }

        private static void RevertColor()
        {
            GUI.backgroundColor = Color.white;
            GUI.color = Color.white;
            GUI.contentColor = Color.white;
        }

        private static void RevertChanges(AudioInformation info)
        {
            info.ForceMono = info.OrigForceMono;
            info.LoadBG = info.OrigLoadBG;
            info.Preload = info.OrigPreload;

            info.LoadType = info.OrigLoadType;
            info.Quality = info.OrigQuality;
            info.CompressionFormat = info.OrigCompressionFormat;
            info.SampleRateSetting = info.OrigSampleRateSetting;
            info.SampleRateOverride = info.OrigSampleRateOverride;
        }

        private void ApplyClipChanges(AudioInformation info, bool writeChanges)
        {
            Selection.objects = new Object[] { }; // unselect to get "Apply" to work automatically.

            // ReSharper disable once AccessToStaticMemberViaDerivedType
            var importer = (AudioImporter)AudioImporter.GetAtPath(info.FullPath);
            AudioImporterSampleSettings settings = importer.defaultSampleSettings;

            importer.forceToMono = info.ForceMono;
            importer.loadInBackground = info.LoadBG;

#if UNITY_2022_2_OR_NEWER
            settings.preloadAudioData = info.Preload;
#else
            importer.preloadAudioData = info.Preload;
#endif
            settings.loadType = info.LoadType;
            settings.compressionFormat = info.CompressionFormat;
            if (settings.compressionFormat == AudioCompressionFormat.Vorbis)
            {
                settings.quality = info.Quality;
            }
            settings.sampleRateSetting = info.SampleRateSetting;
            if (settings.sampleRateSetting == AudioSampleRateSetting.OverrideSampleRate)
            {
                settings.sampleRateOverride = (uint)info.SampleRateOverride;
            }

            importer.defaultSampleSettings = settings;

            AssetDatabase.ImportAsset(info.FullPath, ImportAssetOptions.ForceUpdate);
            info.HasChanged = true;

            if (writeChanges)
            {
                WriteFile(_clipList);
            }
        }

        private bool TranslateFromXml(XmlDocument xDoc)
        {
            _folderPaths.Clear();
            _folderPaths.Add("[All]");

            var files = xDoc.SelectNodes("/Files//File");

            // ReSharper disable once PossibleNullReferenceException
            if (files.Count == 0)
            {
                DTGUIHelper.ShowLargeBarAlert("You have no audio files in this project. Add some, then click 'Scan Project'.");
                return false;
            }

            try
            {
                // ReSharper disable once PossibleNullReferenceException
                _clipList.SearchFilter = xDoc.DocumentElement.Attributes["searchFilter"].Value;
                _clipList.SortColumn = (ClipSortColumn)Enum.Parse(typeof(ClipSortColumn), xDoc.DocumentElement.Attributes["sortColumn"].Value);
                _clipList.SortDir = (ClipSortDirection)Enum.Parse(typeof(ClipSortDirection), xDoc.DocumentElement.Attributes["sortDir"].Value);

                var currentPaths = new List<string>();

                for (var i = 0; i < files.Count; i++)
                {
                    var aNode = files[i];
                    // ReSharper disable once PossibleNullReferenceException
                    var path = aNode.Attributes["path"].Value.Trim();
                    var clipName = aNode.Attributes["name"].Value.Trim();
                    var forceMono = bool.Parse(aNode.Attributes["forceMono"].Value);
                    var loadBG = bool.Parse(aNode.Attributes["loadBG"].Value);
                    var preload = bool.Parse(aNode.Attributes["preload"].Value);

                    var loadType = (AudioClipLoadType)Enum.Parse(typeof(AudioClipLoadType), aNode.Attributes["loadType"].Value);
                    var compressionFormat = (AudioCompressionFormat)Enum.Parse(typeof(AudioCompressionFormat), aNode.Attributes["compFormat"].Value);
                    var sampleRateSetting = (AudioSampleRateSetting)Enum.Parse(typeof(AudioSampleRateSetting), aNode.Attributes["sampleSetting"].Value);
                    var quality = float.Parse(aNode.Attributes["quality"].Value);
                    var sampleRate = int.Parse(aNode.Attributes["sampleRate"].Value);

                    currentPaths.Add(path);

                    var folderPath = Path.GetDirectoryName(path);
                    if (!_folderPaths.Contains(folderPath))
                    {
                        _folderPaths.Add(folderPath);
                    }

                    var matchingClip = _clipList.AudioInfor.Find(delegate (AudioInformation obj)
                    {
                        return obj.FullPath == path;
                    });

                    if (matchingClip == null)
                    {
                        var aud = new AudioInformation(path, clipName, forceMono, loadBG, preload, loadType, compressionFormat, quality, sampleRateSetting, sampleRate);
                        _clipList.AudioInfor.Add(aud);
                    }
                    else
                    {
                        matchingClip.OrigForceMono = forceMono;
                        matchingClip.ForceMono = forceMono;

                        matchingClip.OrigLoadBG = loadBG;
                        matchingClip.LoadBG = loadBG;

                        matchingClip.OrigPreload = preload;
                        matchingClip.Preload = preload;

                        matchingClip.OrigLoadType = loadType;
                        matchingClip.LoadType = loadType;

                        matchingClip.OrigCompressionFormat = compressionFormat;
                        matchingClip.CompressionFormat = compressionFormat;

                        matchingClip.OrigQuality = quality;
                        matchingClip.Quality = quality;

                        matchingClip.OrigSampleRateSetting = sampleRateSetting;
                        matchingClip.SampleRateSetting = sampleRateSetting;

                        matchingClip.OrigSampleRateOverride = sampleRate;
                        matchingClip.SampleRateOverride = sampleRate;
                    }

                    _clipList.NeedsRefresh = false;
                }

                // delete clips no longer in the XML
                _clipList.AudioInfor.RemoveAll(delegate (AudioInformation obj)
                {
                    return !currentPaths.Contains(obj.FullPath);
                });
            }
            catch
            {
                DTGUIHelper.ShowRedError("Could not translate XML from cache file. Please click 'Scan Project'.");
                return false;
            }

            return true;
        }

        private void BuildCache()
        {
            var filePaths = AssetDatabase.GetAllAssetPaths();

            var audioInfo = new AudioInfoData();
            _filterClips = null;
            _pageNumber = 0;

            var updatedTime = DateTime.Now.Ticks;

            var localStreamingAssetsPath = Application.streamingAssetsPath;

            var indexOfAssets = Application.streamingAssetsPath.IndexOf("/Assets/");
            if (indexOfAssets > 0)
            {
                localStreamingAssetsPath = Application.streamingAssetsPath.Substring(indexOfAssets + 1);
            }

            foreach (var aPath in filePaths)
            {
                if (!aPath.EndsWith(".wav", StringComparison.InvariantCultureIgnoreCase)
                    && !aPath.EndsWith(".mp3", StringComparison.InvariantCultureIgnoreCase)
                    && !aPath.EndsWith(".ogg", StringComparison.InvariantCultureIgnoreCase)
                    && !aPath.EndsWith(".aiff", StringComparison.InvariantCultureIgnoreCase))
                {

                    continue;
                }

                if (aPath.Contains(localStreamingAssetsPath))
                {
                    continue; // stream assets don't have AudioImporters
                }

                // ReSharper disable once AccessToStaticMemberViaDerivedType
                var importer = AudioImporter.GetAtPath(aPath) as AudioImporter;
                if (importer == null)
                {
                    continue; 
                }

                // ReSharper disable once UseObjectOrCollectionInitializer
                AudioImporterSampleSettings settings = importer.defaultSampleSettings;

#if UNITY_2022_2_OR_NEWER
                var platform = PlatformString;

                var preloadAudioData = importer.GetOverrideSampleSettings(platform).preloadAudioData;
#else
                var preloadAudioData = importer.preloadAudioData;
#endif


                var newClip = new AudioInformation(aPath, Path.GetFileNameWithoutExtension(aPath), importer.forceToMono, importer.loadInBackground, 
                    preloadAudioData,
                    settings.loadType, settings.compressionFormat, settings.quality, settings.sampleRateSetting, int.Parse(settings.sampleRateOverride.ToString()));

                newClip.LastUpdated = updatedTime;

                audioInfo.AudioInfor.Add(newClip);
            }

            audioInfo.AudioInfor.RemoveAll(delegate (AudioInformation obj)
            {
                return obj.LastUpdated < updatedTime;
            });

            // write file
            if (!WriteFile(audioInfo))
            {
                return;
            }

            LoadAndTranslateFile();
        }

        private bool WriteFile(AudioInfoData audInfo)
        {
            StreamWriter writer = null;

            try
            {
                var sb = new StringBuilder(string.Empty);

                var safeFilter = audInfo.SearchFilter.Replace("'", "").Replace("\"", "");
                sb.Append(string.Format("<Files searchFilter='{0}' sortColumn='{1}' sortDir='{2}'>", safeFilter, audInfo.SortColumn, audInfo.SortDir));
                foreach (var aud in audInfo.AudioInfor)
                {
                    var mono = aud.HasChanged ? aud.ForceMono : aud.OrigForceMono;
                    var loadBG = aud.HasChanged ? aud.LoadBG : aud.OrigLoadBG;
                    var preload = aud.HasChanged ? aud.Preload : aud.OrigPreload;

                    var loadType = aud.HasChanged ? aud.LoadType : aud.OrigLoadType;

                    var compressionFormat = aud.HasChanged ? aud.CompressionFormat : aud.OrigCompressionFormat;
                    var quality = aud.HasChanged ? aud.Quality : aud.OrigQuality;

                    var sampleSetting = aud.HasChanged ? aud.SampleRateSetting : aud.OrigSampleRateSetting;
                    var sampleRate = aud.HasChanged ? aud.SampleRateOverride : aud.OrigSampleRateOverride;

                    sb.Append(string.Format("<File path='{0}' name='{1}' forceMono='{2}' loadBG='{3}' preload='{4}' loadType='{5}' compFormat='{6}' quality='{7}' sampleSetting='{8}' sampleRate='{9}' />",
                        UtilStrings.ReplaceUnsafeChars(aud.FullPath),
                        UtilStrings.ReplaceUnsafeChars(aud.Name),
                        mono,
                        loadBG,
                        preload,
                        loadType,
                        compressionFormat,
                        quality,
                        sampleSetting,
                        sampleRate));
                }
                sb.Append("</Files>");

                writer = new StreamWriter(CacheFilePath);
                writer.WriteLine(sb.ToString());

                _clipList.AudioInfor.RemoveAll(delegate (AudioInformation obj)
                {
                    return obj.HasChanged;
                });

                _filterClips = null; // re-generate the filtered list.
            }
            catch (Exception ex)
            {
                Debug.LogError("Error occurred constructing or writing audioImportSettings.xml file: " + ex);
                return false;
            }
            finally
            {
                if (writer != null)
                {
                    writer.Close();
                }
            }

            return true;
        }

        private static void SelectClip(AudioInformation info)
        {
            Selection.activeObject = AssetDatabase.LoadMainAssetAtPath(info.FullPath);
        }

        public enum ClipSortColumn
        {
            Name,
            ForceMono,
            LoadType,
            CompressionFormat,
            SampleRateSetting,
            Quality,
            SampleRate,
            LoadInBackground,
            PreloadAudio
        }

        public enum ClipSortDirection
        {
            Ascending,
            Descending
        }

        public class AudioInfoData
        {
            public List<AudioInformation> AudioInfor = new List<AudioInformation>();
            public string SearchFilter = string.Empty;
            public ClipSortColumn SortColumn = ClipSortColumn.Name;
            public ClipSortDirection SortDir = ClipSortDirection.Ascending;

            public bool NeedsRefresh;
        }

        public class AudioInformation
        {
            public AudioClipLoadType OrigLoadType;
            public AudioClipLoadType LoadType;
            public AudioCompressionFormat OrigCompressionFormat;
            public AudioCompressionFormat CompressionFormat;
            public AudioSampleRateSetting OrigSampleRateSetting;
            public AudioSampleRateSetting SampleRateSetting;
            public int OrigSampleRateOverride;
            public int SampleRateOverride;
            public float OrigQuality;
            public float Quality;

            public string FullPath;
            public string Name;
            public bool OrigForceMono;
            public bool ForceMono;
            public bool OrigLoadBG;
            public bool LoadBG;
            public bool OrigPreload;
            public bool Preload;

            public bool IsSelected;
            public bool HasChanged;
            public long LastUpdated;

            public AudioInformation(string fullPath, string name, bool forceMono, bool loadInBG, bool preload, AudioClipLoadType loadType, AudioCompressionFormat compressionFormat, float quality, AudioSampleRateSetting sampleRateSetting, int sampleRateOverride)
            {
                OrigForceMono = forceMono;
                ForceMono = forceMono;

                OrigLoadBG = loadInBG;
                LoadBG = loadInBG;

                OrigPreload = preload;
                Preload = preload;

                OrigLoadType = loadType;
                LoadType = loadType;

                OrigCompressionFormat = compressionFormat;
                CompressionFormat = compressionFormat;

                OrigSampleRateSetting = sampleRateSetting;
                SampleRateSetting = sampleRateSetting;

                OrigQuality = quality;
                Quality = quality;

                OrigSampleRateOverride = sampleRateOverride;
                SampleRateOverride = sampleRateOverride;

                FullPath = fullPath;
                Name = name;
                IsSelected = false;
                HasChanged = false;
                LastUpdated = DateTime.MinValue.Ticks;
            }
        }

        private static GUIStyle ToolbarSeachCancelButton { get { return GetStyle("ToolbarSeachCancelButton"); } }

        private static GUIStyle GetStyle(string styleName)
        {
            var guiStyle = GUI.skin.FindStyle(styleName) ?? EditorGUIUtility.GetBuiltinSkin(EditorSkin.Inspector).FindStyle(styleName);
            if (guiStyle != null)
            {
                return guiStyle;
            }

            Debug.LogError("Missing built-in guistyle " + styleName);
            guiStyle = GUI.skin.button;
            return guiStyle;
        }

        private void CopyForceMonoToSelected(bool _bulkForceMono)
        {
            foreach (var aClip in SelectedClips)
            {
                aClip.ForceMono = _bulkForceMono;
            }
        }

        private void CopyLoadInBGToSelected(bool _bulkLoadInBG)
        {
            foreach (var aClip in SelectedClips)
            {
                aClip.LoadBG = _bulkLoadInBG;
            }
        }

        private void CopyPreloadAudioToSelected(bool _bulkPreload)
        {
            foreach (var aClip in SelectedClips)
            {
                aClip.Preload = _bulkPreload;
            }
        }

        private void CopyLoadTypeToSelected(AudioClipLoadType _bulkLoadType)
        {
            foreach (var aClip in SelectedClips)
            {
                aClip.LoadType = _bulkLoadType;
            }
        }

        private void CopyCompFormatToSelected(AudioCompressionFormat _bulkCompressionFormat)
        {
            foreach (var aClip in SelectedClips)
            {
                aClip.CompressionFormat = _bulkCompressionFormat;
            }
        }

        private void CopyQualityToSelected(float _bulkQuality)
        {
            foreach (var aClip in SelectedClips)
            {
                aClip.Quality = _bulkQuality;
            }
        }

        private void CopySampleRateSettingToSelected(AudioSampleRateSetting _bulkSampleRateSetting)
        {
            foreach (var aClip in SelectedClips)
            {
                aClip.SampleRateSetting = _bulkSampleRateSetting;
            }
        }

        private void CopySampleRateToSelected(int _bulkSampleRateOverride)
        {
            foreach (var aClip in SelectedClips)
            {
                aClip.SampleRateOverride = _bulkSampleRateOverride;
            }
        }

        private string CacheFilePath {
            get {
                var path = MasterAudio.MasterAudioFolderPath + "/audioImportSettings.xml";
                return path;
            }
        }

#if UNITY_2022_2_OR_NEWER
        private string PlatformString {
            get {
                var platform = string.Empty;

                switch (Application.platform) {
                    case RuntimePlatform.IPhonePlayer:
                        platform = "iOS";
                        break;
                    case RuntimePlatform.WebGLPlayer:
                        platform = "WebPlayer";
                        break;
                    case RuntimePlatform.LinuxPlayer:
                    case RuntimePlatform.LinuxServer:
                    case RuntimePlatform.LinuxEditor:
                    case RuntimePlatform.EmbeddedLinuxArm32:
                    case RuntimePlatform.EmbeddedLinuxArm64:
                    case RuntimePlatform.EmbeddedLinuxX64:
                    case RuntimePlatform.EmbeddedLinuxX86:
                    case RuntimePlatform.WindowsEditor:
                    case RuntimePlatform.WindowsPlayer:
                    case RuntimePlatform.WindowsServer:
                        platform = "Standalone";
                        break;
                    case RuntimePlatform.Android:
                        platform = "Android";
                        break;
                    case RuntimePlatform.PS4:
                        platform = "PS4";
                        break;
                    case RuntimePlatform.XboxOne:
                        platform = "XBoxOne";
                        break;
                }

                return platform;
            }
        }
#endif
    }
}