namespace GameFoundation.Scripts.Utilities.Extension
{
    using UnityEngine;
    using UnityEngine.UI;

    public class MechButton : MonoBehaviour
    {
        [SerializeField] private Button btn;
        private                  void   Awake() { this.btn = this.GetComponent<Button>(); }
    }
}