using System;
using UnityEngine;
using UnityEngine.UI;
using frame8.Logic.Misc.Other.Extensions;
using Com.ForbiddenByte.OSA.Core;

namespace Com.ForbiddenByte.OSA.CustomAdapters.TableView.Tuple.Basic
{
    /// <summary>
    /// A views holder for a value inside a row from a database, which casts the value passed to it to a 
    /// string or Texture and binds it to a Text or RawImage component, respectively
    /// </summary>
    public class BasicTupleValueViewsHolder : TupleValueViewsHolder
    {
        private RectTransform _ImagePanel;
        private RawImage      _Image;
        private RectTransform _TextPanel;
        private RectTransform _TogglePanel;
        private Toggle        _Toggle;
        private bool          _ForwardValueChanges = true;

        private RectTransform _InputAvailableDot;
        //bool _IsCurrentlyReadonly;

        //LayoutElement _ImagePanelLE;
        //LayoutElement _TextPanelLE;
        //LayoutElement _TogglePanelLE;

        public override void CollectViews()
        {
            base.CollectViews();

            this.root.GetComponentAtPath("ImagePanel", out this._ImagePanel);
            this._ImagePanel.GetComponentAtPath("Image", out this._Image);

            this.root.GetComponentAtPath("TextPanel", out this._TextPanel);

            //root.GetComponentAtPath("TogglePanel", out _TogglePanel);
            //_TogglePanel.GetComponentAtPath("Toggle", out _Toggle);

            this.root.GetComponentAtPath("Toggle", out this._TogglePanel);
            this._Toggle = this._TogglePanel.GetComponent<Toggle>();

            this.root.GetComponentAtPath("InputAvailableDot", out this._InputAvailableDot);

            this._Toggle.onValueChanged.AddListener(this.OnToggleValueChanged);

            //_ImagePanelLE = _ImagePanel.GetComponent<LayoutElement>();
            //if (!_ImagePanelLE)
            //	_ImagePanelLE = _ImagePanel.gameObject.AddComponent<LayoutElement>();
            //_ImagePanelLE.ignoreLayout = true;

            //_TextPanelLE = _TextPanel.GetComponent<LayoutElement>();
            //if (!_TextPanelLE)
            //	_TextPanelLE = _TextPanel.gameObject.AddComponent<LayoutElement>();
            //_TextPanelLE.ignoreLayout = true;

            //_TogglePanelLE = _TogglePanel.GetComponent<LayoutElement>();
            //if (!_TogglePanelLE)
            //	_TogglePanelLE = _TogglePanel.gameObject.AddComponent<LayoutElement>();
            //_TogglePanelLE.ignoreLayout = true;
        }

        public override void UpdateViews(object value, ITableColumns columnsProvider)
        {
            var isNull = value == null;
            var column = columnsProvider.GetColumnState(this.ItemIndex);
            if (isNull)
            {
                this.UpdateAsNullText(column);
                return;
            }

            this.UpdateViews(value, column);
        }

        private void UpdateAsNullText(IColumnState column)
        {
            this.UpdateAsText("<color=#88444499>NULL</color>", false);
            this.SetInputAvailable(!column.CurrentlyReadOnly && this.IsStringInputType(column.Info.ValueType));
        }

        private void UpdateIntOrLong(string asStr, bool canChangeValue)
        {
            var updatedSuccessfully = this.UpdateAsText(asStr, canChangeValue);
            this.SetInputAvailable(canChangeValue && updatedSuccessfully);
        }

        private void UpdateFloatOrDouble(string asStr, bool canChangeValue)
        {
            var updatedSuccessfully = this.UpdateAsText(asStr, canChangeValue);
            this.SetInputAvailable(canChangeValue && updatedSuccessfully);
        }

