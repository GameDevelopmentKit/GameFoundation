using System;
using UnityEngine;
using Com.ForbiddenByte.OSA.Core;

#if OSA_TV_TMPRO
using TText = TMPro.TextMeshProUGUI;
#else
using TText = UnityEngine.UI.Text;
#endif

namespace Com.ForbiddenByte.OSA.CustomAdapters.TableView.Input
{
    /// <summary>
    /// Wrapper around a Text or a TMPro.TextMeshProUGUI.
    /// It expects a Text component attached. 
    /// Or its TMPro counterpart if OSA_TV_TMPRO is defined
    /// </summary>
    public class TableViewText : MonoBehaviour
    {
        [Tooltip(
            "Only needed if you want to display something like an ellipsis when the text shown isn't the entire text. " + "If you're using TMPro, you won't need this, as TMPro already has an ellipsis adding mechanism built-in")]
        [SerializeField]
        private Transform _ObjectToActivateOnOverflow = null;

        /// <summary>
        /// Keeping the same property name as the Unity's Text component
        /// </summary>
        public string text { get => this._Text.text; set => this._Text.text = value; }

        /// <summary>
        /// Keeping the same property name as the Unity's Text component
        /// </summary>
        public bool supportRichText
        {
            #if OSA_TV_TMPRO
			get { return _Text.richText; }
			set { _Text.richText = value; }
            #else
            get { return this._Text.supportRichText; }
            set { this._Text.supportRichText = value; }
            #endif
        }

        /// <summary>
        /// Keeping the same property name as the Unity's Text component
        /// </summary>
        public int fontSize
        {
            #if OSA_TV_TMPRO
			get { return (int)(_Text.fontSize + .5f); /*rounding to nearest int*/ }
            #else
            get { return this._Text.fontSize; }
            #endif
            set { this._Text.fontSize = value; }
        }

        /// <summary>
        /// Keeping the same property name as the Unity's Text component
        /// </summary>
        public Color color { get => this._Text.color; set => this._Text.color = value; }

        public RectTransform RT
        {
            get
            {
                if (!this._RetrievedRT)
                {
                    this._RetrievedRT = true;
                    this._RT          = this.transform as RectTransform;
                }

                return this._RT;
            }
        }

        private bool          _RetrievedRT;
        private RectTransform _RT;

        private TText _Text;
        //float _SavedAlpha;

        private void OnEnable()
        {
            if (this._Text) this._Text.enabled = true;
        }

        private void Awake()
        {
            this._Text = this.GetComponent<TText>();
            if (!this._Text) throw new OSAException("TableViewText: no " + typeof(TText).Name + " component found (expecting it because OSA_TV_TMPRO scripting symbol is defined)");

            //			// The builtin Text will disappear sometimes if verticalOverflow is not set to VerticalWrapMode.Overflow
            //#if OSA_TV_TMPRO
            //			_Text.overflowMode = TMPro.TextOverflowModes.Ellipsis;
            //#else
            //			//_Text.verticalOverflow = VerticalWrapMode.Overflow;
            //			if (_ObjectToActivateOnOverflow)
            //				SetOverflowActive(false);
            //#endif
            if (this._ObjectToActivateOnOverflow) this.SetOverflowActive(false);
        }

        // Manually add an Ellipsis if the text overflows, when using the built-in Text component
        private void Update()
        {
            // Update at larger intervals, for better performance
            if (Time.frameCount % 10 == 0) this.CheckOverflow();
        }

        private void CheckOverflow()
        {
            if (!this._Text || !this._ObjectToActivateOnOverflow) return;

            var active = false;
            #if OSA_TV_TMPRO
			active = _Text.isTextOverflowing;
            #else
            var textGen                 = this._Text.cachedTextGenerator;
            if (textGen != null) active = textGen.characterCountVisible != this._Text.text.Length;
            #endif
            this.SetOverflowActive(active);
        }

        private void SetOverflowActive(bool overFlowActive)
        {
            //float scaleToSet = overFlowActive ? 1f : 0f;
            //if (_ObjectToActivateOnOverflow.localScale.x == scaleToSet)
            //	return;

            //var l = _ObjectToActivateOnOverflow.localScale;
            //l.x = scaleToSet;
            //_ObjectToActivateOnOverflow.localScale = l;

            this._ObjectToActivateOnOverflow.gameObject.SetActive(overFlowActive);
        }

        private void OnDisable()
        {
            if (this._Text) this._Text.enabled = false;
        }

        //public void SetEnabledByScalingGameObject(bool enabled)
        //{
        //	transform.localScale = enabled ? Vector3.one : Vector3.zero;
        //}

        /// <summary>Returns the previous alpha</summary>
        public float SetAlpha(float alpha)
        {
            float prevAlpha;
            if (this._Text)
            {
                var c = this._Text.color;
                prevAlpha        = c.a;
                c.a              = alpha;
                this._Text.color = c;
            }
            else
            {
                prevAlpha = 0f;
            }

            return prevAlpha;
        }
    }
}