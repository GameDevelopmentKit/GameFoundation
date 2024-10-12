//#define OSA_TV_TMPRO

using System;
using UnityEngine;
using Com.ForbiddenByte.OSA.Core;
using frame8.Logic.Misc.Other.Extensions;
using UnityEngine.UI;

#if OSA_TV_TMPRO
using TInputField = TMPro.TMP_InputField;
#else
using TInputField = UnityEngine.UI.InputField;
#endif

namespace Com.ForbiddenByte.OSA.CustomAdapters.TableView.Input
{
    /// <summary>
    /// It expects an InputField in the direct children, and a Text. 
    /// Or their TMPro counterparts if OSA_TV_TMPRO is defined
    /// </summary>
    public class TableViewTextInputController : MonoBehaviour
    {
        private string text
        {
            get => this._InputField.text;
            set
            {
                this._InputField.text = value;
                this.UpdateSizeControllerText(this._InputField.text);
            }
        }

        /// <summary>
        /// Keeping the same property name as the Unity's Text component
        /// </summary>
        public int fontSize
        {
            get => this._Text.fontSize;
            set
            {
                this._Text.fontSize                     = value;
                this._InputField.textComponent.fontSize = value;
            }
        }

        private bool interactable { get => this._InputField.interactable; set => this._InputField.interactable = value; }
        //public TText textComponent { get { return _InputField.textComponent; } /*set { _InputField.textComponent = value; }*/ } 
        //public Image image { get { return _InputField.image; } /*set { _InputField.image = value; }*/ } 

        private bool MultiLine { get => this._InputField.multiLine; set => this._InputField.lineType = value ? TInputField.LineType.MultiLineNewline : TInputField.LineType.SingleLine; }

        //public bool CanAcceptInput { get { return isActiveAndEnabled && interactable && _InputField.isActiveAndEnabled; } }

        private RectTransform _RT;
        private TInputField   _InputField;
        private TableViewText _Text;

        private LayoutElement _TextLayoutElement;

        //LayoutElement _MyLayoutElement;
        private Action<string> _CurrentEndEditCallback;
        private Action         _CurrentCancelCallback;

        //void OnEnable()
        //{
        //	if (_InputField)
        //		_InputField.enabled = true;
        //	if (_Text)
        //		_Text.enabled = true;
        //}

        private void Awake()
        {
            this._RT       = this.transform as RectTransform;
            this._RT.pivot = new(0f, 1f); // top-left

            for (var i = 0; i < this.transform.childCount; i++)
            {
                var ch                                  = this.transform.GetChild(i);
                if (!this._InputField) this._InputField = ch.GetComponent<TInputField>();
                if (!this._Text) this._Text             = ch.GetComponent<TableViewText>();
            }

            if (!this._InputField) throw new OSAException("TableView: no " + typeof(TInputField).Name + " component found in direct children");

            if (!this._Text) throw new OSAException("TableView: no " + typeof(TableViewText).Name + " component field found in direct children");

            if (!this._InputField.textComponent) throw new OSAException("TableView: the " + typeof(TInputField).Name + " has no text component specified");

            var layEl         = this._InputField.GetComponent<LayoutElement>();
            if (!layEl) layEl = this._InputField.gameObject.AddComponent<LayoutElement>();

            layEl.ignoreLayout = true;
            var rt = layEl.transform as RectTransform;
            rt.MatchParentSize(true);

            this._TextLayoutElement = this._Text.GetComponent<LayoutElement>();
            if (!this._TextLayoutElement) this._TextLayoutElement                            = this._Text.gameObject.AddComponent<LayoutElement>();
            this._TextLayoutElement.preferredHeight = this._TextLayoutElement.preferredWidth = -1f;
            this._TextLayoutElement.flexibleHeight  = this._TextLayoutElement.flexibleWidth  = -1f;

            //layEl.flexibleHeight = _FlexibleHeight;
            //layEl.flexibleWidth = _FlexibleWidth;

            var group         = this.GetComponent<HorizontalLayoutGroup>();
            if (!group) group = this.gameObject.AddComponent<HorizontalLayoutGroup>();

            group.childForceExpandHeight = group.childForceExpandWidth = false;
            group.childControlHeight     = group.childControlWidth     = true;

            //_MyLayoutElement = GetComponent<LayoutElement>();
            //if (!_MyLayoutElement)
            //	_MyLayoutElement = gameObject.AddComponent<LayoutElement>();

            var csf       = this.GetComponent<ContentSizeFitter>();
            if (!csf) csf = this.gameObject.AddComponent<ContentSizeFitter>();
            csf.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            csf.verticalFit   = ContentSizeFitter.FitMode.PreferredSize;

            this._InputField.onValueChanged.AddListener(this.UpdateSizeControllerText);
            this._InputField.onEndEdit.AddListener(this.OnEndEdit);
        }

        //void OnDisable()
        //{
        //	if (_InputField)
        //		_InputField.enabled = false;
        //	if (_Text)
        //		_Text.enabled = false;
        //}

        private void OnDestroy()
        {
            if (this._InputField) this._InputField.onValueChanged.RemoveListener(this.UpdateSizeControllerText);
        }

        public void ShowFloating(RectTransform atParent, string initialText, bool multiLine, Action<string> onEndEdit, Action onCancel)
        {
            this._CurrentEndEditCallback = null;
            this._CurrentCancelCallback  = null;
            this.gameObject.SetActive(true);
            this._CurrentEndEditCallback = onEndEdit;
            this._CurrentCancelCallback  = onCancel;

            var parRect = atParent.rect;

            this._RT.position = atParent.position;

            this._TextLayoutElement.minWidth  = this._TextLayoutElement.preferredWidth  = parRect.width;
            this._TextLayoutElement.minHeight = this._TextLayoutElement.preferredHeight = parRect.height;
            //_RT.SetSizeFromParentEdgeWithCurrentAnchors(_RT.parent as RectTransform, RectTransform.Edge.Left, parRect.width);
            this._RT.TryClampPositionToParentBoundary();

            this.MultiLine = multiLine;

            this.ActivateInputField();

            this.text = initialText;
        }

        public void Hide()
        {
            this._CurrentEndEditCallback = null;

            var cancelCallback = this._CurrentCancelCallback;
            var callCancel     = false;
            this._CurrentCancelCallback = null;
            if (this._InputField && this._InputField.isActiveAndEnabled && this._InputField.isFocused)
            {
                this.DeactivateInputField();
                callCancel = true;
            }

            this.gameObject.SetActive(false);

            if (callCancel && cancelCallback != null) cancelCallback();
        }

        private void ActivateInputField()
        {
            this._InputField.ActivateInputField();
        }

        private void DeactivateInputField()
        {
            this._InputField.DeactivateInputField();
        }

        private void UpdateSizeControllerText(string _)
        {
            this._Text.text = this._InputField.text;
        }

        private void OnEndEdit(string text)
        {
            var c = this._CurrentEndEditCallback;

            this.Hide();

            if (c != null) c(text);
        }
    }
}