namespace GameFoundation.Scripts.UIModule.Utilities.UIStuff
{
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.EventSystems;
    using UnityEngine.UI;

    [RequireComponent(typeof(Button))]
    [RequireComponent(typeof(DisallowMultipleComponent))]
    public class MechButton : BaseMechSFX, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private List<GameObject> defaultParticles, appearParticles, hoverParticles, pressParticles;
        private                  bool             isActiveHover;
        private                  Button           btn;
        public                   Button           Btn => this.btn;

        private void Awake()
        {
            this.sfxName       = "btn_team_menu_select";
            this.btn           = this.GetComponent<Button>();
            this.isActiveHover = false;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            this.OnPlaySfx();
            if (this.pressParticles.Count <= 0) return;
            foreach (var pressParticle in this.pressParticles) pressParticle.SetActive(true);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!this.isActiveHover) return;
            if (!this.GetComponent<Button>().interactable) return;
            if (this.hoverParticles.Count <= 0) return;
            foreach (var hoverParticle in this.hoverParticles) hoverParticle.SetActive(true);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (this.hoverParticles.Count <= 0) return;
            foreach (var hoverParticle in this.hoverParticles) hoverParticle.SetActive(false);
        }

        public void SetDefaultParticleActive(bool isActive)
        {
            this.SetActiveHover(isActive);

            if (this.defaultParticles.Count <= 0) return;
            foreach (var defaultParticle in this.defaultParticles) defaultParticle.SetActive(isActive);
        }

        public void EnableAppearParticle()
        {
            if (this.appearParticles.Count <= 0) return;
            foreach (var appearParticle in this.appearParticles) appearParticle.SetActive(true);
        }

        public void SetDisableAllFx()
        {
            foreach (var appearParticle in this.appearParticles) appearParticle.SetActive(false);

            this.SetDefaultParticleActive(false);

            foreach (var pressParticle in this.pressParticles) pressParticle.SetActive(false);
        }

        public void SetActiveHover(bool pIsActiveHover)
        {
            this.isActiveHover = pIsActiveHover;
        }
    }
}