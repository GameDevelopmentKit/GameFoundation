namespace GameFoundation.Scripts.CommonScreen
{
    using System;
    using GameFoundation.Scripts.ScreenFlow.BaseScreen.Presenter;
    using GameFoundation.Scripts.ScreenFlow.BaseScreen.View;
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
        [SerializeField] private Button          btnCancel;
        [SerializeField] private Button          btnClose;
        public                   TextMeshProUGUI TxtTitle   => this.txtTitle;
        public                   TextMeshProUGUI TxtContent => this.txtContent;
        public                   Button          BtnOk      => this.btnOk;
        public                   Button          BtnCancel  => this.btnCancel;
        public                   Button          BtnClose   => this.btnClose;
    }

    [PopupInfo("NotificationPopupUIView", isEnableBlur: true, isCloseWhenTapOutside: false)]
    public class NotificationPopupPresenter : BasePopupPresenter<NotificationPopupUIView, NotificationPopupModel>
    {
        private readonly IMechSoundManager mechSoundManager;
        public NotificationPopupPresenter(SignalBus signalBus, ILogService logService, IMechSoundManager mechSoundManager) : base(signalBus, logService) { this.mechSoundManager = mechSoundManager; }

        public override void BindData(NotificationPopupModel popupPopupModel)
        {
            this.Init();
            this.SetNotificationContent();
            this.SwitchMode();
        }

        private void Init()
        {
            this.View.BtnOk.onClick.AddListener(this.OkAction);
            this.View.BtnCancel.onClick.AddListener(this.CloseView);
            this.View.BtnClose.onClick.AddListener(this.CloseView);
        }

        private void SwitchMode()
        {
            this.View.BtnClose.gameObject.SetActive(this.Model.type == NotificationType.Close);
            this.View.BtnOk.gameObject.SetActive(this.Model.type != NotificationType.Close);
            this.View.BtnCancel.gameObject.SetActive(this.Model.type != NotificationType.Close);
        }

        public override void CloseView()
        {
            this.mechSoundManager.PlaySound("button_click");
            base.CloseView();
            this.Model.CloseAction?.Invoke();
            this.Model.CancelAction?.Invoke();
        }

        private void OkAction()
        {
            this.mechSoundManager.PlaySound("button_click");
            this.CloseView();
            this.Model.OkAction?.Invoke();
        }

        private void SetNotificationContent()
        {
            this.View.TxtTitle.text   = this.Model.title;
            this.View.TxtContent.text = this.Model.content;
        }

        public override void Dispose()
        {
            base.Dispose();
            this.View.BtnOk.onClick.RemoveListener(this.OkAction);
            this.View.BtnCancel.onClick.RemoveListener(this.CloseView);
            this.View.BtnClose.onClick.RemoveListener(this.CloseView);
        }
    }

    public class NotificationPopupModel
    {
        public string           title;
        public string           content;
        public NotificationType type;

        public Action OkAction     { get; set; }
        public Action CancelAction { get; set; }
        public Action CloseAction  { get; set; }
    }
}