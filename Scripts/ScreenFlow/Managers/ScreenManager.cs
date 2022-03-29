namespace GameFoundation.Scripts.ScreenFlow.Managers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Cysharp.Threading.Tasks;
    using GameFoundation.Scripts.AssetLibrary;
    using GameFoundation.Scripts.CommonScreen;
    using GameFoundation.Scripts.ScreenFlow.BaseScreen.Presenter;
    using GameFoundation.Scripts.ScreenFlow.BaseScreen.View;
    using GameFoundation.Scripts.ScreenFlow.Signals;
    using GameFoundation.Scripts.Utilities.Extension;
    using GameFoundation.Scripts.Utilities.LogService;
    using UnityEngine;
    using Zenject;

    /// <summary>
    /// Control open and close flow of all screens
    /// </summary>
    public interface IScreenManager
    {
        /// <summary>
        /// Get instance of a screen
        /// </summary>
        /// <typeparam name="T">Type of screen presenter</typeparam>
        public UniTask<T> GetScreen<T>() where T : IScreenPresenter;

        /// <summary>
        /// Open a screen by type
        /// </summary>
        /// <typeparam name="T">Type of screen presenter</typeparam>
        public UniTask<T> OpenScreen<T>() where T : IScreenPresenter;

        public UniTask<TPresenter> OpenScreen<TPresenter, TModel>(TModel model) where TPresenter : IScreenPresenter<TModel>;

        /// <summary>
        /// Close a screen on top
        /// </summary>
        public Task CloseCurrentScreen();

        /// <summary>
        /// Close all screen on current scene
        /// </summary>
        public void CloseAllScreen();

        /// <summary>
        /// Cleanup/ destroy all screen on current scene
        /// </summary>
        public void CleanUpAllScreen();

        /// <summary>
        /// Get root transform of all screen, used as the parent transform of each screen
        /// </summary>
        public Transform CurrentRootScreen { get; set; }

        public Transform CurrentHiddenRoot { get; set; }

        /// <summary>
        /// Reference to low level DiContainer follow scene
        /// </summary>
        public IInstantiator Instantiator { get; set; }

        /// <summary>
        /// Current screen shown on top.
        /// </summary>
        public IScreenPresenter CurrentScreen { get; }
    }

    public class ScreenManager : MonoBehaviour, IScreenManager, IDisposable
    {
        #region Properties

        /// <summary>
        /// List of active screens
        /// </summary>
        [SerializeField] private List<IScreenPresenter> activeScreens;

        /// <summary>
        /// Current screen shown on top.
        /// </summary>
        [SerializeField] private IScreenPresenter currentActiveScreen;

        [SerializeField] private IScreenPresenter previousActiveScreen;

        private Dictionary<Type, IScreenPresenter>       typeToLoadedScreenPresenter;
        private Dictionary<Type, Task<IScreenPresenter>> typeToPendingScreen;


        private SignalBus    signalBus;
        private RootUICanvas rootUICanvas;
        private ILogService  logService;
        private IGameAssets  gameAssets;

        #endregion

        [Inject]
        public void Init(SignalBus signalBusParam, ILogService logServiceParam, IGameAssets gameAssetsParam)
        {
            this.signalBus  = signalBusParam;
            this.logService = logServiceParam;
            this.gameAssets = gameAssetsParam;

            this.activeScreens               = new List<IScreenPresenter>();
            this.typeToLoadedScreenPresenter = new Dictionary<Type, IScreenPresenter>();
            this.typeToPendingScreen         = new Dictionary<Type, Task<IScreenPresenter>>();

            this.signalBus.Subscribe<StartLoadingNewSceneSignal>(this.CleanUpAllScreen);
            this.signalBus.Subscribe<ScreenShowSignal>(this.OnShowScreen);
            this.signalBus.Subscribe<ScreenCloseSignal>(this.OnCloseScreen);
            this.signalBus.Subscribe<ManualInitScreenSignal>(this.OnManualInitScreen);
            this.signalBus.Subscribe<ScreenSelfDestroyedSignal>(this.OnDestroyScreen);
            this.signalBus.Subscribe<PopupBlurBgShowedSignal>(this.OnPopupBlurBgShowed);
        }

        public void Dispose()
        {
            this.signalBus.Unsubscribe<StartLoadingNewSceneSignal>(this.CleanUpAllScreen);
            this.signalBus.Unsubscribe<ScreenShowSignal>(this.OnShowScreen);
            this.signalBus.Unsubscribe<ScreenCloseSignal>(this.OnCloseScreen);
            this.signalBus.Unsubscribe<ManualInitScreenSignal>(this.OnManualInitScreen);
            this.signalBus.Unsubscribe<ScreenSelfDestroyedSignal>(this.OnDestroyScreen);
            this.signalBus.Unsubscribe<PopupBlurBgShowedSignal>(this.OnPopupBlurBgShowed);
        }

        #region Implement IScreenManager

        public async UniTask<T> OpenScreen<T>() where T : IScreenPresenter
        {
            var nextScreen = await this.GetScreen<T>();
            if (nextScreen != null)
            {
                await nextScreen.OpenViewAsync();
                return nextScreen;
            }
            else
            {
                Debug.LogError($"The {typeof(T).Name} screen does not exist");
                // Need to implement lazy initialization by Load from resource
                return default;
            }
        }
        public async UniTask<TPresenter> OpenScreen<TPresenter, TModel>(TModel model) where TPresenter : IScreenPresenter<TModel>
        {
            var nextScreen = (await this.GetScreen<TPresenter>());
            if (nextScreen != null)
            {
                await nextScreen.OpenView(model);
                return nextScreen;
            }
            else
            {
                Debug.LogError($"The {typeof(TPresenter).Name} screen does not exist");
                // Need to implement lazy initialization by Load from resource
                return default;
            }
        }

        public async UniTask<T> GetScreen<T>() where T : IScreenPresenter
        {
            var screenType = typeof(T);
            if (this.typeToLoadedScreenPresenter.TryGetValue(screenType, out var screenPresenter)) return (T)screenPresenter;

            if (!this.typeToPendingScreen.TryGetValue(screenType, out var loadingTask))
            {
                loadingTask = InstantiateScreen();
                this.typeToPendingScreen.Add(screenType, loadingTask);
            }

            var result = await loadingTask;
            this.typeToPendingScreen.Remove(screenType);
            return (T)result;

            async Task<IScreenPresenter> InstantiateScreen()
            {
                screenPresenter = this.Instantiator.Instantiate<T>();
                var screenInfo = screenPresenter.GetCustomAttribute<ScreenInfoAttribute>();
                var viewObject = Instantiate(await this.gameAssets.LoadAssetAsync<GameObject>(screenInfo.AddressableScreenPath), this.CurrentRootScreen).GetComponent<IScreenView>();
                screenPresenter.SetView(viewObject);
                this.typeToLoadedScreenPresenter.Add(screenType, screenPresenter);
                return (T)screenPresenter;
            }
        }

        public async Task CloseCurrentScreen()
        {
            if (this.activeScreens.Count > 0)
                await this.activeScreens.Last().CloseViewAsync();
        }

        public void CloseAllScreen()
        {
            var cacheActiveScreens = this.activeScreens.ToList();
            this.activeScreens.Clear();
            foreach (var screen in cacheActiveScreens)
            {
                screen.CloseViewAsync();
            }

            this.currentActiveScreen  = null;
            this.previousActiveScreen = null;
        }

        public void CleanUpAllScreen()
        {
            this.activeScreens.Clear();
            this.currentActiveScreen  = null;
            this.previousActiveScreen = null;
            foreach (var screen in this.typeToLoadedScreenPresenter)
            {
                screen.Value.Dispose();
            }

            this.typeToLoadedScreenPresenter.Clear();
        }
        public Transform        CurrentRootScreen { get; set; }
        public Transform        CurrentHiddenRoot { get; set; }
        public IInstantiator    Instantiator      { get; set; }
        public IScreenPresenter CurrentScreen     => this.currentActiveScreen;

        #endregion

        #region Handle events

        private void OnShowScreen(ScreenShowSignal signal)
        {
            this.previousActiveScreen = this.currentActiveScreen;
            this.currentActiveScreen  = signal.ScreenPresenter;
            this.currentActiveScreen.SetViewParent(this.CurrentRootScreen);

            // if show the screen that already in the active screens list, remove current one in list and add it to the last of list
            if (this.activeScreens.Contains(signal.ScreenPresenter))
                this.activeScreens.Remove(signal.ScreenPresenter);
            this.activeScreens.Add(signal.ScreenPresenter);

            if (this.previousActiveScreen != null && this.previousActiveScreen != this.currentActiveScreen)
            {
                if (this.currentActiveScreen.IsClosePrevious)
                {
                    this.previousActiveScreen.CloseViewAsync();
                    this.previousActiveScreen = null;
                }
                else
                {
                    this.previousActiveScreen.OnOverlap();
                    //With the current screen is popup, the previous screen will be hide after the blur background is shown
                    if (!this.currentActiveScreen.GetType().IsSubclassOfRawGeneric(typeof(BasePopupPresenter<>)))
                        this.previousActiveScreen.HideView();
                }
            }
        }

        private void OnCloseScreen(ScreenCloseSignal signal)
        {
            var closeScreenPresenter = signal.ScreenPresenter;
            if (this.activeScreens.LastOrDefault() == closeScreenPresenter)
            {
                // If close the screen on the top, will be open again the behind screen if available
                this.currentActiveScreen = null;
                this.activeScreens.Remove(closeScreenPresenter);
                if (this.activeScreens.Count > 0)
                {
                    var nextScreen = this.activeScreens.Last();
                    if (nextScreen.ScreenStatus == ScreenStatus.Opened)
                        this.OnShowScreen(new ScreenShowSignal() { ScreenPresenter = nextScreen });
                    else
                        nextScreen.OpenViewAsync();
                }
            }
            else
            {
                this.activeScreens.Remove(closeScreenPresenter);
            }

            closeScreenPresenter?.SetViewParent(this.CurrentHiddenRoot);
        }

        private void OnManualInitScreen(ManualInitScreenSignal signal)
        {
            var screenPresenter = signal.ScreenPresenter;
            var screenType      = screenPresenter.GetType();
            if (this.typeToLoadedScreenPresenter.ContainsKey(screenType)) return;
            this.typeToLoadedScreenPresenter.Add(screenType, screenPresenter);
            var screenInfo = screenPresenter.GetCustomAttribute<ScreenInfoAttribute>();

            var viewObj = this.CurrentRootScreen.Find(screenInfo.AddressableScreenPath);
            if (viewObj != null)
            {
                screenPresenter.SetView(viewObj.GetComponent<IScreenView>());
            }
            else
                this.logService.Error($"The {screenInfo.AddressableScreenPath} object may be not instantiated in the RootUICanvas!!!");
        }

        private void OnDestroyScreen(ScreenSelfDestroyedSignal signal)
        {
            var screenPresenter = signal.ScreenPresenter;
            var screenType      = screenPresenter.GetType();

            if (this.previousActiveScreen != null && this.previousActiveScreen.Equals(screenPresenter)) this.previousActiveScreen = null;
            this.typeToLoadedScreenPresenter.Remove(screenType);
            this.activeScreens.Remove(screenPresenter);
        }

        private void OnPopupBlurBgShowed()
        {
            if (this.previousActiveScreen != null && this.previousActiveScreen.ScreenStatus != ScreenStatus.Hide)
            {
                this.previousActiveScreen.HideView();
            }
        }

        #endregion

        #region Monobehaviour

        private void Update()
        {
            // back button flow
            if (!Input.GetKeyDown(KeyCode.Escape)) return;

            if (this.activeScreens.Count > 1)
            {
                Debug.Log("Close last screen");
                this.activeScreens.Last().CloseViewAsync();
            }
            else
            {
                Debug.Log("Show popup confirm quit app");
                _ = this.OpenScreen<NotificationPopupPresenter, NotificationPopupModel>(new NotificationPopupModel()
                {
                    content        = "Do you really want to quit?",
                    title          = "Are you sure?",
                    type           = NotificationType.Option,
                    OkNoticeAction = this.QuitApplication,
                });
            }
        }

        private void QuitApplication()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
                             Application.Quit();
#endif
        }

        #endregion
    }
}