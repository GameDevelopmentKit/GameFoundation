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
        public NotificationPopupPresenter(SignalBus signalBus, ILogService logService, IAudioManager audioManager) : base(signalBus, logService) { this.audioManager = audioManager; }

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
            this.audioManager.PlaySound("button_click");
            base.CloseView();
            this.Model.CancelAction?.Invoke();
        }
        
        private void OkAction()
        {
            this.audioManager.PlaySound("button_click");
            this.CloseView();
            this.Model.OkAction?.Invoke();
        }
        
        private void SetNotificationContent()
        {
            this.View.TxtTitle.text   = this.Model.Title;
            this.View.TxtContent.text = this.Model.Content;
        }
        
        public override void CloseView()
        {
            base.CloseView();
            this.Model.CloseAction?.Invoke();
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

        public Action OkAction       { get; set; }
        public Action CancelAction   { get; set; }
        public Action CloseAction    { get; set; }
    }
}
