//
//███████╗██╗░██████╗░███╗░░░███╗░█████╗░  ░█████╗░░█████╗░███╗░░██╗██╗░░░██╗███████╗██████╗░████████╗███████╗██████╗░
//██╔════╝██║██╔════╝░████╗░████║██╔══██╗  ██╔══██╗██╔══██╗████╗░██║██║░░░██║██╔════╝██╔══██╗╚══██╔══╝██╔════╝██╔══██╗
//█████╗░░██║██║░░██╗░██╔████╔██║███████║  ██║░░╚═╝██║░░██║██╔██╗██║╚██╗░██╔╝█████╗░░██████╔╝░░░██║░░░█████╗░░██████╔╝
//██╔══╝░░██║██║░░╚██╗██║╚██╔╝██║██╔══██║  ██║░░██╗██║░░██║██║╚████║░╚████╔╝░██╔══╝░░██╔══██╗░░░██║░░░██╔══╝░░██╔══██╗
//██║░░░░░██║╚██████╔╝██║░╚═╝░██║██║░░██║  ╚█████╔╝╚█████╔╝██║░╚███║░░╚██╔╝░░███████╗██║░░██║░░░██║░░░███████╗██║░░██║
//╚═╝░░░░░╚═╝░╚═════╝░╚═╝░░░░░╚═╝╚═╝░░╚═╝  ░╚════╝░░╚════╝░╚═╝░░╚══╝░░░╚═╝░░░╚══════╝╚═╝░░╚═╝░░░╚═╝░░░╚══════╝╚═╝░░╚═╝
//
//███████╗░█████╗░██████╗░  ██╗░░░██╗███╗░░██╗██╗████████╗██╗░░░██╗
//██╔════╝██╔══██╗██╔══██╗  ██║░░░██║████╗░██║██║╚══██╔══╝╚██╗░██╔╝
//█████╗░░██║░░██║██████╔╝  ██║░░░██║██╔██╗██║██║░░░██║░░░░╚████╔╝░
//██╔══╝░░██║░░██║██╔══██╗  ██║░░░██║██║╚████║██║░░░██║░░░░░╚██╔╝░░
//██║░░░░░╚█████╔╝██║░░██║  ╚██████╔╝██║░╚███║██║░░░██║░░░░░░██║░░░
//╚═╝░░░░░░╚════╝░╚═╝░░╚═╝  ░╚═════╝░╚═╝░░╚══╝╚═╝░░░╚═╝░░░░░░╚═╝░░░
//

#if UNITY_EDITOR

using DA_Assets.Exceptions;
using DA_Assets.Extensions;
using DA_Assets.Model;
using System;
using System.Collections.Generic;
using System.Linq;
#if TMPRO_EXISTS
using TMPro;
#endif
using UnityEngine;

namespace DA_Assets
{
    [ExecuteInEditMode]
    public class FigmaConverterUnity : MonoBehaviour
    {
        public MainSettings mainSettings = DefaultSettings.mainSettings;
        public StandardTextSettings defaultTextSettings = DefaultSettings.defaultTextSettings;
        public ProceduralImageSettings proceduralImageSettings = DefaultSettings.proceduralImageSettings;
        public List<CustomPrefab> customPrefabs = new List<CustomPrefab>();
    

#if TMPRO_EXISTS
        public TextMeshProSettings textMeshProSettings = DefaultSettings.textMeshProSettings;
#endif
    /// <summary> Buffer for hamburger menu items. </summary>
    public bool[] itemsBuffer = new bool[32];
#if I2LOC_EXISTS
        public I2.Loc.LanguageSource languageSource;
#endif
#if JSON_NET_EXISTS

        public bool drawFrameButtonVisible = false;
        /// <summary> Array with fonts used in figma layout. </summary>
        public List<Font> fonts;
#if TMPRO_EXISTS
        public List<TMP_FontAsset> textMeshProFonts;
#endif
        /// <summary> Downloadable figma frames. </summary>
        public List<SelectableFObject> framesToDownload = new List<SelectableFObject>();
        public List<SelectableFObject> pagesForSelect = new List<SelectableFObject>();

        private FObject selectedPage;

