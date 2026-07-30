namespace GameFoundation.Scripts.UIModule.CommonScreen
{
    using System;
    using Cysharp.Threading.Tasks;
    using GameFoundation.Scripts.UIModule.ScreenFlow.BaseScreen.Presenter;
    using GameFoundation.Scripts.UIModule.ScreenFlow.BaseScreen.View;
    using GameFoundation.Scripts.UIModule.Utilities;
    using GameFoundation.Scripts.Utilities;
    using GameFoundation.Scripts.Utilities.LogService;
    using TMPro;
    using UnityEngine;
    using UnityEngine.UI;
    using Zenject;

    public enum NotificationType
    {
        Close,
        Option,
        Blocking,
    }

    public class NotificationPopupUIView : BaseView
    {
        [SerializeField] private TextMeshProUGUI txtTitle;
        [SerializeField] private TextMeshProUGUI txtContent;
        [SerializeField] private Button          btnOk;
        [SerializeField] private Button          btnOkNotice;
        [SerializeField] private Button          btnCancel;
        [SerializeField] private GameObject      noticeObj;
        [SerializeField] private GameObject      closeObj;

        public TextMeshProUGUI TxtTitle    => this.txtTitle;
        public TextMeshProUGUI TxtContent  => this.txtContent;
        public Button          BtnOk       => this.btnOk;
        public Button          BtnOkNotice => this.btnOkNotice;
        public Button          BtnCancel   => this.btnCancel;
        public GameObject      NoticeObj   => this.noticeObj;
        public GameObject      CloseObj    => this.closeObj;
    }

    [PopupInfo("UIPopupNotice", isEnableBlur: false, isCloseWhenTapOutside: false, isOverlay: true)]
    public class NotificationPopupPresenter : BasePopupPresenter<NotificationPopupUIView, NotificationPopupModel>
    {
        private readonly IAudioManager audioManager;
        private string defaultOkButtonText;
        private string defaultOkNoticeButtonText;
        private string defaultCancelButtonText;
        private bool   allowBlockingClose;

        public bool IsBlocking => this.Model?.Type == NotificationType.Blocking;

        public NotificationPopupPresenter(SignalBus signalBus, ILogService logService, IAudioManager audioManager) : base(signalBus, logService) { this.audioManager = audioManager; }

        public override UniTask BindData(NotificationPopupModel popupPopupModel)
        {
            this.allowBlockingClose = false;
            this.Init();
            this.SetNotificationContent();
            this.SetButtonTexts();
            this.SwitchMode();

            return UniTask.CompletedTask;
        }

        private void Init()
        {
            this.CacheDefaultButtonTexts();
            this.View.BtnOk.onClick.RemoveListener(this.OkAction);
            this.View.BtnOkNotice.onClick.RemoveListener(this.OkAction);
            this.View.BtnCancel.onClick.RemoveListener(this.CancelAction);

            this.View.BtnOk.onClick.AddListener(this.OkAction);
            this.View.BtnOkNotice.onClick.AddListener(this.OkAction);
            this.View.BtnCancel.onClick.AddListener(this.CancelAction);
        }

        private void SwitchMode()
        {
            this.View.NoticeObj.SetActive(this.Model.Type == NotificationType.Option);
            this.View.CloseObj.SetActive(this.Model.Type == NotificationType.Close);
        }

        public void CancelAction()
        {
            if (this.IsBlocking) return;

            this.audioManager.PlaySound("button_click");
            base.CloseView();
            this.Model.CancelAction?.Invoke();
        }
        
        private void OkAction()
        {
            if (this.IsBlocking) return;

            this.audioManager.PlaySound("button_click");
            this.CloseView();
            this.Model.OkAction?.Invoke();
        }
        
        private void SetNotificationContent()
        {
            this.View.TxtTitle.text   = this.Model.Title;
            this.View.TxtContent.text = this.Model.Content;
        }

        private void SetButtonTexts()
        {
            this.SetButtonText(this.View.BtnOk, this.Model.OkButtonText, this.defaultOkButtonText);
            this.SetButtonText(this.View.BtnOkNotice, this.Model.OkButtonText, this.defaultOkNoticeButtonText);
            this.SetButtonText(this.View.BtnCancel, this.Model.CancelButtonText, this.defaultCancelButtonText);
        }

        private void CacheDefaultButtonTexts()
        {
            this.defaultOkButtonText ??= this.GetButtonText(this.View.BtnOk);
            this.defaultOkNoticeButtonText ??= this.GetButtonText(this.View.BtnOkNotice);
            this.defaultCancelButtonText ??= this.GetButtonText(this.View.BtnCancel);
        }

        private void SetButtonText(Button button, string text, string fallbackText)
        {
            if (button == null) return;
            var label = button.GetComponentInChildren<TextMeshProUGUI>(true);
            if (label == null) return;
            label.text = string.IsNullOrWhiteSpace(text) ? fallbackText : text;
        }

        private string GetButtonText(Button button)
        {
            if (button == null) return string.Empty;
            var label = button.GetComponentInChildren<TextMeshProUGUI>(true);
            return label == null ? string.Empty : label.text;
        }
        
        public override async UniTask CloseViewAsync()
        {
            if (this.IsBlocking && !this.allowBlockingClose) return;

            await base.CloseViewAsync();
        }

        public override void CloseView()
        {
            if (this.IsBlocking && !this.allowBlockingClose) return;

            base.CloseView();
            this.Model.CloseAction?.Invoke();
        }

        public async UniTask CompleteAndClose()
        {
            if (!this.IsBlocking) return;

            this.allowBlockingClose = true;

            try
            {
                await this.CloseViewAsync();
                this.Model.CloseAction?.Invoke();
            }
            finally
            {
                this.allowBlockingClose = false;
            }
        }

        public override void Dispose()
        {
            base.Dispose();
            this.View.BtnOk.onClick.RemoveListener(this.OkAction);
            this.View.BtnCancel.onClick.RemoveListener(this.CancelAction);
            this.View.BtnOkNotice.onClick.RemoveListener(this.OkAction);
        }
    }

    public class NotificationPopupModel
    {
        public string           Title;
        public string           Content;
        public NotificationType Type;
        public string           OkButtonText;
        public string           CancelButtonText;

        public Action OkAction       { get; set; }
        public Action CancelAction   { get; set; }
        public Action CloseAction    { get; set; }
    }
}
