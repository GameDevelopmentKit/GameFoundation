using UnityEngine;
using UnityEngine.UI;
using Com.ForbiddenByte.OSA.CustomAdapters.DateTimePicker;

namespace Com.ForbiddenByte.OSA.Demos.DateTimePicker
{
	public class ShowDateTimePickerButton : MonoBehaviour
	{
		void Start()
        {
			var b = GetComponent<Button>();
			if (!b)
				b = gameObject.AddComponent<Button>();
			b.onClick.AddListener(() => DateTimePickerDialog.Show(null));
		}
	}
}