        /// <summary>
        /// Expecting value to be non-null
        /// </summary>
        private void UpdateViews(object value, IColumnState column)
        {
            bool? textInputAvailable = null;
            try
            {
                var  canChangeValue = !column.CurrentlyReadOnly;
                bool updatedSuccessfully;
                switch (column.Info.ValueType)
                {
                    case TableValueType.RAW:
                        this.UpdateAsText("<color=#22552266>" + value.GetType().Name + "</color> " + value.ToString(), false);
                        textInputAvailable = false;
                        break;

                    case TableValueType.STRING:
                        updatedSuccessfully = this.UpdateAsText((string)value, canChangeValue);
                        textInputAvailable  = canChangeValue && updatedSuccessfully;
                        break;

                    case TableValueType.INT:
                        this.UpdateIntOrLong(((int)value).ToString(), canChangeValue);
                        break;

                    case TableValueType.LONG_INT:
                        this.UpdateIntOrLong(((long)value).ToString(), canChangeValue);
                        break;

                    case TableValueType.FLOAT:
                        var fl = (float)value;
                        this.UpdateFloatOrDouble(fl.ToString(OSAConst.FLOAT_TO_STRING_CONVERSION_SPECIFIER_PRESERVE_PRECISION), canChangeValue);
                        break;

                    case TableValueType.DOUBLE:
                        var db = (double)value;
                        //string text = val.ToString();
                        // Spent like 2 hours to find out C# double doesn't always convert successfully to string by default without losing precision, smh.
                        // https://docs.microsoft.com/en-us/dotnet/standard/base-types/standard-numeric-format-strings#RFormatString
                        // Furthermore, they have a useless R specifier that is more annoying because it doesn't always work. Probably there for historical reasons.
                        // The odd "G17" should be used for making sure a string that doesn't lose precision is output
                        this.UpdateFloatOrDouble(db.ToString(OSAConst.DOUBLE_TO_STRING_CONVERSION_SPECIFIER_PRESERVE_PRECISION), canChangeValue);
                        break;

                    case TableValueType.ENUMERATION:
                        string textToSet = null;
                        if (column.Info.EnumValueType != null && column.Info.EnumValueType.IsEnum)
                            try
                            {
                                textToSet = Enum.GetName(column.Info.EnumValueType, value);
                            }
                            catch
                            {
                            }
                        var validEnum             = !string.IsNullOrEmpty(textToSet);
                        if (!validEnum) textToSet = value.ToString();

                        updatedSuccessfully = this.UpdateAsText(textToSet, false /*enum text is changed by other means, not direct text editing*/);
                        textInputAvailable  = false;
                        break;

                    case TableValueType.BOOL:
                        updatedSuccessfully = this.UpdateAsCheckbox((bool)value, canChangeValue);
                        break;

                    case TableValueType.TEXTURE:
                        this.UpdateAsImage((Texture)value);
                        break;
                }

                if (textInputAvailable != null) this.SetInputAvailable(textInputAvailable.Value);
            }
            catch (Exception e)
            {
                Debug.LogError("Exception pre-details: " + column.Info.ValueType + ", value " + (value == null ? "NULL" : value));
                throw e;
            }
        }

        protected void UpdateAsImage(Texture texture)
        {
            if (this.ActivatePanelOnlyFor(this._Image)) this._Image.texture = texture;
            this.SetInputAvailable(false);
        }

        protected bool UpdateAsText(string text, bool editable)
        {
            if (this.TextComponent) this.TextComponent.supportRichText = !editable;

            if (this.ActivatePanelOnlyFor(this.TextComponent))
            {
                // Don't forward changes that are done from the model
                this._ForwardValueChanges             = false;
                this.HasPendingTransversalSizeChanges = this.TextComponent.text != text;
                if (this.HasPendingTransversalSizeChanges) this.TextComponent.text = text;
                this._ForwardValueChanges = true;
                return true;
            }

            return false;
        }

        protected bool UpdateAsCheckbox(bool value, bool editable)
        {
            if (this._Toggle) this._Toggle.interactable = editable;

            this.SetInputAvailable(false);

            if (this.ActivatePanelOnlyFor(this._Toggle))
            {
                // Don't forward changes that are done from the model
                this._ForwardValueChanges = false;
                this._Toggle.isOn         = value;
                this._ForwardValueChanges = true;

                return true;
            }

            return false;
        }

        private bool ActivatePanelOnlyFor(MonoBehaviour uiElement)
        {
            var result = false;
            if (this._Image)
            {
                var act = this._Image == uiElement;
                this._ImagePanel.gameObject.SetActive(act);

                result = result || act;
            }
            if (this.TextComponent)
            {
                var act = this.TextComponent == uiElement;
                this._TextPanel.gameObject.SetActive(act);

                result = result || act;
            }
            if (this._Toggle)
            {
                var act = this._Toggle == uiElement;
                this._TogglePanel.gameObject.SetActive(act);

                result = result || act;
            }

            return result;
        }

        private void SetInputAvailable(bool available)
        {
            //_InputAvailableDot.localScale = available ? Vector3.one : Vector3.zero;
            this._InputAvailableDot.gameObject.SetActive(available);
        }

        private void OnToggleValueChanged(bool newValue)
        {
            if (this._ForwardValueChanges && this._TogglePanel.gameObject.activeSelf) // just a sanity check
                this.NotifyValueChangedFromInput(newValue);
        }

        private bool IsStringInputType(TableValueType type)
        {
            return type == TableValueType.STRING
                || type == TableValueType.INT
                || type == TableValueType.LONG_INT
                || type == TableValueType.FLOAT
                || type == TableValueType.DOUBLE;
        }
    }
}