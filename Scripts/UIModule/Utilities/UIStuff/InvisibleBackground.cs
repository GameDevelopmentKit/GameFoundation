namespace UIModule.Utilities.UIStuff
{
    using UnityEngine;
    using UnityEngine.Events;
    using UnityEngine.UI;

    public class InvisibleBackground : MonoBehaviour
    {
        private const string InvisibleBgName = "InvisibleBG";
        public void SetupInvisibleBg(Transform parent, UnityAction onClose)
        {
            GameObject invisibleBg = new(InvisibleBackground.InvisibleBgName);
            invisibleBg.SetActive(true);

            var tempImage = invisibleBg.AddComponent<Image>();
            tempImage.color = new(1f, 1f, 1f, 0f);

            var tempBtn = invisibleBg.AddComponent<Button>();
            tempBtn.onClick.RemoveAllListeners();
            tempBtn.onClick.AddListener(onClose);

            var tempTransform = invisibleBg.GetComponent<RectTransform>();
            tempTransform.anchorMin = new(0f, 0f);
            tempTransform.anchorMax = new(1f, 1f);
            tempTransform.offsetMin = new(0f, 0f);
            tempTransform.offsetMax = new(0f, 0f);
            invisibleBg.transform.SetParent(parent, false);
        }
    }
}