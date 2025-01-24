using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using Com.ForbiddenByte.OSA.Core;

namespace Com.ForbiddenByte.OSA.Demos.Common
{
	public class OSATitle : MonoBehaviour
	{
		protected void Start() { GetComponent<Text>().text = "Optimized ScrollView Adapter v" + OSAConst.OSA_VERSION_STRING; }
	}
}