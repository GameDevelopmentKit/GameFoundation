namespace GameFoundation.Scripts.UIModule.CommonScreen
{
    using System;
    using Cysharp.Threading.Tasks;
    using GameFoundation.Scripts.UIModule.ScreenFlow.BaseScreen.Presenter;
    using GameFoundation.Scripts.UIModule.ScreenFlow.BaseScreen.View;
    using GameFoundation.Scripts.Utilities;
    using GameFoundation.Scripts.Utilities.LogService;
    using GameFoundation.Signals;
    using TMPro;
    using UnityEngine;
    using UnityEngine.Scripting;
    using UnityEngine.UI;

    public enum NotificationType
    {
        Close,
        Option,
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

    [PopupInfo("UIPopupNotice", true, false, true)]
    public class NotificationPopupPresenter : BasePopupPresenter<NotificationPopupUIView, NotificationPopupModel>
    {
        private readonly IAudioService audioManager;

        [Preserve]
        public NotificationPopupPresenter(SignalBus signalBus, ILogService logService, IAudioService audioManager) : base(signalBus, logService)
        {
            this.audioManager = audioManager;
        }

        public override UniTask BindData(NotificationPopupModel popupPopupModel)
        {
            this.Init();
            this.SetNotificationContent();
            this.SwitchMode();
            return UniTask.CompletedTask;
        }

        private void Init()
        {
            this.View.BtnOk.onClick.AddListener(this.OkAction);
            this.View.BtnOkNotice.onClick.AddListener(this.OkNoticeAction);
            this.View.BtnCancel.onClick.AddListener(this.CloseView);
        }

        private void SwitchMode()
        {
            this.View.NoticeObj.SetActive(this.Model.Type == NotificationType.Option);
            this.View.CloseObj.SetActive(this.Model.Type == NotificationType.Close);
        }

        public override void CloseView()
        {
            this.audioManager.PlaySound("button_click");
            base.CloseView();
            this.Model.CloseAction?.Invoke();
            this.Model.CancelAction?.Invoke();
        }

        private void OkAction()
        {
            this.audioManager.PlaySound("button_click");
            this.CloseView();
            this.Model.OkAction?.Invoke();
        }

        private void OkNoticeAction()
        {
            this.audioManager.PlaySound("button_click");
            this.CloseView();
            this.Model.OkNoticeAction?.Invoke();
        }

        private void SetNotificationContent()
        {
            this.View.TxtTitle.text   = this.Model.Title;
            this.View.TxtContent.text = this.Model.Content;
        }

        public override void Dispose()
        {
            base.Dispose();
            this.View.BtnOk.onClick.RemoveListener(this.OkAction);
            this.View.BtnCancel.onClick.RemoveListener(this.CloseView);
        }
    }

    public class NotificationPopupModel
    {
        public string           Title;
        public string           Content;
        public NotificationType Type;

        public Action OkAction       { get; set; }
        public Action OkNoticeAction { get; set; }
        public Action CancelAction   { get; set; }
        public Action CloseAction    { get; set; }
    }
}