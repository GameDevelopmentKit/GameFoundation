#if MPUIKIT_EXISTS
using MPUIKIT;
#endif
using UnityEngine;

public class NeoCircleBar : MonoBehaviour
{
    [SerializeField, Range(0, 1)] float barValue;
#if MPUIKIT_EXISTS
    [SerializeField] MPImage imgCircle;
#endif
    [SerializeField] CanvasGroup warmLight;
    void FixedUpdate()
    {
#if MPUIKIT_EXISTS
        imgCircle.fillAmount = barValue;
#endif
        warmLight.alpha = 1 - (4f - barValue * 4);
    }
}
