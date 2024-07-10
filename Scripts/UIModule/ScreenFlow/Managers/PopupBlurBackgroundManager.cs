namespace GameFoundation.Scripts.UIModule.ScreenFlow.Managers
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Reflection;
    using GameFoundation.Scripts.UIModule.ScreenFlow.BaseScreen.Presenter;
    using GameFoundation.Scripts.UIModule.ScreenFlow.Signals;
    using LeTai.Asset.TranslucentImage;
    using UnityEngine;
    using UnityEngine.UI;
    using Zenject;

    /// <summary>
    /// Manager the blur background display flow for popup
    /// </summary>
    public class PopupBlurBackgroundManager : MonoBehaviour
    {
        [SerializeField] private Image                  blurImage;
        [SerializeField] private Button                 btnClose;
        [SerializeField] private TranslucentImageSource translucentImageSource;

        private IScreenPresenter currentPopup;
        private Coroutine        showImageCoroutine;
        private SignalBus        signalBus;

        private readonly Dictionary<Type, PopupInfoAttribute> popupInfoPool = new();


        [Inject]
        private void Init(SignalBus signalBusParam)
        {
            this.signalBus = signalBusParam;
            this.signalBus.Subscribe<PopupShowedSignal>(this.OnPopupShowed);
            this.signalBus.Subscribe<PopupHiddenSignal>(this.OnPopupHidden);

            this.btnClose.onClick.AddListener(this.OnCloseButton);
        }

        private void OnDestroy()
        {
            this.signalBus.Unsubscribe<PopupShowedSignal>(this.OnPopupShowed);
            this.signalBus.Unsubscribe<PopupHiddenSignal>(this.OnPopupHidden);
        }

        private void OnCloseButton()
        {
            if (this.currentPopup != null && this.GetPopupInfo(this.currentPopup).IsCloseWhenTapOutside)
            {
                this.currentPopup.CloseView();
            }
        }

        private void OnPopupHidden(PopupHiddenSignal signal)
        {
            if (this.currentPopup != null && this.currentPopup.ScreenStatus != ScreenStatus.Opened)
            {
                
                if (this.showImageCoroutine != null)
                    this.StopCoroutine(this.showImageCoroutine);
                this.blurImage.enabled = false;
                this.blurImage.transform.SetParent(null,false);

                this.btnClose.gameObject.SetActive(false);
                this.currentPopup = null;
            }
        }

        private void OnPopupShowed(PopupShowedSignal signal)
        {
            this.currentPopup = signal.ScreenPresenter;
            var popupInfo = this.GetPopupInfo(this.currentPopup);
            if (popupInfo.IsEnableBlur)
            {
                this.showImageCoroutine = this.StartCoroutine(this.ShowImageInternal());
            }

            this.btnClose.gameObject.SetActive(popupInfo.IsCloseWhenTapOutside);
            this.blurImage.transform.SetParent(this.currentPopup.CurrentTransform,false);
            this.blurImage.rectTransform.SetAsFirstSibling();
        }

        private IEnumerator ShowImageInternal()
        {
            this.blurImage.enabled              = false;
            this.translucentImageSource.Request();
            this.translucentImageSource.enabled = true;
            // First, disable the blur image so that we can see the UI behind it.
            // Then enable TranslucentImageSource and wait until end of frame for the camera to render the UI and TranslucentImageSource can capture it.
            // Finally, disable TranslucentImageSource because we have what we want now and re-enable the blur image.
            yield return new WaitForEndOfFrame();

            this.translucentImageSource.enabled = false;
            this.blurImage.enabled              = true;
            this.signalBus.Fire<PopupBlurBgShowedSignal>();
        }

        private PopupInfoAttribute GetPopupInfo(IScreenPresenter popup)
        {
            var popupType = popup.GetType();
            if (!this.popupInfoPool.TryGetValue(popupType, out var result))
            {
                result = popupType.GetCustomAttribute<PopupInfoAttribute>();
                this.popupInfoPool.Add(popupType, result);
            }

            return result;
        }
    }
}