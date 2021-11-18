using UnityEngine;

namespace DarkTonic.MasterAudio.Examples
{
	public class MA_EnemySpawner : MonoBehaviour
	{
		public GameObject Enemy;
		public bool spawnerEnabled = false;

		private Transform trans;
		private float nextSpawnTime;

		void Awake()
		{
			this.useGUILayout = false;
			this.trans = this.transform;
			this.nextSpawnTime = AudioUtil.Time + Random.Range(.3f, .7f);
		}

		// Update is called once per frame
		void Update()
		{
			if (!spawnerEnabled)
			{
				return;
			}

			if (Time.time >= this.nextSpawnTime)
			{
				var spawnPos = this.trans.position;

				var numToSpawn = Random.Range(1, 3);

				for (var i = 0; i < numToSpawn; i++)
				{
					spawnPos.x = Random.Range(spawnPos.x - 6, spawnPos.x + 6);
					Instantiate(Enemy, spawnPos, Enemy.transform.rotation);
				}

				this.nextSpawnTime = AudioUtil.Time + Random.Range(.3f, .7f);
			}
		}
	}
}