        /// <summary> Downloaded figma frames. </summary>
        private List<FObject> downloadedFrames = new List<FObject>();
        private List<FObject> downloadedPages = new List<FObject>();

        private static FigmaConverterUnity instance;

        public static FigmaConverterUnity Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindObjectOfType<FigmaConverterUnity>();
                }

                return instance;
            }
        }

        public async void AuthWithBrowser()
        {
            mainSettings.ApiKey = await WebClient.Authorize();
            Console.WriteLine(Localization.AUTH_COMPLETE);
        }
        public async void DownloadProject()
        {
            downloadedPages.Clear();
            pagesForSelect.Clear();
            framesToDownload.Clear();
            downloadedFrames.Clear();

            Checkers.IsValidSettings();

            Console.WriteLine(Localization.STARTING_PROJECT_DOWNLOAD);
#if I2LOC_EXISTS
            if (mainSettings.UseI2Localization)
            {
                CanvasDrawer.InstantiateI2LocalizationSource();
            }
#endif

            FigmaProject fproject = await WebClient.GetProject();
            downloadedPages = fproject.Document.Children.ToList();

            pagesForSelect = new List<SelectableFObject>();
            foreach (FObject page in downloadedPages)
            {
                page.FTag = FTag.Page;
                pagesForSelect.Add(new SelectableFObject
                {
                    Id = page.Id,
                    Name = page.Name,
                    Selected = false
                });
            }

            Console.WriteLine(Localization.PROJECT_DOWNLOADED);
        }

        public void GetFramesFromSelectedPage()
        {
            framesToDownload.Clear();
            downloadedFrames.Clear();

            SelectableFObject _selectedPage = pagesForSelect.FirstOrDefault(x => x.Selected == true);
            if (_selectedPage == null)
            {
                throw new NoSelectedPageException();
            }

            selectedPage = downloadedPages.FirstOrDefault(x => x.Id == _selectedPage.Id);

            framesToDownload = new List<SelectableFObject>();

            foreach (FObject frame in selectedPage.Children)
            {
                if (frame.Type == FTag.Frame.GetDescription().ToUpper())
                {
                    frame.FTag = FTag.Frame;
                    framesToDownload.Add(new SelectableFObject
                    {
                        Id = frame.Id,
                        Name = frame.Name,
                        Selected = true
                    });
                }
            }

            if (framesToDownload.Count > 0)
            {
                Console.WriteLine(string.Format(Localization.FRAMES_FINDED, framesToDownload.Count, selectedPage.Name));
            }
            else
            {
                Console.Error(Localization.FRAMES_NOT_FINDED);
            }
            
        }

        public async void DownloadSelectedFrames()
        {
            List<FObject> frames = new List<FObject>();
            List<SelectableFObject> selectedFrames = framesToDownload.Where(x => x.Selected == true).ToList();

            foreach (SelectableFObject frame in selectedFrames)
            {
                List<FObject> _frames = selectedPage.Children.Where(x => x.Id == frame.Id).ToList();
                frames.AddRange(_frames);
            }

            selectedPage.Children = frames;

            List<FObject> parsedFObjects = FigmaParser.GetChildrenOfPage(selectedPage);
            List<FObject> fobjectsWithRootFrames = FigmaParser.GetSetRootFrameForFObjects(parsedFObjects);
            List<FObject> fobjectsCheckedForMutualFObjects = FigmaParser.GetMutualFObjects(frames, parsedFObjects);

            List<FObject> linked = await WebClient.GetImageLinksForFObjects(fobjectsCheckedForMutualFObjects);
            downloadedFrames = await WebClient.DownloadSpritesAsync(linked);
            drawFrameButtonVisible = true;
        }
        public void DrawDownloadedFrames()
        {
            transform.localScale = new Vector3(1, 1, 1);
            CanvasDrawer.DrawToCanvas(downloadedFrames);
            drawFrameButtonVisible = false;
           
            Console.Success(Localization.IMPORT_COMPLETE);
        }

#endif
    }
}

public class SelectableFObject
{
    public string Id;
    public string Name;
    public bool Selected;
}
[Serializable]
public class CustomPrefab
{
    public string Tag;
    public GameObject Prefab;
}
#endif