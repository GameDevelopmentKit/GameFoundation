using UnityEngine.EventSystems;

namespace GameFoundation.Scripts.UIModule.Utilities.UIStuff
{
    public class MechPointerEvent : BaseMechSFX, IPointerEnterHandler
    {
        public void OnPointerEnter(PointerEventData eventData)
        {
            this.OnPlaySfx();
        }
    }
}