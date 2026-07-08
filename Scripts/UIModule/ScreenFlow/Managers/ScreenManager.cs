namespace GameFoundation.Scripts.UIModule.ScreenFlow.Managers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Cysharp.Threading.Tasks;
    using GameFoundation.Scripts.AssetLibrary;
    using GameFoundation.Scripts.UIModule.CommonScreen;
    using GameFoundation.Scripts.UIModule.ScreenFlow.BaseScreen.Presenter;
    using GameFoundation.Scripts.UIModule.ScreenFlow.BaseScreen.View;
    using GameFoundation.Scripts.UIModule.ScreenFlow.Signals;
    using GameFoundation.Scripts.Utilities.Extension;
    using GameFoundation.Scripts.Utilities.LogService;
    using Sirenix.OdinInspector;
    using UniRx;
    using UnityEditor;
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
        /// Get instance of a screen
        /// </summary>
        /// <param name="screenType"></param>
        /// <returns></returns>
        public UniTask<IScreenPresenter> GetScreen(Type screenType);

        /// <summary>
        /// Open a screen by type
        /// </summary>
        /// <typeparam name="T">Type of screen presenter</typeparam>
        public UniTask<T> OpenScreen<T>() where T : IScreenPresenter;

        public UniTask<IScreenPresenter> OpenScreen(Type screenType);

        public UniTask<TPresenter> OpenScreen<TPresenter, TModel>(TModel model) where TPresenter : IScreenPresenter<TModel>;
        public UniTask<TPresenter> OpenScreen<TPresenter, TModel>(TModel model, string customPath) where TPresenter : IScreenPresenter<TModel>;

        /// <summary>
        ///  Open a screen by type with model
        /// </summary>
        /// <param name="screenType"></param>
        /// <param name="model"></param>
        /// <typeparam name="TModel"></typeparam>
        /// <returns></returns>
        public UniTask<IScreenPresenter<TModel>> OpenScreen<TModel>(Type screenType, TModel model);
        public UniTask<IScreenPresenter<TModel>> OpenScreen<TModel>(Type screenType, TModel model, string customPath);

        /// <summary>
        /// Close a screen on top
        /// </summary>
        public UniTask CloseCurrentScreen();

        /// <summary>
        /// Close all screen on current scene
        /// </summary>
        public void CloseAllScreen();

        /// <summary>
        /// Close all screen on current scene async
        /// </summary>
        public UniTask CloseAllScreenAsync();

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
        /// Get overlay transform
        /// </summary>
        public Transform CurrentOverlayRoot { get; set; }

        /// <summary>
        /// Get root canvas of all screen, use to disable UI for creative purpose
        /// </summary>
        public RootUICanvas RootUICanvas { get; set; }

        /// <summary>
        /// Current screen shown on top.
        /// </summary>
        public ReactiveProperty<IScreenPresenter> CurrentActiveScreen { get; }
    }

    public class ScreenManager : MonoBehaviour, IScreenManager, IDisposable
    {
        #region Properties

        /// <summary>
        /// List of active screens
        /// </summary>
        [ShowInInspector] private List<IScreenPresenter> activeScreens;

        /// <summary>
        /// Current screen shown on top.
        /// </summary>
        [ShowInInspector] public ReactiveProperty<IScreenPresenter> CurrentActiveScreen { get; private set; } = new ReactiveProperty<IScreenPresenter>();

        private IScreenPresenter previousActiveScreen;

        private Dictionary<Type, ScreenInfo>       typeToLoadedScreenPresenter;
        private Dictionary<string, Task<IScreenView>> typeToPendingScreen;

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
            this.typeToLoadedScreenPresenter = new Dictionary<Type, ScreenInfo>();
            this.typeToPendingScreen         = new Dictionary<string, Task<IScreenView>>();

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

        public Transform    CurrentRootScreen  { get; set; }
        public Transform    CurrentHiddenRoot  { get; set; }
        public Transform    CurrentOverlayRoot { get; set; }
        public RootUICanvas RootUICanvas       { get; set; }

        public async UniTask<T> OpenScreen<T>() where T : IScreenPresenter { return (T)await this.OpenScreen(typeof(T)); }

        public async UniTask<IScreenPresenter> OpenScreen(Type screenType)
        {
            var screenInfo = this.GetScreenInfo(screenType);
            await this.SetObjectView(screenInfo);
            var nextScreen = screenInfo.Presenter;
            if (nextScreen != null)
            {
                try
                {
                    await nextScreen.OpenViewAsync();
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }

                return nextScreen;
            }
            else
            {
                Debug.LogError($"The {screenType.Name} screen does not exist");

                return default;
            }
        }

        public async UniTask<TPresenter> OpenScreen<TPresenter, TModel>(TModel model) where TPresenter : IScreenPresenter<TModel>
        {
            return (TPresenter)await this.OpenScreen(typeof(TPresenter), model);
        }

        public async UniTask<TPresenter> OpenScreen<TPresenter, TModel>(TModel model, string customPath) where TPresenter : IScreenPresenter<TModel>
        {
            return (TPresenter)await this.OpenScreen(typeof(TPresenter), model, customPath);
        }

        public async UniTask<IScreenPresenter<TModel>> OpenScreen<TModel>(Type screenType, TModel model)
        {
            var screenInfo = this.GetScreenInfo(screenType);
            await this.SetObjectView(screenInfo);
            var nextScreen = (IScreenPresenter<TModel>)screenInfo.Presenter;

            if (nextScreen != null)
            {
                nextScreen.SetViewParent(this.CheckPopupIsOverlay(nextScreen) ? this.CurrentOverlayRoot : this.CurrentRootScreen);

                try
                {
                    await nextScreen.OpenViewAsync(model);
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }

                return nextScreen;
            }
            else
            {
                Debug.LogError($"The {screenType.Name} screen does not exist");

                return default;
            }
        }
        
        public async UniTask<IScreenPresenter<TModel>> OpenScreen<TModel>(Type screenType, TModel model, string customPath)
        {
            var screenInfo = this.GetScreenInfo(screenType);
            if (screenInfo.Presenter is ICustomPresenter customPresenter) customPresenter.CustomAddressPath = customPath;
            await this.SetObjectView(screenInfo);
            var nextScreen = (IScreenPresenter<TModel>)screenInfo.Presenter;

            if (nextScreen != null)
            {
                nextScreen.SetViewParent(this.CheckPopupIsOverlay(nextScreen) ? this.CurrentOverlayRoot : this.CurrentRootScreen);

                try
                {
                    await nextScreen.OpenViewAsync(model);
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }

                return nextScreen;
            }
            else
            {
                Debug.LogError($"The {screenType.Name} screen does not exist");

                return default;
            }
        }
        
        public async UniTask<IScreenPresenter> GetScreen(Type screenType)
        {
            var screenInfo = this.GetScreenInfo(screenType);
            await this.SetObjectView(screenInfo);
            return screenInfo.Presenter;
        }
        

        public async UniTask<T> GetScreen<T>() where T : IScreenPresenter
        {
            return (T)await GetScreen(typeof(T));
        }

        public async UniTask<IScreenView> SetObjectView(ScreenInfo screenInfo)
        {
            var addressPath = screenInfo.Attribute.AddressableScreenPath;
            if (screenInfo.Presenter is ICustomPresenter customScreen) addressPath = customScreen.CustomAddressPath;
            
            if (screenInfo.AddressPathToScreenView.TryGetValue(addressPath, out var viewObject) && !viewObject.Equals(null))
            {
                if (screenInfo.CurrentScreenViewPath != addressPath)
                {
                    screenInfo.Presenter.SetView(viewObject);
                    screenInfo.CurrentScreenViewPath = addressPath;
                }
                return viewObject;
            }

            if (!this.typeToPendingScreen.TryGetValue(addressPath, out var loadingTask))
            {
                loadingTask = InstantiateScreen(addressPath);
                this.typeToPendingScreen.Add(addressPath, loadingTask);
            }

            var result = await loadingTask;
            this.typeToPendingScreen.Remove(addressPath);

            return result;

            async Task<IScreenView> InstantiateScreen(string address)
            {
                var viewObject = Instantiate(await this.gameAssets.LoadAssetAsync<GameObject>(address),
                    this.CheckPopupIsOverlay(screenInfo.Presenter) ? this.CurrentOverlayRoot : this.CurrentRootScreen).GetComponent<IScreenView>();

                screenInfo.AddressPathToScreenView[address] = viewObject;
                screenInfo.Presenter.SetView(viewObject);
                screenInfo.CurrentScreenViewPath = address;
                return viewObject;
            }
        }

        public ScreenInfo GetScreenInfo(Type screenType)
        {
            //check screen type is implemented IScreenPresenter
            if (!typeof(IScreenPresenter).IsAssignableFrom(screenType))
            {
                throw new ArgumentException($"The provided type {screenType.Name} does not implement IScreenPresenter.");
            }

            if (this.typeToLoadedScreenPresenter.TryGetValue(screenType, out var screenInfo))
            {
                return screenInfo;
            }

            var presenter = this.GetCurrentContainer().Instantiate(screenType) as IScreenPresenter;
            screenInfo = new ScreenInfo()
            {
                Presenter = presenter,
                AddressPathToScreenView = new Dictionary<string, IScreenView>(),
                Attribute = presenter.GetCustomAttribute<ScreenInfoAttribute>()
            };
            this.typeToLoadedScreenPresenter.Add(screenType, screenInfo);
            return screenInfo;
        }
        
        public async UniTask CloseCurrentScreen()
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

            this.CurrentActiveScreen.Value = null;
            this.previousActiveScreen      = null;
        }

        public void CloseAllScreen(params Type[] except)
        {
            var cacheActiveScreens = this.activeScreens.ToList();

            foreach (var screen in cacheActiveScreens)
            {
                if (!except.Contains(screen.GetType()))
                {
                    screen.CloseViewAsync();
                    this.activeScreens.Remove(screen);
                }
            }

            if (activeScreens.Count > 0)
            {
                this.CurrentActiveScreen.Value = this.activeScreens.Last();
                this.previousActiveScreen      = this.activeScreens.Count > 1 ? this.activeScreens[^2] : null;
            }
            else
            {
                this.CurrentActiveScreen.Value = null;
                this.previousActiveScreen      = null;
            }
        }

        public async UniTask CloseAllScreenAsync()
        {
            var tasks              = new List<UniTask>();
            var cacheActiveScreens = this.activeScreens.ToList();
            this.activeScreens.Clear();

            foreach (var screen in cacheActiveScreens)
            {
                if (screen.ScreenStatus == ScreenStatus.Opened)
                {
                    tasks.Add(screen.CloseViewAsync());
                }
            }

            this.CurrentActiveScreen.Value = null;
            this.previousActiveScreen      = null;

            await UniTask.WhenAll(tasks);
        }

        public void CleanUpAllScreen()
        {
            this.activeScreens.Clear();
            this.CurrentActiveScreen.Value = null;
            this.previousActiveScreen      = null;

            foreach (var screen in this.typeToLoadedScreenPresenter)
            {
                if (screen.Value.Presenter.ScreenStatus != ScreenStatus.Opened) continue;
                screen.Value.Presenter.Dispose();
            }

            this.typeToLoadedScreenPresenter.Clear();
        }

        #endregion

        #region Handle events

        #region Check Overlay Popup

        private bool CheckScreenIsPopup(IScreenPresenter screenPresenter) { return screenPresenter.GetType().IsSubclassOfRawGeneric(typeof(BasePopupPresenter<>)); }

        private bool CheckPopupIsOverlay(IScreenPresenter screenPresenter) { return this.CheckScreenIsPopup(screenPresenter) && screenPresenter.GetCustomAttribute<PopupInfoAttribute>().IsOverlay; }

        #endregion

        private void OnShowScreen(ScreenShowSignal signal)
        {
            this.previousActiveScreen      = this.CurrentActiveScreen.Value;
            this.CurrentActiveScreen.Value = signal.ScreenPresenter;

            this.CurrentActiveScreen.Value.SetViewParent(this.CheckPopupIsOverlay(this.CurrentActiveScreen.Value) ? this.CurrentOverlayRoot : this.CurrentRootScreen);

            // if show the screen that already in the active screens list, remove current one in list and add it to the last of list
            if (this.activeScreens.Contains(signal.ScreenPresenter))
                this.activeScreens.Remove(signal.ScreenPresenter);

            this.activeScreens.Add(signal.ScreenPresenter);

            if (this.previousActiveScreen != null && this.previousActiveScreen != this.CurrentActiveScreen.Value)
            {
                if (this.CurrentActiveScreen.Value.IsClosePrevious)
                {
                    this.previousActiveScreen.CloseViewAsync();
                    this.previousActiveScreen = null;
                }
                else
                {
                    this.previousActiveScreen.OnOverlap();

                    //With the current screen is popup, the previous screen will be hide after the blur background is shown
                    if (!this.CheckScreenIsPopup(this.CurrentActiveScreen.Value))
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
                this.CurrentActiveScreen.Value = null;
                this.activeScreens.Remove(closeScreenPresenter);

                if (this.activeScreens.Count > 0)
                {
                    var nextScreen = this.activeScreens.Last();

                    if (nextScreen.ScreenStatus == ScreenStatus.Opened)
                    {
                        this.OnShowScreen(new ScreenShowSignal() { ScreenPresenter = nextScreen });
                        if (this.CheckScreenIsPopup(nextScreen)) signalBus.Fire(new PopupShowedSignal() { ScreenPresenter = nextScreen });
                    }
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
            this.typeToLoadedScreenPresenter.Add(screenType, new ScreenInfo()
            {
                Presenter = screenPresenter,
                AddressPathToScreenView = new Dictionary<string, IScreenView>()
            });
            var screenInfo = screenPresenter.GetCustomAttribute<ScreenInfoAttribute>();

            var viewObj = this.CurrentRootScreen.Find(screenInfo.AddressableScreenPath);

            if (viewObj != null)
            {
                screenPresenter.SetView(viewObj.GetComponent<IScreenView>());

                if (signal.IncludingBindData)
                {
                    screenPresenter.BindData();
                }
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

        //todo should refactor this back button flow to a dedicated class
        // private void Update()
        // {
        //     // back button flow
        //     if (!Input.GetKeyDown(KeyCode.Escape)) return;
        //
        //     if (this.activeScreens.Count > 1)
        //     {
        //         Debug.Log("Close last screen");
        //         this.activeScreens.Last().CloseViewAsync();
        //     }
        //     else
        //     {
        //         Debug.Log("Show popup confirm quit app");
        //
        //         _ = this.OpenScreen<NotificationPopupPresenter, NotificationPopupModel>(new NotificationPopupModel()
        //         {
        //             Content        = "Do you really want to quit?",
        //             Title          = "Are you sure?",
        //             Type           = NotificationType.Option,
        //             OkNoticeAction = this.QuitApplication,
        //         });
        //     }
        // }

        private void QuitApplication()
        {
#if UNITY_EDITOR
            EditorApplication.isPlaying = false;
#else
                             Application.Quit();
#endif
        }

        #endregion
    }
    
    public interface ICustomPresenter
    {
        public string CustomAddressPath { get; set; }
    }

    public class ScreenInfo
    {
        public IScreenPresenter Presenter;
        public string CurrentScreenViewPath;
        public ScreenInfoAttribute Attribute;
        public Dictionary<string, IScreenView> AddressPathToScreenView;
    }
}
