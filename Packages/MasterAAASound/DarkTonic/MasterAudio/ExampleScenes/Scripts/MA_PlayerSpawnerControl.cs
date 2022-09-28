using UnityEngine;

namespace DarkTonic.MasterAudio.Examples
{
	public class MA_PlayerSpawnerControl : MonoBehaviour
	{
		public GameObject Player;

		private float nextSpawnTime;

		void Awake()
		{
			this.useGUILayout = false;
			this.nextSpawnTime = -1f;
		}

		private bool PlayerActive {
			get {
				return Player.activeInHierarchy;
			}
		}

		// Update is called once per frame
		void Update()
		{
			if (!PlayerActive)
			{
				if (nextSpawnTime < 0)
				{
					nextSpawnTime = AudioUtil.Time + 1;
				}

				if (Time.time >= this.nextSpawnTime)
				{
					Player.SetActive(true);

					nextSpawnTime = -1;
				}
			}
		}
	}
}