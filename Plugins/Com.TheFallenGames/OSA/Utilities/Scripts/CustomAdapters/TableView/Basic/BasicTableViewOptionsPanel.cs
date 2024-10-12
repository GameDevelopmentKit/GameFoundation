using System;
using UnityEngine;
using UnityEngine.UI;

namespace Com.ForbiddenByte.OSA.CustomAdapters.TableView.Basic
{
    public class BasicTableViewOptionsPanel : MonoBehaviour, ITableViewOptionsPanel
    {
        [SerializeField] private Button _EnterClearingStateButton = null;

        [SerializeField] private Button _ExitClearingStateButton = null;

        [SerializeField] private GameObject _ClearingStateShownObject = null;

        [SerializeField] private GameObject _NoneStateShownObject = null;

        [SerializeField] private GameObject _LoadingGameObject = null;

        [SerializeField] private CanvasGroup _CanvasGroupToDisableOnLoad = null;

        public bool IsClearing { get => this._IsClearing; set => this.SetIsClearing(value); }
        public bool IsLoading  { get => this._IsLoading;  set => this.SetIsLoading(value); }

        private bool _IsClearing;
        private bool _IsLoading;

        private void Start()
        {
            if (this._EnterClearingStateButton) this._EnterClearingStateButton.onClick.AddListener(this.SetClearing);
            if (this._ExitClearingStateButton) this._ExitClearingStateButton.onClick.AddListener(this.SetNoClearing);

            this.IsClearing = false;
            this.IsLoading  = false;
        }

        private void Update()
        {
            if (this._IsLoading)
                if (this._LoadingGameObject)
                    this._LoadingGameObject.transform.Rotate(Vector3.forward, -270f * Time.deltaTime, Space.Self);
        }

        private void SetNoClearing()
        {
            this.IsClearing = false;
        }

        private void SetClearing()
        {
            this.IsClearing = true;
        }

        private void SetIsClearing(bool isClearing)
        {
            if (this._EnterClearingStateButton) this._EnterClearingStateButton.gameObject.SetActive(!isClearing);
            if (this._ExitClearingStateButton) this._ExitClearingStateButton.gameObject.SetActive(isClearing);
            if (this._ClearingStateShownObject) this._ClearingStateShownObject.gameObject.SetActive(isClearing);
            if (this._NoneStateShownObject) this._NoneStateShownObject.gameObject.SetActive(!isClearing);

            this._IsClearing = isClearing;
        }

        private void SetIsLoading(bool isLoading)
        {
            this._IsLoading = isLoading;
            if (this._LoadingGameObject) this._LoadingGameObject.SetActive(this._IsLoading);

            if (this._CanvasGroupToDisableOnLoad) this._CanvasGroupToDisableOnLoad.blocksRaycasts = !this._IsLoading;
        }
    }
}