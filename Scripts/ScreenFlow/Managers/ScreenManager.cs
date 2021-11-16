namespace GameFoundation.Scripts.ScreenFlow.Managers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Cysharp.Threading.Tasks;
    using GameFoundation.Scripts.AssetLibrary;
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
        public void CloseCurrentScreen();

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

        private Dictionary<Type, IScreenPresenter> screenPool;


        private SignalBus    signalBus;
        private RootUICanvas rootUICanvas;
        private ILogService  logService;

        #endregion

        [Inject]
        public void Init(SignalBus signalBusParam, ILogService logServiceParam)
        {
            this.signalBus   = signalBusParam;
            this.logService  = logServiceParam;

            this.activeScreens = new List<IScreenPresenter>();
            this.screenPool    = new Dictionary<Type, IScreenPresenter>();

            this.signalBus.Subscribe<StartLoadingNewSceneSignal>(this.CleanUpAllScreen);
            this.signalBus.Subscribe<ScreenShowSignal>(this.OnShowScreen);
            this.signalBus.Subscribe<ScreenHideSignal>(this.OnCloseScreen);
            this.signalBus.Subscribe<ManualInitScreenSignal>(this.OnManualInitScreen);
            this.signalBus.Subscribe<ScreenSelfDestroyedSignal>(this.OnDestroyScreen);
            this.signalBus.Subscribe<PopupBlurBgShowedSignal>(this.OnPopupBlurBgShowed);
        }

        public void Dispose()
        {
            this.signalBus.Unsubscribe<StartLoadingNewSceneSignal>(this.CleanUpAllScreen);
            this.signalBus.Unsubscribe<ScreenShowSignal>(this.OnShowScreen);
            this.signalBus.Unsubscribe<ScreenHideSignal>(this.OnCloseScreen);
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
                nextScreen.OpenView();
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
                nextScreen.OpenView(model);
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
            if (this.screenPool.TryGetValue(screenType, out var screenController)) return (T)screenController;
            screenController = this.Instantiator.Instantiate<T>();
            await this.InstantiateView(screenController);
            this.screenPool.Add(screenType, screenController);
            return (T)screenController;
        }

        private async UniTask<IScreenView> InstantiateView(IScreenPresenter presenter)
        {
            var screenInfo = presenter.GetCustomAttribute<ScreenInfoAttribute>();
            var viewObject = Instantiate(await GameAssets.LoadAssetAsync<GameObject>(screenInfo.AddressableScreenPath), this.CurrentRootScreen).GetComponent<IScreenView>();
            presenter.SetView(viewObject);
            return viewObject;
        }

        public void CloseCurrentScreen()
        {
            if (this.activeScreens.Count > 0) this.activeScreens.Last().CloseView();
        }

        public void CloseAllScreen()
        {
            var cacheActiveScreens = this.activeScreens.ToList();
            this.activeScreens.Clear();
            foreach (var screen in cacheActiveScreens)
            {
                screen.CloseView();
            }

            this.currentActiveScreen  = null;
            this.previousActiveScreen = null;
        }

        public void CleanUpAllScreen()
        {
            this.activeScreens.Clear();
            this.currentActiveScreen  = null;
            this.previousActiveScreen = null;
            foreach (var screen in this.screenPool)
            {
                screen.Value.Dispose();
            }

            this.screenPool.Clear();
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
                    this.previousActiveScreen.CloseView();
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

        private void OnCloseScreen(ScreenHideSignal signal)
        {
            var closeScreenPresenter = signal.ScreenPresenter;
            if (this.activeScreens.LastOrDefault() == closeScreenPresenter)
            {
                // If close the screen on the top, will be open again the behind screen if available
                this.activeScreens.Remove(closeScreenPresenter);
                if (this.activeScreens.Count > 0)
                {
                    var nextScreen = this.activeScreens.Last();
                    if (nextScreen.ScreenStatus == ScreenStatus.Opened)
                        this.OnShowScreen(new ScreenShowSignal() { ScreenPresenter = nextScreen });
                    else
                        nextScreen.OpenView();
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
            if (this.screenPool.ContainsKey(screenType)) return;
            this.screenPool.Add(screenType, screenPresenter);
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
            this.screenPool.Remove(screenType);
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
    }
}