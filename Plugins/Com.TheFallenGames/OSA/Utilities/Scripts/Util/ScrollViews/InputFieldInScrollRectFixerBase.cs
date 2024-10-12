using UnityEngine;
using UnityEngine.UI;
using System;
using UnityEngine.EventSystems;

namespace Com.ForbiddenByte.OSA.AdditionalComponents
{
    /// <summary>
    /// Utility that allows dragging a ScrollRect even if the PointerDown event has started inside a child InputField (which cancels the dragging by default)
    /// </summary>
    public abstract class InputFieldInScrollRectFixerBase : MonoBehaviour, IInitializePotentialDragHandler, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler, IPointerDownHandler, IPointerUpHandler, IScrollHandler
    {
        protected     Selectable _InputField;
        private       Image      _ImageOnMeIfChild;
        private const string     CHILD_NAME = "InputFieldFixer-Child";

        private bool _IAmChild;
        private bool _DragInProgress;

        protected virtual void Awake()
        {
            this._InputField = this.GetComponent<Selectable>();
            this._IAmChild   = this._InputField == null;
            if (this._IAmChild)
                this.InitAsChild();
            else
            {
                this.CacheMethods();
                this.InitAsParent();
            }
        }

        protected abstract void CacheMethods();

        private void OnDisable()
        {
            this._DragInProgress = false;
        }

        private void InitAsParent()
        {
            var inputFieldImg                              = this._InputField.image;
            if (inputFieldImg) inputFieldImg.raycastTarget = false;

            var           tr = this.transform.Find(CHILD_NAME);
            GameObject    go;
            RectTransform goRT;

            // The child may already exist if you'll instantiate an existing InputField with InputFieldInScrollRectFixer attached
            if (tr == null)
            {
                go   = new(CHILD_NAME, typeof(RectTransform));
                goRT = go.transform as RectTransform;
                goRT.SetParent(this._InputField.transform, false);
                go.AddComponent(this.GetType() /*add the same component as <this>'s type, i.e. the one for InputField or TMPro.TMP_InputField*/);
            }

            // Parent not needed anymore
            Destroy(this);
        }

        private void InitAsChild()
        {
            this.name        = CHILD_NAME;
            this._InputField = this.transform.parent.GetComponent<Selectable>();
            if (!this._InputField) throw new InvalidOperationException("Child InputFieldInScrollRectFixer: InputField not found in parent");

            this.CacheMethods();

            var inputFieldImg = this._InputField.image;
            if (!inputFieldImg) throw new InvalidOperationException("Child InputFieldInScrollRectFixer: InputField must have an image attached (can be invisible)");

            // May have already been created if this is an instance of a another runtime instance
            this._ImageOnMeIfChild = this.GetComponent<Image>();
            if (!this._ImageOnMeIfChild)
            {
                this._ImageOnMeIfChild        = this.gameObject.AddComponent<Image>();
                this._ImageOnMeIfChild.sprite = inputFieldImg.sprite;
            }

            var goRT = this.transform as RectTransform;

            goRT.SetAsLastSibling();
            goRT.anchorMin = Vector2.zero;
            goRT.anchorMax = Vector2.one;
            goRT.sizeDelta = Vector2.zero;

            this._ImageOnMeIfChild.color = Color.clear;
        }

        protected abstract void ActivateInputField();
        protected abstract bool IsInputFieldFocused();

        void IPointerDownHandler.OnPointerDown(PointerEventData eventData)
        {
            var par = this.GetInputFieldIfActiveOrNextComponentInItsParents<IPointerDownHandler>();
            if (par != null) par.OnPointerDown(eventData);
        }

        void IPointerUpHandler.OnPointerUp(PointerEventData eventData)
        {
            var par = this.GetInputFieldIfActiveOrNextComponentInItsParents<IPointerUpHandler>();
            if (par != null) par.OnPointerUp(eventData);
        }

        void IPointerClickHandler.OnPointerClick(PointerEventData eventData)
        {
            if (this.InputFieldActiveAndFocused())
            {
                (this._InputField as IPointerClickHandler).OnPointerClick(eventData);
                return;
            }

            if (this._DragInProgress) return;

            if (eventData.useDragThreshold)
            {
                var dragDist = Vector2.Distance(eventData.pressPosition, eventData.position);
                if (dragDist > EventSystem.current.pixelDragThreshold) return;
            }

            if (this.CanInputFieldBeFocused())
            {
                this.ActivateInputField();
                return;
            }

            var par = this.GetComponentInInputFieldParents<IPointerClickHandler>();
            if (par != null) par.OnPointerClick(eventData);
        }

        void IInitializePotentialDragHandler.OnInitializePotentialDrag(PointerEventData eventData)
        {
            if (!this.InputFieldActiveAndFocused())
            {
                var par = this.GetComponentInInputFieldParents<IInitializePotentialDragHandler>();
                if (par != null) par.OnInitializePotentialDrag(eventData);
            }
        }

        void IBeginDragHandler.OnBeginDrag(PointerEventData eventData)
        {
            this._DragInProgress = true;
            var par = this.GetInputFieldIfActiveOrNextComponentInItsParents<IBeginDragHandler>();
            if (par != null) par.OnBeginDrag(eventData);
        }

        void IDragHandler.OnDrag(PointerEventData eventData)
        {
            var par = this.GetInputFieldIfActiveOrNextComponentInItsParents<IDragHandler>();
            if (par != null) par.OnDrag(eventData);
        }

        void IScrollHandler.OnScroll(PointerEventData eventData)
        {
            var par = this.GetInputFieldIfActiveOrNextComponentInItsParents<IScrollHandler>();
            if (par != null) par.OnScroll(eventData);
        }

        void IEndDragHandler.OnEndDrag(PointerEventData eventData)
        {
            this._DragInProgress = false;
            var par = this.GetInputFieldIfActiveOrNextComponentInItsParents<IEndDragHandler>();
            if (par != null) par.OnEndDrag(eventData);
        }

        private bool InputFieldActiveAndFocused()
        {
            return this.CanInputFieldBeFocused() && this.IsInputFieldFocused();
        }

        private bool CanInputFieldBeFocused()
        {
            return this._InputField.isActiveAndEnabled && this._InputField.interactable;
        }

        private T GetInputFieldIfActiveOrNextComponentInItsParents<T>()
        {
            if (this.InputFieldActiveAndFocused()) return (T)(object)this._InputField;

            return this.GetComponentInInputFieldParents<T>();
        }

        private T GetComponentInInputFieldParents<T>()
        {
            return (T)(object)this._InputField.transform.parent.GetComponentInParent(typeof(T));
        }
    }
}