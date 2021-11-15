//#define FR2_DEBUG

using CBParams = System.Collections.Generic.List<System.Collections.Generic.List<string>>;

namespace vietlabs.fr2
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using UnityEditor;
    using UnityEngine;
    using Object = UnityEngine.Object;

    internal class FR2_DuplicateTree2 : IRefDraw
    {
        private const float TimeDelayDelete = .5f;

        private static readonly FR2_FileCompare                   fc = new FR2_FileCompare();
        private readonly        FR2_TreeUI2.GroupDrawer           groupDrawer;
        private                 CBParams                          cacheAssetList;
        public                  bool                              caseSensitive = false;
        private                 Dictionary<string, List<FR2_Ref>> dicIndex; //index, list

        private          bool                        dirty;
        private          int                         excludeCount;
        private          string                      guidPressDelete;
        internal         List<FR2_Ref>               list;
        internal         Dictionary<string, FR2_Ref> refs;
        public           int                         scanExcludeByIgnoreCount;
        public           int                         scanExcludeByTypeCount;
        private readonly string                      searchTerm = "";
        private          float                       TimePressDelete;

        public FR2_DuplicateTree2(IWindow window)
        {
            this.window      = window;
            this.groupDrawer = new FR2_TreeUI2.GroupDrawer(this.DrawGroup, this.DrawAsset);
        }

        public IWindow window { get; set; }

        public bool Draw(Rect rect) { return false; }

        public bool DrawLayout()
        {
            if (this.dirty) this.RefreshView(this.cacheAssetList);

            if (fc.nChunks2 > 0 && fc.nScaned < fc.nChunks2)
            {
                var rect = GUILayoutUtility.GetRect(1, Screen.width, 18f, 18f);
                var p    = fc.nScaned / (float)fc.nChunks2;

                EditorGUI.ProgressBar(rect, p, string.Format("Scanning {0} / {1}", fc.nScaned, fc.nChunks2));
                GUILayout.FlexibleSpace();
                return true;
            }

            if (this.groupDrawer.hasValidTree) this.groupDrawer.tree.itemPaddingRight = 60f;
            this.groupDrawer.DrawLayout();
            this.DrawHeader();
            return false;
        }

        public int ElementCount() { return this.list == null ? 0 : this.list.Count; }

        private void DrawAsset(Rect r, string guid)
        {
            FR2_Ref rf;
            if (!this.refs.TryGetValue(guid, out rf)) return;

            rf.asset.Draw(r, false,
                FR2_Setting.GroupMode != FR2_RefDrawer.Mode.Folder,
                FR2_Setting.ShowFileSize,
                FR2_Setting.s.displayAssetBundleName,
                FR2_Setting.s.displayAtlasName,
                FR2_Setting.s.showUsedByClassed, this.window);

            var tex = AssetDatabase.GetCachedIcon(rf.asset.assetPath);
            if (tex == null) return;

            var drawR = r;
            drawR.x      =  drawR.x + drawR.width; // (groupDrawer.TreeNoScroll() ? 60f : 70f) ;
            drawR.width  =  40f;
            drawR.y      += 1;
            drawR.height -= 2;

            if (GUI.Button(drawR, "Use", EditorStyles.miniButton))
            {
                if (FR2_Export.IsMergeProcessing)
                {
                    Debug.LogWarning("Previous merge is processing");
                }
                else
                {
                    //AssetDatabase.SaveAssets();
                    //EditorGUIUtility.systemCopyBuffer = rf.asset.guid;
                    //EditorGUIUtility.systemCopyBuffer = rf.asset.guid;
                    // Debug.Log("guid: " + rf.asset.guid + "  systemCopyBuffer " + EditorGUIUtility.systemCopyBuffer);
                    var index = rf.index;
                    Selection.objects = this.list.Where(x => x.index == index)
                        .Select(x => FR2_Unity.LoadAssetAtPath<Object>(x.asset.assetPath)).ToArray();
                    FR2_Export.MergeDuplicate(rf.asset.guid);
                }
            }

            if (rf.asset.UsageCount() > 0) return;

            drawR.x     -= 25;
            drawR.width =  20;
            if (this.wasPreDelete(guid))
            {
                var col = GUI.color;
                GUI.color = Color.red;
                if (GUI.Button(drawR, "X", EditorStyles.miniButton))
                {
                    this.guidPressDelete = null;
                    AssetDatabase.DeleteAsset(rf.asset.assetPath);
                }

                GUI.color               = col;
                this.window.WillRepaint = true;
            }
            else
            {
                if (GUI.Button(drawR, "X", EditorStyles.miniButton))
                {
                    this.guidPressDelete    = guid;
                    this.TimePressDelete    = Time.realtimeSinceStartup;
                    this.window.WillRepaint = true;
                }
            }
        }

        private bool wasPreDelete(string guid)
        {
            if (this.guidPressDelete == null || guid != this.guidPressDelete) return false;

            if (Time.realtimeSinceStartup - this.TimePressDelete < TimeDelayDelete) return true;

            this.guidPressDelete = null;
            return false;
        }

        private void DrawGroup(Rect r, string label, int childCount)
        {
            // GUI.Label(r, label + " (" + childCount + ")", EditorStyles.boldLabel);
            var asset = this.dicIndex[label][0].asset;

            var tex  = AssetDatabase.GetCachedIcon(asset.assetPath);
            var rect = r;

            if (tex != null)
            {
                rect.width = 16f;
                GUI.DrawTexture(rect, tex);
            }

            rect      =  r;
            rect.xMin += 16f;
            GUI.Label(rect, asset.assetName, EditorStyles.boldLabel);

            rect      =  r;
            rect.xMin += rect.width - 50f;
            GUI.Label(rect, FR2_Helper.GetfileSizeString(asset.fileSize), EditorStyles.miniLabel);

            rect      =  r;
            rect.xMin += rect.width - 70f;
            GUI.Label(rect, childCount.ToString(), EditorStyles.miniLabel);

            rect      =  r;
            rect.xMin += rect.width - 70f;
        }


        // private List<FR2_DuplicateFolder> duplicated;

        public void Reset(CBParams assetList) { fc.Reset(assetList, this.OnUpdateView, this.RefreshView); }

        private void OnUpdateView(CBParams assetList) { }

        public bool isExclueAnyItem() { return this.excludeCount > 0 || this.scanExcludeByTypeCount > 0; }

        public bool isExclueAnyItemByIgnoreFolder() { return this.scanExcludeByIgnoreCount > 0; }

        // void OnActive
        private void RefreshView(CBParams assetList)
        {
            this.cacheAssetList = assetList;
            this.dirty          = false;
            this.list           = new List<FR2_Ref>();
            this.refs           = new Dictionary<string, FR2_Ref>();
            this.dicIndex       = new Dictionary<string, List<FR2_Ref>>();
            if (assetList == null) return;

            var minScore                   = this.searchTerm.Length;
            var term1                      = this.searchTerm;
            if (!this.caseSensitive) term1 = term1.ToLower();

            var term2 = term1.Replace(" ", string.Empty);
            this.excludeCount = 0;

            for (var i = 0; i < assetList.Count; i++)
            {
                var lst = new List<FR2_Ref>();
                for (var j = 0; j < assetList[i].Count; j++)
                {
                    var path = assetList[i][j];
                    if (!path.StartsWith("Assets/"))
                    {
                        Debug.LogWarning("Ignore asset: " + path);
                        continue;
                    }

                    var guid = AssetDatabase.AssetPathToGUID(path);
                    if (string.IsNullOrEmpty(guid)) continue;

                    if (this.refs.ContainsKey(guid)) continue;

                    var asset = FR2_Cache.Api.Get(guid);
                    if (asset == null) continue;
                    if (!asset.assetPath.StartsWith("Assets/")) continue; // ignore builtin, packages, ...

                    var fr2 = new FR2_Ref(i, 0, asset, null);

                    if (FR2_Setting.IsTypeExcluded(fr2.type))
                    {
                        this.excludeCount++;
                        continue; //skip this one
                    }

                    if (string.IsNullOrEmpty(this.searchTerm))
                    {
                        fr2.matchingScore = 0;
                        this.list.Add(fr2);
                        lst.Add(fr2);
                        this.refs.Add(guid, fr2);
                        continue;
                    }

                    //calculate matching score
                    var name1                      = fr2.asset.assetName;
                    if (!this.caseSensitive) name1 = name1.ToLower();

                    var name2 = name1.Replace(" ", string.Empty);

                    var score1 = FR2_Unity.StringMatch(term1, name1);
                    var score2 = FR2_Unity.StringMatch(term2, name2);

                    fr2.matchingScore = Mathf.Max(score1, score2);
                    if (fr2.matchingScore > minScore)
                    {
                        this.list.Add(fr2);
                        lst.Add(fr2);
                        this.refs.Add(guid, fr2);
                    }
                }

                this.dicIndex.Add(i.ToString(), lst);
            }

            this.ResetGroup();
        }

        private void ResetGroup()
        {
            this.groupDrawer.Reset(this.list,
                rf => rf.asset.guid
                , this.GetGroup, this.SortGroup);
            if (this.window != null) this.window.Repaint();
        }

        private string GetGroup(FR2_Ref rf) { return rf.index.ToString(); }

        private void SortGroup(List<string> groups)
        {
            // groups.Sort( (item1, item2) =>
            // {
            // 	if (item1 == "Others" || item2 == "Selection") return 1;
            // 	if (item2 == "Others" || item1 == "Selection") return -1;
            // 	return item1.CompareTo(item2);
            // });
        }

        public void SetDirty() { this.dirty = true; }

        public void RefreshSort() { }

        private void DrawHeader()
        {
            var text = this.groupDrawer.hasValidTree ? "Rescan" : "Scan";

            if (GUILayout.Button(text))
                // if (FR2_Cache)
            {
                this.OnCacheReady();
            }

            // FR2_Cache.onReady -= OnCacheReady;
            // FR2_Cache.onReady += OnCacheReady;
            // FR2_Cache.Api.Check4Changes(false);
        }

        private void OnCacheReady()
        {
            this.scanExcludeByTypeCount = 0;
            this.Reset(FR2_Cache.Api.ScanSimilar(this.IgnoreTypeWhenScan, this.IgnoreFolderWhenScan));
            FR2_Cache.onReady -= this.OnCacheReady;
        }

        private void IgnoreTypeWhenScan() { this.scanExcludeByTypeCount++; }

        private void IgnoreFolderWhenScan() { this.scanExcludeByIgnoreCount++; }
    }

    internal class FR2_FileCompare
    {
        public static   HashSet<FR2_Chunk> HashChunksNotComplete;
        internal static int                streamClosedCount;
        private         CBParams           cacheList;
        public          List<FR2_Head>     deads = new List<FR2_Head>();
        public          List<FR2_Head>     heads = new List<FR2_Head>();

        public int              nChunks;
        public int              nChunks2;
        public int              nScaned;
        public Action<CBParams> OnCompareComplete;

        public Action<CBParams> OnCompareUpdate;
        // private int streamCount;

        public void Reset(CBParams list, Action<CBParams> onUpdate, Action<CBParams> onComplete)
        {
            this.nChunks  = 0;
            this.nScaned  = 0;
            this.nChunks2 = 0;
            // streamCount = streamClosedCount = 0;
            HashChunksNotComplete = new HashSet<FR2_Chunk>();

            if (this.heads.Count > 0)
                for (var i = 0; i < this.heads.Count; i++)
                    this.heads[i].CloseChunk();

            this.deads.Clear();
            this.heads.Clear();

            this.OnCompareUpdate   = onUpdate;
            this.OnCompareComplete = onComplete;
            if (list.Count <= 0)
            {
                this.OnCompareComplete(new CBParams());
                return;
            }

            this.cacheList = list;
            for (var i = 0; i < list.Count; i++)
            {
                var file   = new FileInfo(list[i][0]);
                var nChunk = Mathf.CeilToInt(file.Length / (float)FR2_Head.chunkSize);
                this.nChunks2 += nChunk;
            }

            // for(int i =0;i< list.Count;i++)
            // {
            //     AddHead(list[i]);
            // }
            this.AddHead(this.cacheList[this.cacheList.Count - 1]);
            this.cacheList.RemoveAt(this.cacheList.Count - 1);

            EditorApplication.update -= this.ReadChunkAsync;
            EditorApplication.update += this.ReadChunkAsync;
        }

        public FR2_FileCompare AddHead(List<string> files)
        {
            if (files.Count < 2) Debug.LogWarning("Something wrong ! head should not contains < 2 elements");

            var chunkList = new List<FR2_Chunk>();
            for (var i = 0; i < files.Count; i++)
                // streamCount++;

                // try 
                // {
                // 	Debug.Log("new stream ");
                // 	stream = new FileStream(files[i], FileMode.Open, FileAccess.Read);
                // }
                // catch (Exception e)
                // {
                // 	Debug.LogWarning(e + "\nCan not open file: " + files[i]);
                // 	if (stream != null) stream.Close();
                // 	continue;
                // }

                chunkList.Add(new FR2_Chunk
                {
                    file   = files[i],
                    buffer = new byte[FR2_Head.chunkSize]
                });

            var file   = new FileInfo(files[0]);
            var nChunk = Mathf.CeilToInt(file.Length / (float)FR2_Head.chunkSize);

            this.heads.Add(new FR2_Head
            {
                fileSize     = file.Length,
                currentChunk = 0,
                nChunk       = nChunk,
                chunkList    = chunkList
            });

            this.nChunks += nChunk;

            return this;
        }

        // private bool checkCompleteAllCurFile()
        // {
        // 	return streamClosedCount + HashChunksNotComplete.Count >= streamCount; //-1 for safe
        // }

        private void ReadChunkAsync()
        {
            var alive = this.ReadChunk();
            if (alive) return;

            if (this.cacheList.Count > 0)
            {
                this.AddHead(this.cacheList[this.cacheList.Count - 1]);
                this.cacheList.RemoveAt(this.cacheList.Count - 1);
            }

            var update = false;
            for (var i = this.heads.Count - 1; i >= 0; i--)
            {
                var h = this.heads[i];
                if (!h.isDead) continue;

                h.CloseChunk();
                this.heads.RemoveAt(i);
                if (h.chunkList.Count > 1)
                {
                    update = true;
                    this.deads.Add(h);
                }
            }

            if (update) this.Trigger(this.OnCompareUpdate);

            if (!alive && this.cacheList.Count <= 0) //&& cacheList.Count <= 0 complete all chunk and cache list empty
            {
                foreach (var item in HashChunksNotComplete)
                    if (item.stream != null && item.stream.CanRead)
                    {
                        Debug.Log("Close Stream!");

                        item.stream.Close();
                        item.stream = null;
                    }

                HashChunksNotComplete.Clear();
                // Debug.Log("complete ");
                this.nScaned             =  this.nChunks;
                EditorApplication.update -= this.ReadChunkAsync;
                this.Trigger(this.OnCompareComplete);
            }
        }

        private void Trigger(Action<CBParams> cb)
        {
            if (cb == null) return;

            var list = this.deads.Select(item => item.GetFiles()).ToList();

            //#if FR2_DEBUG
            //        Debug.Log("Callback ! " + deads.Count + ":" + heads.Count);
            //#endif
            cb(list);
        }

        private bool ReadChunk()
        {
            var alive = false;

            for (var i = 0; i < this.heads.Count; i++)
            {
                var h = this.heads[i];
                if (h.isDead)
                    //Debug.LogWarning("Should never be here : " + h.chunkList[0].file);
                    continue;

                this.nScaned++;
                alive = true;
                h.ReadChunk();
                h.CompareChunk(this.heads);
                break;
            }

            //if (!alive) return false;

            //alive = false;
            //for (var i = 0; i < heads.Count; i++)
            //{
            //    var h = heads[i];
            //    if (h.isDead) continue;

            //    h.CompareChunk(heads);
            //    alive |= !h.isDead;
            //}

            return alive;
        }
    }

    internal class FR2_Head
    {
        public const int chunkSize = 10240;

        public List<FR2_Chunk> chunkList;
        public int             currentChunk;

        public long fileSize;

        public int nChunk;
        public int size; //last stream read size

        public bool isDead => this.currentChunk == this.nChunk || this.chunkList.Count == 1;

        public List<string> GetFiles() { return this.chunkList.Select(item => item.file).ToList(); }

        public void AddToDict(byte b, FR2_Chunk chunk, Dictionary<byte, List<FR2_Chunk>> dict)
        {
            List<FR2_Chunk> list;
            if (!dict.TryGetValue(b, out list))
            {
                list = new List<FR2_Chunk>();
                dict.Add(b, list);
            }

            list.Add(chunk);
        }

        public void CloseChunk()
        {
            for (var i = 0; i < this.chunkList.Count; i++)
            {
                FR2_FileCompare.streamClosedCount++;

                if (this.chunkList[i].stream != null)
                {
#if FR2_DEBUG
					Debug.Log("stream close: " + chunkList[i].file);
#endif

                    this.chunkList[i].stream.Close();
                    this.chunkList[i].stream = null;
                }
            }
        }

        public void ReadChunk()
        {
#if FR2_DEBUG
        if (currentChunk == 0) Debug.LogWarning("Read <" + chunkList[0].file + "> " + currentChunk + ":" + nChunk);
#endif
            if (this.currentChunk == this.nChunk)
            {
                Debug.LogWarning("Something wrong, should dead <" + this.isDead + ">");
                return;
            }

            var from = this.currentChunk * chunkSize;
            this.size = (int)Mathf.Min(this.fileSize - from, chunkSize);

            for (var i = 0; i < this.chunkList.Count; i++)
            {
                var chunk = this.chunkList[i];
                if (chunk.streamError) continue;
                chunk.size = this.size;

                if (chunk.streamInited == false)
                {
                    chunk.streamInited = true;

                    try
                    {
#if FR2_DEBUG
						Debug.Log("New chunk: " + chunk.file);
#endif
                        chunk.stream = new FileStream(chunk.file, FileMode.Open, FileAccess.Read);
                    }
#if FR2_DEBUG
                    catch (Exception e)
                    {
						
						Debug.LogWarning("Exception: " + e + "\n" + chunk.file + "\n" + chunk.stream);
#else
                    catch
                    {
#endif

                        chunk.streamError = true;
                        if (chunk.stream != null) // just to make sure we close the stream
                        {
                            chunk.stream.Close();
                            chunk.stream = null;
                        }
                    }

                    if (chunk.stream == null)
                    {
                        chunk.streamError = true;
                        continue;
                    }
                }

                try
                {
                    chunk.stream.Read(chunk.buffer, 0, this.size);
                }
                catch (Exception e)
                {
                    Debug.LogWarning(e + "\n" + chunk.file);

                    chunk.streamError = true;
                    chunk.stream.Close();
                }
            }

            // clean up dead chunks
            for (var i = this.chunkList.Count - 1; i >= 0; i--)
                if (this.chunkList[i].streamError)
                    this.chunkList.RemoveAt(i);

            if (this.chunkList.Count == 1) Debug.LogWarning("No more chunk in list");

            this.currentChunk++;
        }

        public void CompareChunk(List<FR2_Head> heads)
        {
            var idx    = this.chunkList.Count;
            var buffer = this.chunkList[idx - 1].buffer;

            while (--idx >= 0)
            {
                var chunk = this.chunkList[idx];
                var diff  = FirstDifferentIndex(buffer, chunk.buffer, this.size);
                if (diff == -1) continue;
#if FR2_DEBUG
            Debug.Log(string.Format(
                " --> Different found at : idx={0} diff={1} size={2} chunk={3}",
            idx, diff, size, currentChunk));
#endif

                var v = buffer[diff];
                var d = new Dictionary<byte, List<FR2_Chunk>>(); //new heads
                this.chunkList.RemoveAt(idx);
                FR2_FileCompare.HashChunksNotComplete.Add(chunk);

                this.AddToDict(chunk.buffer[diff], chunk, d);

                for (var j = idx - 1; j >= 0; j--)
                {
                    var tChunk = this.chunkList[j];
                    var tValue = tChunk.buffer[diff];
                    if (tValue == v) continue;

                    idx--;
                    FR2_FileCompare.HashChunksNotComplete.Add(tChunk);
                    this.chunkList.RemoveAt(j);
                    this.AddToDict(tChunk.buffer[diff], tChunk, d);
                }

                foreach (var item in d)
                {
                    var list = item.Value;
                    if (list.Count == 1)
                    {
#if FR2_DEBUG
                    Debug.Log(" --> Dead head found for : " + list[0].file);
#endif
                        if (list[0].stream != null) list[0].stream.Close();
                    }
                    else if (list.Count > 1) // 1 : dead head
                    {
#if FR2_DEBUG
                    Debug.Log(" --> NEW HEAD : " + list[0].file);
#endif
                        heads.Add(new FR2_Head
                        {
                            nChunk       = this.nChunk,
                            fileSize     = this.fileSize,
                            currentChunk = this.currentChunk - 1,
                            chunkList    = list
                        });
                    }
                }
            }
        }

        internal static int FirstDifferentIndex(byte[] arr1, byte[] arr2, int maxIndex)
        {
            for (var i = 0; i < maxIndex; i++)
                if (arr1[i] != arr2[i])
                    return i;

            return -1;
        }
    }

    internal class FR2_Chunk
    {
        public byte[] buffer;
        public string file;
        public long   size;

        public bool       streamInited;
        public bool       streamError;
        public FileStream stream;
    }
}