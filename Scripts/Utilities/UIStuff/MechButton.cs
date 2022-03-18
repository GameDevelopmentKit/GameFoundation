namespace GameFoundation.Scripts.Utilities.UIStuff
{
    using System;
    using UnityEngine;
    using UnityEngine.Events;
    using UnityEngine.EventSystems;
    using UnityEngine.UI;

    [RequireComponent(typeof(Button))]
    [RequireComponent(typeof(DisallowMultipleComponent))]
    public class MechButton : BaseMechSFX, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler, IDisposable
    {
        [SerializeField] private GameObject defaultParticle, appearParticle, hoverParticle, pressParticle;
        private                  bool       isActiveHover;
        private                  Button     btn;
        public                   Button     Btn => this.btn;

        private void Awake()
        {
            this.btn = this.GetComponent<Button>();
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            this.OnPlaySfx();
            
            if (this.pressParticle == null) return;
            this.pressParticle.SetActive(true);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!this.isActiveHover) return;
            if (!this.GetComponent<Button>().interactable) return;
            if (this.hoverParticle == null) return;
            this.hoverParticle.SetActive(true);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (this.hoverParticle != null) 
                this.hoverParticle.SetActive(false);
        }

        public void SetDefaultParticleActive(bool isActive)
        {
            this.isActiveHover = isActive;
            if (this.defaultParticle == null) return;
            this.defaultParticle.SetActive(isActive);
        }
        
        public void EnableAppearParticle()
        {
            if (this.appearParticle == null) return;
            this.appearParticle.SetActive(true);
        }
        
        public void Dispose()
        {
            
        }
    }
}