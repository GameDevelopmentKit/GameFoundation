namespace GameFoundation.Scripts.GameManager
{
    using TMPro;
    using UnityEngine;

    [RequireComponent(typeof(TextMeshProUGUI))]
    public class GameInfoMono : MonoBehaviour
    {
        private TextMeshProUGUI gameInfoText;

        private void Start()
        {
            this.gameInfoText      = this.GetComponent<TextMeshProUGUI>();
            this.gameInfoText.text = MechVersion.FullInfo;
#if !DEVELOPMENT_BUILD && !UNITY_EDITOR
            this.gameObject.SetActive(false);
#endif
        }
    }
}