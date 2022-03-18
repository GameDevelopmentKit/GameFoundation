namespace GameFoundation.Scripts.Utilities.Extension
{
    using UnityEngine;
    using UnityEngine.EventSystems;
    using UnityEngine.UI;

    public class MechButton : BaseMechSFX, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private GameObject defaultParticle, appearParticle, hoverParticle, pressParticle;
        private                  bool       isActiveHover;

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
    }
}