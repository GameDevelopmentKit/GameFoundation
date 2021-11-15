namespace Mech.Utils
{
    using Mech.Services;
    using TMPro;
    using UnityEngine;
    using Zenject;

    [DisallowMultipleComponent]
    public class MechTextMeshPro : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI     txtText;
        [Inject]         private ILogService         logger;
        [Inject]         private LocalizationService localizationService;
        private void Awake()
        {
            this.txtText      = this.GetComponent<TextMeshProUGUI>();
            this.txtText.text = this.localizationService.GetTextWithKey(this.txtText.text);
        }
    }
}