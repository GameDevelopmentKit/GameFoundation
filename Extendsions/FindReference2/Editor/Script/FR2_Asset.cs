//#define FR2_DEBUG_BRACE_LEVEL
//#define FR2_DEBUG_SYMBOL
//#define FR2_DEBUG


#if FR2_ADDRESSABLE
using UnityEditor.AddressableAssets;
using UnityEngine.AddressableAssets;
#endif


using UnityObject = UnityEngine.Object;

namespace vietlabs.fr2
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Text.RegularExpressions;
    using UnityEditor;
    using UnityEngine;

    public enum FR2_AssetType
    {
        UNKNOWN,
        FOLDER,
        SCRIPT,
        SCENE,
        DLL,
        REFERENCABLE,
        BINARY_ASSET,
        MODEL,
        TERRAIN,
        NON_READABLE
    }

    public enum FR2_AssetState
    {
        NEW,
        CACHE,
        MISSING
    }

    [Serializable]
    public class FR2_Asset
    {
        // ------------------------------ CONSTANTS ---------------------------

        private static readonly HashSet<string> SCRIPT_EXTENSIONS = new HashSet<string>
        {
            ".cs", ".js", ".boo", ".h", ".java", ".cpp", ".m", ".mm"
        };

        private static readonly HashSet<string> REFERENCABLE_EXTENSIONS = new HashSet<string>
        {
            ".anim", ".controller", ".mat", ".unity", ".guiskin", ".prefab",
            ".overridecontroller", ".mask", ".rendertexture", ".cubemap", ".flare",
            ".mat", ".prefab", ".physicsmaterial", ".fontsettings", ".asset", ".prefs", ".spriteatlas"
        };

        private static readonly Dictionary<int, Type>          HashClasses = new Dictionary<int, Type>();
        internal static         Dictionary<string, GUIContent> cacheImage  = new Dictionary<string, GUIContent>();

        private bool                             _isExcluded;
        private Dictionary<string, HashSet<int>> _UseGUIDs;
        private float                            excludeTS;

        public static float ignoreTS;


        // ----------------------------- DRAW  ---------------------------------------
        [NonSerialized] private GUIContent fileSizeText;

        // ----------------------------- DRAW  ---------------------------------------

        [SerializeField] public string guid;

        // easy to recalculate: will not cache
        [NonSerialized] private bool   m_pathLoaded;
        [NonSerialized] private string m_assetFolder;
        [NonSerialized] private string m_assetName;
        [NonSerialized] private string m_assetPath;
        [NonSerialized] private string m_extension;
        [NonSerialized] private bool   m_inEditor;
        [NonSerialized] private bool   m_inPlugins;
        [NonSerialized] private bool   m_inResources;
        [NonSerialized] private bool   m_inStreamingAsset;
        [NonSerialized] private bool   m_isAssetFile;

        // Need to read FileInfo: soft-cache (always re-read when needed)
        [SerializeField] public  FR2_AssetType type;
        [SerializeField] private string        m_fileInfoHash;
        [SerializeField] private string        m_assetbundle;
        [SerializeField] private string        m_addressable;

        [SerializeField] private string m_atlas;
        [SerializeField] private long   m_fileSize;

        [SerializeField] private int m_assetChangeTS; // Realtime when asset changed (trigger by import asset operation)
        [SerializeField] private int m_fileInfoReadTS; // Realtime when asset being read

        [SerializeField] private int m_fileWriteTS; // file's lastModification (file content + meta)
        [SerializeField] private int m_cachefileWriteTS; // file's lastModification at the time the content being read

        [SerializeField] internal int refreshStamp; // use to check if asset has been deleted (refreshStamp not updated)


        // Do not cache
        [NonSerialized] internal FR2_AssetState                state;
        internal                 Dictionary<string, FR2_Asset> UsedByMap            = new Dictionary<string, FR2_Asset>();
        internal                 HashSet<int>                  HashUsedByClassesIds = new HashSet<int>();
        [SerializeField] private List<Classes>                 UseGUIDsList         = new List<Classes>();

        public FR2_Asset(string guid)
        {
            this.guid = guid;
            this.type = FR2_AssetType.UNKNOWN;
        }

        // ----------------------- PATH INFO ------------------------
        public void LoadPathInfo()
        {
            if (this.m_pathLoaded) return;
            this.m_pathLoaded = true;

            this.m_assetPath = AssetDatabase.GUIDToAssetPath(this.guid);
            if (string.IsNullOrEmpty(this.assetPath))
            {
                this.state = FR2_AssetState.MISSING;
                return;
            }

#if FR2_DEBUG
			Debug.LogWarning("Refreshing ... " + loadInfoTS + ":" + AssetDatabase.GUIDToAssetPath(guid));
			if (!m_assetPath.StartsWith("Assets"))
			{
				Debug.Log("LoadAssetInfo: " + m_assetPath);
			}
#endif
            FR2_Unity.SplitPath(this.m_assetPath, out this.m_assetName, out this.m_extension, out this.m_assetFolder);

            if (this.m_assetFolder.StartsWith("Assets/"))
                this.m_assetFolder                                                                                                   = this.m_assetFolder.Substring(7);
            else if (!FR2_Unity.StringStartsWith(this.m_assetPath, "Packages/", "Project Settings/", "Library/")) this.m_assetFolder = "built-in/";

            this.m_inEditor         = this.m_assetPath.Contains("/Editor/") || this.m_assetPath.Contains("/Editor Default Resources/");
            this.m_inResources      = this.m_assetPath.Contains("/Resources/");
            this.m_inStreamingAsset = this.m_assetPath.Contains("/StreamingAssets/");
            this.m_inPlugins        = this.m_assetPath.Contains("/Plugins/");
            this.m_isAssetFile      = this.m_assetPath.EndsWith(".asset", StringComparison.Ordinal);
        }

        public string assetName
        {
            get
            {
                this.LoadPathInfo();
                return this.m_assetName;
            }
        }

        public string assetPath
        {
            get
            {
                if (!string.IsNullOrEmpty(this.m_assetPath)) return this.m_assetPath;
                this.m_assetPath = AssetDatabase.GUIDToAssetPath(this.guid);
                if (string.IsNullOrEmpty(this.m_assetPath)) this.state = FR2_AssetState.MISSING;
                return this.m_assetPath;
            }
        }

        public string parentFolderPath
        {
            get
            {
                this.LoadPathInfo();
                return this.m_assetFolder;
            }
        }

        public string assetFolder
        {
            get
            {
                this.LoadPathInfo();
                return this.m_assetFolder;
            }
        }

        public string extension
        {
            get
            {
                this.LoadPathInfo();
                return this.m_extension;
            }
        }

        public bool inEditor
        {
            get
            {
                this.LoadPathInfo();
                return this.m_inEditor;
            }
        }

        public bool inPlugins
        {
            get
            {
                this.LoadPathInfo();
                return this.m_inPlugins;
            }
        }

        public bool inResources
        {
            get
            {
                this.LoadPathInfo();
                return this.m_inResources;
            }
        }

        public bool inStreamingAsset
        {
            get
            {
                this.LoadPathInfo();
                return this.m_inStreamingAsset;
            }
        }

        // ----------------------- TYPE INFO ------------------------

        internal bool IsFolder  => this.type == FR2_AssetType.FOLDER;
        internal bool IsScript  => this.type == FR2_AssetType.SCRIPT;
        internal bool IsMissing => this.state == FR2_AssetState.MISSING;

        internal bool IsReferencable => this.type == FR2_AssetType.REFERENCABLE || this.type == FR2_AssetType.SCENE;

        internal bool IsBinaryAsset => this.type == FR2_AssetType.BINARY_ASSET || this.type == FR2_AssetType.MODEL || this.type == FR2_AssetType.TERRAIN;

        // ----------------------- PATH INFO ------------------------
        public bool fileInfoDirty    => this.type == FR2_AssetType.UNKNOWN || this.m_fileInfoReadTS <= this.m_assetChangeTS;
        public bool fileContentDirty => this.m_fileWriteTS != this.m_cachefileWriteTS;

        public bool isDirty => this.fileInfoDirty || this.fileContentDirty;

        private bool ExistOnDisk()
        {
            if (this.IsMissing) return false; // asset not exist - no need to check FileSystem!
            if (this.type == FR2_AssetType.FOLDER || this.type == FR2_AssetType.UNKNOWN)
            {
                if (Directory.Exists(this.m_assetPath))
                {
                    if (this.type == FR2_AssetType.UNKNOWN) this.type = FR2_AssetType.FOLDER;
                    return true;
                }

                if (this.type == FR2_AssetType.FOLDER) return false;
            }

            // must be file here
            if (!File.Exists(this.m_assetPath)) return false;

            if (this.type == FR2_AssetType.UNKNOWN) this.GuessAssetType();
            return true;
        }

        internal void LoadFileInfo()
        {
            if (!this.fileInfoDirty) return;
            if (string.IsNullOrEmpty(this.m_assetPath)) this.LoadPathInfo(); // always reload Path Info

            //Debug.Log("--> Read: " + assetPath + " --> " + m_fileInfoReadTS + "<" + m_assetChangeTS);
            this.m_fileInfoReadTS = FR2_Unity.Epoch(DateTime.Now);

            if (this.IsMissing)
            {
                Debug.LogWarning("Should never be here! - missing files can not trigger LoadFileInfo()");
                return;
            }

            if (!this.ExistOnDisk())
            {
                this.state = FR2_AssetState.MISSING;
                return;
            }

            if (this.type == FR2_AssetType.FOLDER) return; // nothing to read

            var assetType = AssetDatabase.GetMainAssetTypeAtPath(this.m_assetPath);
            if (assetType == typeof(FR2_Cache)) return;

            var info = new FileInfo(this.m_assetPath);
            this.m_fileSize     = info.Length;
            this.m_fileInfoHash = info.Length + info.Extension;
            this.m_addressable  = FR2_Unity.GetAddressable(this.guid);
            //if (!string.IsNullOrEmpty(m_addressable)) Debug.LogWarning(guid + " --> " + m_addressable);
            this.m_assetbundle = AssetDatabase.GetImplicitAssetBundleName(this.m_assetPath);

            if (assetType == typeof(Texture2D))
            {
                var importer = AssetImporter.GetAtPath(this.m_assetPath);
                if (importer is TextureImporter)
                {
                    var tImporter                                         = importer as TextureImporter;
                    if (tImporter.qualifiesForSpritePacking) this.m_atlas = tImporter.spritePackingTag;
                }
            }

            // check if file content changed
            var metaInfo  = new FileInfo(this.m_assetPath + ".meta");
            var assetTime = FR2_Unity.Epoch(info.LastWriteTime);
            var metaTime  = FR2_Unity.Epoch(metaInfo.LastWriteTime);

            // update fileChangeTimeStamp
            this.m_fileWriteTS = Mathf.Max(metaTime, assetTime);
        }

        internal string fileInfoHash
        {
            get
            {
                this.LoadFileInfo();
                return this.m_fileInfoHash;
            }
        }

        internal long fileSize
        {
            get
            {
                this.LoadFileInfo();
                return this.m_fileSize;
            }
        }

        public string AtlasName
        {
            get
            {
                this.LoadFileInfo();
                return this.m_atlas;
            }
        }

        public string AssetBundleName
        {
            get
            {
                this.LoadFileInfo();
                return this.m_assetbundle;
            }
        }

        public string AddressableName
        {
            get
            {
                this.LoadFileInfo();
                return this.m_addressable;
            }
        }


        public Dictionary<string, HashSet<int>> UseGUIDs
        {
            get
            {
                if (this._UseGUIDs != null) return this._UseGUIDs;

                this._UseGUIDs = new Dictionary<string, HashSet<int>>(this.UseGUIDsList.Count);
                for (var i = 0; i < this.UseGUIDsList.Count; i++)
                {
                    var guid = this.UseGUIDsList[i].guid;
                    if (this._UseGUIDs.ContainsKey(guid))
                        for (var j = 0; j < this.UseGUIDsList[i].ids.Count; j++)
                        {
                            var val = this.UseGUIDsList[i].ids[j];
                            if (this._UseGUIDs[guid].Contains(val)) continue;

                            this._UseGUIDs[guid].Add(this.UseGUIDsList[i].ids[j]);
                        }
                    else
                        this._UseGUIDs.Add(guid, new HashSet<int>(this.UseGUIDsList[i].ids));
                }

                return this._UseGUIDs;
            }
        }

        // ------------------------------- GETTERS -----------------------------


        internal bool IsExcluded
        {
            get
            {
                if (this.excludeTS >= ignoreTS) return this._isExcluded;

                this.excludeTS   = ignoreTS;
                this._isExcluded = false;

                var h = FR2_Setting.IgnoreAsset;
                foreach (var item in FR2_Setting.IgnoreAsset)
                    if (this.m_assetPath.StartsWith(item, false, CultureInfo.InvariantCulture))
                    {
                        this._isExcluded = true;
                        break;
                    }

                return this._isExcluded;
            }
        }

        public void AddUsedBy(string guid, FR2_Asset asset)
        {
            if (this.UsedByMap.ContainsKey(guid)) return;

            if (guid == this.guid)
                //Debug.LogWarning("self used");
                return;


            this.UsedByMap.Add(guid, asset);
            HashSet<int> output;
            if (this.HashUsedByClassesIds == null) this.HashUsedByClassesIds = new HashSet<int>();

            if (asset.UseGUIDs.TryGetValue(this.guid, out output))
                foreach (var item in output)
                    this.HashUsedByClassesIds.Add(item);

            // int classId = HashUseByClassesIds    
        }

        public int UsageCount() { return this.UsedByMap.Count; }

        public override string ToString() { return string.Format("FR2_Asset[{0}]", this.m_assetName); }

        //--------------------------------- STATIC ----------------------------

        internal static bool IsValidGUID(string guid)
        {
            return AssetDatabase.GUIDToAssetPath(guid) != FR2_Cache.CachePath; // just skip FR2_Cache asset
        }

        internal void MarkAsDirty(bool isMoved = true, bool force = false)
        {
            if (isMoved)
            {
                var newPath = AssetDatabase.GUIDToAssetPath(this.guid);
                if (newPath != this.m_assetPath)
                {
                    this.m_pathLoaded = false;
                    this.m_assetPath  = newPath;
                }
            }

            this.state           = FR2_AssetState.CACHE;
            this.m_assetChangeTS = FR2_Unity.Epoch(DateTime.Now); // re-read FileInfo
            if (force) this.m_cachefileWriteTS = 0;
        }

        // --------------------------------- APIs ------------------------------

        internal void GuessAssetType()
        {
            if (SCRIPT_EXTENSIONS.Contains(this.m_extension))
            {
                this.type = FR2_AssetType.SCRIPT;
            }
            else if (REFERENCABLE_EXTENSIONS.Contains(this.m_extension))
            {
                var isUnity = this.m_extension == ".unity";
                this.type = isUnity ? FR2_AssetType.SCENE : FR2_AssetType.REFERENCABLE;

                if (this.m_extension == ".asset" || isUnity || this.m_extension == ".spriteatlas")
                {
                    var        buffer = new byte[5];
                    FileStream stream = null;

                    try
                    {
                        stream = File.OpenRead(this.m_assetPath);
                        stream.Read(buffer, 0, 5);
                        stream.Close();
                    }
#if FR2_DEBUG
                    catch (Exception e)
                    {
                        Debug.LogWarning("Guess Asset Type error :: " + e + "\n" + m_assetPath);
#else
                    catch
                    {
#endif
                        if (stream != null) stream.Close();
                        this.state = FR2_AssetState.MISSING;
                        return;
                    }
                    finally
                    {
                        if (stream != null) stream.Close();
                    }

                    var str                       = string.Empty;
                    foreach (var t in buffer) str += (char)t;

                    if (str != "%YAML") this.type = FR2_AssetType.BINARY_ASSET;
                }
            }
            else if (this.m_extension == ".fbx")
            {
                this.type = FR2_AssetType.MODEL;
            }
            else if (this.m_extension == ".dll")
            {
                this.type = FR2_AssetType.DLL;
            }
            else
            {
                this.type = FR2_AssetType.NON_READABLE;
            }
        }


        internal void LoadContent()
        {
            if (!this.fileContentDirty) return;
            this.m_cachefileWriteTS = this.m_fileWriteTS;

            if (this.IsMissing || this.type == FR2_AssetType.NON_READABLE) return;

            if (this.type == FR2_AssetType.DLL)
            {
#if FR2_DEBUG
            Debug.LogWarning("Parsing DLL not yet supportted ");
#endif
                return;
            }

            if (!this.ExistOnDisk())
            {
                this.state = FR2_AssetState.MISSING;
                return;
            }

            this.ClearUseGUIDs();

            if (this.IsFolder)
                this.LoadFolder();
            else if (this.IsReferencable)
                this.LoadYAML2();
            else if (this.IsBinaryAsset) this.LoadBinaryAsset();
        }

        internal void AddUseGUID(string fguid, int fFileId = -1, bool checkExist = true)
        {
            // if (checkExist && UseGUIDs.ContainsKey(fguid)) return;
            if (!IsValidGUID(fguid)) return;

            if (!this.UseGUIDs.ContainsKey(fguid))
            {
                this.UseGUIDsList.Add(new Classes
                {
                    guid = fguid,
                    ids  = new List<int>()
                });
                this.UseGUIDs.Add(fguid, new HashSet<int>());
            }

            if (fFileId != -1)
            {
                if (this.UseGUIDs[fguid].Contains(fFileId)) return;

                this.UseGUIDs[fguid].Add(fFileId);
                var i = this.UseGUIDsList.FirstOrDefault(x => x.guid == fguid);
                if (i != null) i.ids.Add(fFileId);
            }
        }

        // ----------------------------- STATIC  ---------------------------------------

        internal static int SortByExtension(FR2_Asset a1, FR2_Asset a2)
        {
            if (a1 == null) return -1;

            if (a2 == null) return 1;

            var result = string.Compare(a1.m_extension, a2.m_extension, StringComparison.Ordinal);
            return result == 0 ? string.Compare(a1.m_assetName, a2.m_assetName, StringComparison.Ordinal) : result;
        }

        internal static List<FR2_Asset> FindUsage(FR2_Asset asset)
        {
            if (asset == null) return null;

            var refs = FR2_Cache.Api.FindAssets(asset.UseGUIDs.Keys.ToArray(), true);


            return refs;
        }

        internal static List<FR2_Asset> FindUsedBy(FR2_Asset asset) { return asset.UsedByMap.Values.ToList(); }

        internal static List<string> FindUsageGUIDs(FR2_Asset asset, bool includeScriptSymbols)
        {
            var result = new HashSet<string>();
            if (asset == null)
            {
                Debug.LogWarning("Asset invalid : " + asset.m_assetName);
                return result.ToList();
            }

            // for (var i = 0;i < asset.UseGUIDs.Count; i++)
            // {
            // 	result.Add(asset.UseGUIDs[i]);
            // }
            foreach (var item in asset.UseGUIDs) result.Add(item.Key);

            //if (!includeScriptSymbols) return result.ToList();

            //if (asset.ScriptUsage != null)
            //{
            //	for (var i = 0; i < asset.ScriptUsage.Count; i++)
            //	{
            //    	var symbolList = FR2_Cache.Api.FindAllSymbol(asset.ScriptUsage[i]);
            //    	if (symbolList.Contains(asset)) continue;

            //    	var symbol = symbolList[0];
            //    	if (symbol == null || result.Contains(symbol.guid)) continue;

            //    	result.Add(symbol.guid);
            //	}	
            //}

            return result.ToList();
        }

        internal static List<string> FindUsedByGUIDs(FR2_Asset asset) { return asset.UsedByMap.Keys.ToList(); }

        internal float Draw(Rect r,
            bool highlight,
            bool drawPath = true,
            bool showFileSize = true,
            bool showABName = false,
            bool showAtlasName = false,
            bool showUsageIcon = true,
            IWindow window = null
        )
        {
            var singleLine = r.height <= 18f;
            var rw         = r.width;
            var selected   = FR2_Bookmark.Contains(this.guid);

            r.height = 16f;
            var hasMouse = Event.current.type == EventType.MouseUp && r.Contains(Event.current.mousePosition);

            if (hasMouse && Event.current.button == 1)
            {
                var menu = new GenericMenu();
                if (this.m_extension == ".prefab") menu.AddItem(new GUIContent("Edit in Scene"), false, this.EditPrefab);

                menu.AddItem(new GUIContent("Open"), false, this.Open);
                menu.AddItem(new GUIContent("Ping"), false, this.Ping);
                menu.AddItem(new GUIContent(this.guid), false, this.CopyGUID);
                //menu.AddItem(new GUIContent("Reload"), false, Reload);

                menu.AddSeparator(string.Empty);
                menu.AddItem(new GUIContent("Bookmark"), selected, this.AddToSelection);
                menu.AddSeparator(string.Empty);
                menu.AddItem(new GUIContent("Copy path"), false, this.CopyAssetPath);
                menu.AddItem(new GUIContent("Copy full path"), false, this.CopyAssetPathFull);

                //if (IsScript)
                //{
                //    menu.AddSeparator(string.Empty);
                //    AddArray(menu, ScriptSymbols, "+ ", "Definitions", "No Definition", false);

                //    menu.AddSeparator(string.Empty);
                //    AddArray(menu, ScriptUsage, "-> ", "Depends", "No Dependency", true);
                //}

                menu.ShowAsContext();
                Event.current.Use();
            }

            if (this.IsMissing)
            {
                if (!singleLine) r.y += 16f;

                if (Event.current.type != EventType.Repaint) return 0;

                GUI.Label(r, "(missing) " + this.guid, EditorStyles.whiteBoldLabel);
                return 0;
            }

            var iconRect = GUI2.LeftRect(16f, ref r);
            if (Event.current.type == EventType.Repaint)
            {
                var icon = AssetDatabase.GetCachedIcon(this.m_assetPath);
                if (icon != null) GUI.DrawTexture(iconRect, icon);
            }


            if (Event.current.type == EventType.MouseDown && Event.current.button == 0)
            {
                var pingRect = FR2_Setting.PingRow ? new Rect(0, r.y, r.x + r.width, r.height) : iconRect;
                if (pingRect.Contains(Event.current.mousePosition))
                {
                    if (Event.current.control || Event.current.command)
                    {
                        if (selected)
                            this.RemoveFromSelection();
                        else
                            this.AddToSelection();

                        if (window != null) window.Repaint();
                    }
                    else
                    {
                        this.Ping();
                    }


                    //Event.current.Use();
                }
            }

            if (Event.current.type != EventType.Repaint) return 0;

            if (this.UsedByMap != null && this.UsedByMap.Count > 0)
            {
                var str       = new GUIContent(this.UsedByMap.Count.ToString());
                var countRect = iconRect;
                countRect.x    -= 16f;
                countRect.xMin =  -10f;
                GUI.Label(countRect, str, GUI2.miniLabelAlignRight);
            }

            var pathW = drawPath ? EditorStyles.miniLabel.CalcSize(new GUIContent(this.m_assetFolder)).x : 0;
            var nameW = EditorStyles.boldLabel.CalcSize(new GUIContent(this.m_assetName)).x;
            var cc    = FR2_Cache.Api.setting.SelectedColor;

            if (singleLine)
            {
                var lbRect = GUI2.LeftRect(pathW + nameW, ref r);

                if (selected)
                {
                    var c1 = GUI.color;
                    GUI.color = cc;
                    GUI.DrawTexture(lbRect, EditorGUIUtility.whiteTexture);
                    GUI.color = c1;
                }

                if (drawPath)
                {
                    var c2 = GUI.color;
                    GUI.color = new Color(c2.r, c2.g, c2.b, c2.a * 0.5f);
                    GUI.Label(GUI2.LeftRect(pathW, ref lbRect), this.m_assetFolder, EditorStyles.miniLabel);
                    GUI.color = c2;

                    lbRect.xMin -= 4f;
                    GUI.Label(lbRect, this.m_assetName, EditorStyles.boldLabel);
                }
                else
                {
                    GUI.Label(lbRect, this.m_assetName);
                }
            }
            else
            {
                if (drawPath) GUI.Label(new Rect(r.x, r.y + 16f, r.width, r.height), this.m_assetFolder, EditorStyles.miniLabel);

                var lbRect = GUI2.LeftRect(nameW, ref r);
                if (selected) GUI2.Rect(lbRect, cc);

                GUI.Label(lbRect, this.m_assetName, EditorStyles.boldLabel);
            }

            var rr = GUI2.RightRect(10f, ref r); //margin
            if (highlight)
            {
                rr.xMin  += 2f;
                rr.width =  1f;
                GUI2.Rect(rr, GUI2.darkGreen);
            }

            var c = GUI.color;
            GUI.color = new Color(c.r, c.g, c.b, c.a * 0.5f);

            if (showFileSize)
            {
                var fsRect = GUI2.RightRect(40f, ref r); // filesize label

                if (this.fileSizeText == null) this.fileSizeText = new GUIContent(FR2_Helper.GetfileSizeString(this.fileSize));


                GUI.Label(fsRect, this.fileSizeText, GUI2.miniLabelAlignRight);
            }

            if (!string.IsNullOrEmpty(this.m_addressable))
            {
                var adRect = GUI2.RightRect(100f, ref r);
                GUI.Label(adRect, this.m_addressable, GUI2.miniLabelAlignRight);
            }


            if (showUsageIcon && this.HashUsedByClassesIds != null)
                foreach (var item in this.HashUsedByClassesIds)
                {
                    if (!FR2_Unity.HashClassesNormal.ContainsKey(item)) continue;

                    var  name = FR2_Unity.HashClassesNormal[item];
                    Type t    = null;
                    if (!HashClasses.TryGetValue(item, out t))
                    {
                        t = FR2_Unity.GetType(name);
                        HashClasses.Add(item, t);
                    }

                    GUIContent content           = null;
                    var        isExisted         = cacheImage.TryGetValue(name, out content);
                    if (content == null) content = new GUIContent(EditorGUIUtility.ObjectContent(null, t).image, name);

                    if (!isExisted)
                        cacheImage.Add(name, content);
                    else
                        cacheImage[name] = content;

                    if (content != null)
                    {
                        try
                        {
                            GUI.Label(GUI2.RightRect(15f, ref r), content, GUI2.miniLabelAlignRight);
                        }
#if !FR2_DEBUG
                        catch
                        {
                        }
#else
						catch (Exception e)
						{
							UnityEngine.Debug.LogWarning(e);
						}
#endif
                    }
                }

            if (showAtlasName)
            {
                GUI2.RightRect(10f, ref r); //margin
                var abRect = GUI2.RightRect(120f, ref r); // filesize label
                if (!string.IsNullOrEmpty(this.m_atlas)) GUI.Label(abRect, this.m_atlas, GUI2.miniLabelAlignRight);
            }

            if (showABName)
            {
                GUI2.RightRect(10f, ref r); //margin
                var abRect = GUI2.RightRect(100f, ref r); // filesize label
                if (!string.IsNullOrEmpty(this.m_assetbundle)) GUI.Label(abRect, this.m_assetbundle, GUI2.miniLabelAlignRight);
            }

            if (true)
            {
                GUI2.RightRect(10f, ref r); //margin
                var abRect = GUI2.RightRect(100f, ref r); // filesize label
                if (!string.IsNullOrEmpty(this.m_addressable)) GUI.Label(abRect, this.m_addressable, GUI2.miniLabelAlignRight);
            }

            GUI.color = c;

            if (Event.current.type == EventType.Repaint) return rw < pathW + nameW ? 32f : 18f;

            return r.height;
        }


        internal GenericMenu AddArray(GenericMenu menu, List<string> list, string prefix, string title,
            string emptyTitle, bool showAsset, int max = 10)
        {
            //if (list.Count > 0)
            //{
            //    if (list.Count > max)
            //    {
            //        prefix = string.Format("{0} _{1}/", title, list.Count) + prefix;
            //    }

            //    //for (var i = 0; i < list.Count; i++)
            //    //{
            //    //    var def = list[i];
            //    //    var suffix = showAsset ? "/" + FR2_Cache.Api.FindSymbol(def).assetName : string.Empty;
            //    //    menu.AddItem(new GUIContent(prefix + def + suffix), false, () => OpenScript(def));
            //    //}
            //}
            //else
            {
                menu.AddItem(new GUIContent(emptyTitle), true, null);
            }

            return menu;
        }

        internal void CopyGUID()
        {
            EditorGUIUtility.systemCopyBuffer = this.guid;
            Debug.Log(this.guid);
        }

        internal void CopyName()
        {
            EditorGUIUtility.systemCopyBuffer = this.m_assetName;
            Debug.Log(this.m_assetName);
        }

        internal void CopyAssetPath()
        {
            EditorGUIUtility.systemCopyBuffer = this.m_assetPath;
            Debug.Log(this.m_assetPath);
        }

        internal void CopyAssetPathFull()
        {
            var fullName = new FileInfo(this.m_assetPath).FullName;
            EditorGUIUtility.systemCopyBuffer = fullName;
            Debug.Log(fullName);
        }

        internal void AddToSelection()
        {
            if (!FR2_Bookmark.Contains(this.guid)) FR2_Bookmark.Add(this.guid);

            //var list = Selection.objects.ToList();
            //var obj = FR2_Unity.LoadAssetAtPath<Object>(assetPath);
            //if (!list.Contains(obj))
            //{
            //    list.Add(obj);
            //    Selection.objects = list.ToArray();
            //}
        }

        internal void RemoveFromSelection()
        {
            if (FR2_Bookmark.Contains(this.guid)) FR2_Bookmark.Remove(this.guid);
        }

        internal void Ping()
        {
            EditorGUIUtility.PingObject(
                AssetDatabase.LoadAssetAtPath(this.m_assetPath, typeof(UnityObject))
            );
        }

        internal void Open()
        {
            AssetDatabase.OpenAsset(
                AssetDatabase.LoadAssetAtPath(this.m_assetPath, typeof(UnityObject))
            );
        }

        internal void EditPrefab()
        {
            var prefab = AssetDatabase.LoadAssetAtPath(this.m_assetPath, typeof(UnityObject));
            UnityObject.Instantiate(prefab);
        }

        //internal void OpenScript(string definition)
        //{
        //    var asset = FR2_Cache.Api.FindSymbol(definition);
        //    if (asset == null) return;

        //    EditorGUIUtility.PingObject(
        //        AssetDatabase.LoadAssetAtPath(asset.assetPath, typeof(Object))
        //        );
        //}

        // ----------------------------- SERIALIZED UTILS ---------------------------------------


        // ----------------------------- LOAD ASSETS ---------------------------------------

        internal void LoadGameObject(GameObject go)
        {
            var compList = go.GetComponentsInChildren<Component>();
            for (var i = 0; i < compList.Length; i++) this.LoadSerialized(compList[i]);
        }

        internal void LoadSerialized(UnityObject target)
        {
            var props = FR2_Unity.xGetSerializedProperties(target, true);

            for (var i = 0; i < props.Length; i++)
            {
                if (props[i].propertyType != SerializedPropertyType.ObjectReference) continue;

                var refObj = props[i].objectReferenceValue;
                if (refObj == null) continue;

                var refGUID = AssetDatabase.AssetPathToGUID(
                    AssetDatabase.GetAssetPath(refObj)
                );

                //Debug.Log("Found Reference BinaryAsset <" + assetPath + "> : " + refGUID + ":" + refObj);
                this.AddUseGUID(refGUID);
            }
        }

        internal void LoadTerrainData(TerrainData terrain)
        {
#if UNITY_2018_3_OR_NEWER
            var arr0 = terrain.terrainLayers;
            for (var i = 0; i < arr0.Length; i++)
            {
                var aPath   = AssetDatabase.GetAssetPath(arr0[i]);
                var refGUID = AssetDatabase.AssetPathToGUID(aPath);
                this.AddUseGUID(refGUID);
            }
#endif


            var arr = terrain.detailPrototypes;

            for (var i = 0; i < arr.Length; i++)
            {
                var aPath   = AssetDatabase.GetAssetPath(arr[i].prototypeTexture);
                var refGUID = AssetDatabase.AssetPathToGUID(aPath);
                this.AddUseGUID(refGUID);
            }

            var arr2 = terrain.treePrototypes;
            for (var i = 0; i < arr2.Length; i++)
            {
                var aPath   = AssetDatabase.GetAssetPath(arr2[i].prefab);
                var refGUID = AssetDatabase.AssetPathToGUID(aPath);
                this.AddUseGUID(refGUID);
            }

            var arr3 = FR2_Unity.GetTerrainTextureDatas(terrain);
            for (var i = 0; i < arr3.Length; i++)
            {
                var texs = arr3[i];
                for (var k = 0; k < texs.textures.Length; k++)
                {
                    var tex = texs.textures[k];
                    if (tex == null) continue;

                    var aPath = AssetDatabase.GetAssetPath(tex);
                    if (string.IsNullOrEmpty(aPath)) continue;

                    var refGUID = AssetDatabase.AssetPathToGUID(aPath);
                    if (string.IsNullOrEmpty(refGUID)) continue;

                    this.AddUseGUID(refGUID);
                }
            }
        }

        private void ClearUseGUIDs()
        {
#if FR2_DEBUG
		    Debug.Log("ClearUseGUIDs: " + assetPath);
#endif

            this.UseGUIDs.Clear();
            this.UseGUIDsList.Clear();
        }

        private static int binaryLoaded;
        internal void LoadBinaryAsset()
        {
            this.ClearUseGUIDs();

            var assetData = AssetDatabase.LoadAssetAtPath(this.m_assetPath, typeof(UnityObject));
            if (assetData is GameObject)
            {
                this.type = FR2_AssetType.MODEL;
                this.LoadGameObject(assetData as GameObject);
            }
            else if (assetData is TerrainData)
            {
                this.type = FR2_AssetType.TERRAIN;
                this.LoadTerrainData(assetData as TerrainData);
            }
            else
            {
                this.LoadSerialized(assetData);
            }

#if FR2_DEBUG
			Debug.Log("LoadBinaryAsset :: " + assetData + ":" + type);
#endif

            assetData = null;

            if (binaryLoaded++ <= 30) return;
            binaryLoaded = 0;
            FR2_Unity.UnloadUnusedAssets();
        }

        internal void LoadYAML()
        {
            if (!File.Exists(this.m_assetPath))
            {
                this.state = FR2_AssetState.MISSING;
                return;
            }


            if (this.m_isAssetFile)
            {
                var s = AssetDatabase.LoadAssetAtPath<FR2_Cache>(this.m_assetPath);
                if (s != null) return;
            }

            var text = string.Empty;
            try
            {
                text = File.ReadAllText(this.m_assetPath);
            }
#if FR2_DEBUG
            catch (Exception e)
            {
                Debug.LogWarning("Guess Asset Type error :: " + e + "\n" + assetPath);
#else
            catch
            {
#endif
                this.state = FR2_AssetState.MISSING;
                return;
            }

#if FR2_DEBUG
	        Debug.Log("LoadYAML: " + assetPath);
#endif

            //if(assetPath.Contains("Myriad Pro - Bold SDF"))
            //{
            //    Debug.Log("no ne");
            //}
            // PERFORMANCE HOG!
            // var matches = Regex.Matches(text, @"\bguid: [a-f0-9]{32}\b");
            var matches = Regex.Matches(text, @".*guid: [a-f0-9]{32}.*\n");

            foreach (Match match in matches)
            {
                var guidMatch = Regex.Match(match.Value, @"\bguid: [a-f0-9]{32}\b");
                var refGUID   = guidMatch.Value.Replace("guid: ", string.Empty);

                var fileIdMatch = Regex.Match(match.Value, @"\bfileID: ([0-9]*).*");
                var id          = -1;
                try
                {
                    id = int.Parse(fileIdMatch.Groups[1].Value) / 100000;
                }
                catch
                {
                }

                this.AddUseGUID(refGUID, id);
            }

            //var idx = text.IndexOf("guid: ");
            //var counter=0;
            //while (idx != -1)
            //{
            //	var guid = text.Substring(idx + 6, 32);
            //	if (UseGUIDs.Contains(guid)) continue;
            //	AddUseGUID(guid);

            //	idx += 39;
            //	if (idx > text.Length-40) break;

            //	//Debug.Log(assetName + ":" +  guid);
            //	idx = text.IndexOf("guid: ", idx + 39);
            //	if (counter++ > 100) break;
            //}

            //if (counter > 100){
            //	Debug.LogWarning("Never finish on " + assetName);
            //}
        }

        internal void LoadYAML2()
        {
            if (!File.Exists(this.m_assetPath))
            {
                this.state = FR2_AssetState.MISSING;
                return;
            }

            if (this.m_assetPath == "ProjectSettings/EditorBuildSettings.asset")
            {
                var listScenes = EditorBuildSettings.scenes;
                foreach (var scene in listScenes)
                {
                    if (!scene.enabled) continue;
                    var path = scene.path;
                    var guid = AssetDatabase.AssetPathToGUID(path);

                    this.AddUseGUID(guid, 0);

#if FR2_DEBUG
					Debug.Log("AddScene: " + path);
#endif
                }
            }

            // var text = string.Empty;
            try
            {
                using (var sr = new StreamReader(this.m_assetPath))
                {
                    while (sr.Peek() >= 0)
                    {
                        var line  = sr.ReadLine();
                        var index = line.IndexOf("guid: ");
                        if (index < 0) continue;

                        var refGUID     = line.Substring(index + 6, 32);
                        var indexFileId = line.IndexOf("fileID: ");
                        var fileID      = -1;
                        if (indexFileId >= 0)
                        {
                            indexFileId += 8;
                            var fileIDStr =
                                line.Substring(indexFileId, line.IndexOf(',', indexFileId) - indexFileId);
                            try
                            {
                                fileID = int.Parse(fileIDStr) / 100000;
                            }
                            catch
                            {
                            }
                        }

                        this.AddUseGUID(refGUID, fileID);
                    }
                }

#if FR2_DEBUG
	            if (UseGUIDsList.Count > 0)
	            {
	            	Debug.Log(assetPath + ":" + UseGUIDsList.Count);
	            }
#endif
            }
#if FR2_DEBUG
            catch (Exception e)
            {
                Debug.LogWarning("Guess Asset Type error :: " + e + "\n" + assetPath);
#else
            catch
            {
#endif
                this.state = FR2_AssetState.MISSING;
            }
        }

        internal void LoadFolder()
        {
            if (!Directory.Exists(this.m_assetPath))
            {
                this.state = FR2_AssetState.MISSING;
                return;
            }

            // do not analyse folders outside project
            if (!this.m_assetPath.StartsWith("Assets/")) return;


            try
            {
                var files = Directory.GetFiles(this.m_assetPath);
                var dirs  = Directory.GetDirectories(this.m_assetPath);

                foreach (var f in files)
                {
                    if (f.EndsWith(".meta", StringComparison.Ordinal)) continue;

                    var fguid = AssetDatabase.AssetPathToGUID(f);
                    if (string.IsNullOrEmpty(fguid)) continue;

                    // AddUseGUID(fguid, true);
                    this.AddUseGUID(fguid);
                }

                foreach (var d in dirs)
                {
                    var fguid = AssetDatabase.AssetPathToGUID(d);
                    if (string.IsNullOrEmpty(fguid)) continue;

                    // AddUseGUID(fguid, true);
                    this.AddUseGUID(fguid);
                }
            }
#if FR2_DEBUG
            catch (Exception e)
            {
                Debug.LogWarning("LoadFolder() error :: " + e + "\n" + assetPath);
#else
            catch
            {
#endif
                this.state = FR2_AssetState.MISSING;
            }

            //Debug.Log("Load Folder :: " + assetName + ":" + type + ":" + UseGUIDs.Count);
        }


        // ----------------------------- REPLACE GUIDS ---------------------------------------

        internal bool ReplaceReference(string fromGUID, string toGUID, TerrainData terrain = null)
        {
            if (this.IsMissing) return false;

            if (this.IsReferencable)
            {
                var text = string.Empty;

                if (!File.Exists(this.m_assetPath))
                {
                    this.state = FR2_AssetState.MISSING;
                    return false;
                }

                try
                {
                    text = File.ReadAllText(this.m_assetPath).Replace("\r", "\n");
                    File.WriteAllText(this.m_assetPath, text.Replace(fromGUID, toGUID));
                    // AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.Default);
                    return true;
                }
                catch (Exception e)
                {
                    this.state = FR2_AssetState.MISSING;
                    //#if FR2_DEBUG
                    Debug.LogWarning("Replace Reference error :: " + e + "\n" + this.m_assetPath);
                    //#endif
                }

                return false;
            }

            if (this.type == FR2_AssetType.TERRAIN)
            {
                var fromObj = FR2_Unity.LoadAssetWithGUID<UnityObject>(fromGUID);
                var toObj   = FR2_Unity.LoadAssetWithGUID<UnityObject>(toGUID);
                var found   = 0;
                // var terrain = AssetDatabase.LoadAssetAtPath(assetPath, typeof(Object)) as TerrainData;

                if (fromObj is Texture2D)
                {
                    var arr = terrain.detailPrototypes;
                    for (var i = 0; i < arr.Length; i++)
                        if (arr[i].prototypeTexture == (Texture2D)fromObj)
                        {
                            found++;
                            arr[i].prototypeTexture = (Texture2D)toObj;
                        }

                    terrain.detailPrototypes = arr;
                    FR2_Unity.ReplaceTerrainTextureDatas(terrain, (Texture2D)fromObj, (Texture2D)toObj);
                }

                if (fromObj is GameObject)
                {
                    var arr2 = terrain.treePrototypes;
                    for (var i = 0; i < arr2.Length; i++)
                        if (arr2[i].prefab == (GameObject)fromObj)
                        {
                            found++;
                            arr2[i].prefab = (GameObject)toObj;
                        }

                    terrain.treePrototypes = arr2;
                }

                // EditorUtility.SetDirty(terrain);
                // AssetDatabase.SaveAssets();

                fromObj = null;
                toObj   = null;
                terrain = null;
                // FR2_Unity.UnloadUnusedAssets();

                return found > 0;
            }

            Debug.LogWarning("Something wrong, should never be here - Ignored <" + this.m_assetPath +
                             "> : not a readable type, can not replace ! " + this.type);
            return false;
        }

        internal bool ReplaceReference(string fromGUID, string toGUID, long toFileId, TerrainData terrain = null)
        {
            // Debug.Log("ReplaceReference: from " + fromGUID + "  to: " + toGUID + "  toFileId: " + toFileId);

            if (this.IsMissing)
                //				Debug.Log("this asset is missing");
                return false;

            if (this.IsReferencable)
            {
                if (!File.Exists(this.m_assetPath))
                {
                    this.state = FR2_AssetState.MISSING;
                    //					Debug.Log("this asset not exits");
                    return false;
                }

                try
                {
                    var sb    = new StringBuilder();
                    var text  = File.ReadAllText(this.assetPath).Replace("\r", "\n");
                    var lines = text.Split('\n');
                    //string result = "";
                    for (var i = 0; i < lines.Length; i++)
                    {
                        var line = lines[i];
                        if (line.IndexOf(fromGUID, StringComparison.Ordinal) >= 0)
                        {
                            if (toFileId > 0)
                            {
                                const string FileID = "fileID: ";
                                var          index  = line.IndexOf(FileID, StringComparison.Ordinal);
                                if (index >= 0)
                                {
                                    var  fromFileId = line.Substring(index + FileID.Length, line.IndexOf(',', index) - (index + FileID.Length));
                                    long fileType   = 0;
                                    if (!long.TryParse(fromFileId, out fileType))
                                    {
                                        Debug.LogWarning("cannot parse file");
                                        return false;
                                    }

                                    if (fileType.ToString().Substring(0, 3) != toFileId.ToString().Substring(0, 3))
                                    {
                                        //difference file type
                                        Debug.LogWarning("Difference file type");
                                        return false;
                                    }

                                    Debug.Log("ReplaceReference: fromFileId " + fromFileId + "  to File Id " + toFileId);
                                    line = line.Replace(fromFileId, toFileId.ToString());
                                }
                            }

                            line = line.Replace(fromGUID, toGUID);
                        }

                        sb.Append(line);
                        sb.AppendLine();
                        //result += line + "\n";
                    }

                    //File.WriteAllText(assetPath, result.Trim());
                    File.WriteAllText(this.assetPath, sb.ToString());
                    //AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.Default);
                    return true;
                }
                catch (Exception e)
                {
                    this.state = FR2_AssetState.MISSING;
                    //#if FR2_DEBUG
                    Debug.LogWarning("Replace Reference error :: " + e + "\n" + this.m_assetPath);
                    //#endif
                }

                return false;
            }

            if (this.type == FR2_AssetType.TERRAIN)
            {
                var fromObj = FR2_Unity.LoadAssetWithGUID<UnityObject>(fromGUID);
                var toObj   = FR2_Unity.LoadAssetWithGUID<UnityObject>(toGUID);
                var found   = 0;
                // var terrain = AssetDatabase.LoadAssetAtPath(assetPath, typeof(Object)) as TerrainData;

                if (fromObj is Texture2D)
                {
                    var arr = terrain.detailPrototypes;
                    for (var i = 0; i < arr.Length; i++)
                        if (arr[i].prototypeTexture == (Texture2D)fromObj)
                        {
                            found++;
                            arr[i].prototypeTexture = (Texture2D)toObj;
                        }

                    terrain.detailPrototypes = arr;
                    FR2_Unity.ReplaceTerrainTextureDatas(terrain, (Texture2D)fromObj, (Texture2D)toObj);
                }

                if (fromObj is GameObject)
                {
                    var arr2 = terrain.treePrototypes;
                    for (var i = 0; i < arr2.Length; i++)
                        if (arr2[i].prefab == (GameObject)fromObj)
                        {
                            found++;
                            arr2[i].prefab = (GameObject)toObj;
                        }

                    terrain.treePrototypes = arr2;
                }

                // EditorUtility.SetDirty(terrain);
                // AssetDatabase.SaveAssets();

                // FR2_Unity.UnloadUnusedAssets();

                return found > 0;
            }

            Debug.LogWarning("Something wrong, should never be here - Ignored <" + this.m_assetPath +
                             "> : not a readable type, can not replace ! " + this.type);
            return false;
        }

        [Serializable]
        private class Classes
        {
            public string    guid;
            public List<int> ids;
        }
    }
}