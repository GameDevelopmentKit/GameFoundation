using UnityEngine.EventSystems;

namespace GameFoundation.Scripts.UIModule.Utilities.UIStuff
{
    public class PointerEvent : BaseSFX,IPointerEnterHandler
    {
        public void OnPointerEnter(PointerEventData eventData)
        {
            this.OnPlaySfx();
        }
    }
}
