using UnityEngine;

namespace DarkTonic.MasterAudio.Examples
{
	// ReSharper disable once CheckNamespace
	// ReSharper disable once InconsistentNaming
	public class MA_EnemyOne : MonoBehaviour
	{
		public GameObject ExplosionParticlePrefab;
		private Transform _trans;
		private float _speed;
		private float _horizSpeed;

		// ReSharper disable once UnusedMember.Local
		void Awake()
		{
			useGUILayout = false;
			_trans = transform;
			_speed = Random.Range(-3, -8) * AudioUtil.FrameTime;
			_horizSpeed = Random.Range(-3, 3) * AudioUtil.FrameTime;

#if !PHY3D_ENABLED
			Debug.LogError("MA_EnemyOne and this example Scene will not work properly without Physics3D package installed. Please enable it in the Master Audio Welcome Window if it's already installed.");
#endif
		}

#if PHY3D_ENABLED
    // ReSharper disable once UnusedMember.Local
    // ReSharper disable once UnusedParameter.Local
    void OnCollisionEnter(Collision collision) {
		Instantiate(ExplosionParticlePrefab, _trans.position, Quaternion.identity);
	}
#endif

		// Update is called once per frame
		// ReSharper disable once UnusedMember.Local
		void Update()
		{
			var pos = _trans.position;
			pos.x += _horizSpeed;
			pos.y += _speed;
			_trans.position = pos;

			_trans.Rotate(Vector3.down * 300 * AudioUtil.FrameTime);

			if (_trans.position.y < -5)
			{
				//this.gameObject.SetActiveRecursively(false);
				Destroy(gameObject);
			}
		}
	}
}