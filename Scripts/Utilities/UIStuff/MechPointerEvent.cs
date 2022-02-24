
using GameFoundation.Scripts.Utilities;
using UnityEngine;
using UnityEngine.EventSystems;

public class MechPointerEvent : BaseMechSFX,IPointerEnterHandler
{
    public void OnPointerEnter(PointerEventData eventData)
    {
        this.OnPlaySfx();
    }
}
