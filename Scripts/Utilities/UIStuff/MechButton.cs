namespace GameFoundation.Scripts.Utilities.Extension
{
    using UnityEngine;
    using UnityEngine.EventSystems;

    
    public class MechButton : BaseMechSFX,IPointerClickHandler
    {
        public void OnPointerClick(PointerEventData eventData)
        {
            this.OnPlaySfx();
        }
    }
}