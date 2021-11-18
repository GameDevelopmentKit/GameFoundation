using UnityEngine;

namespace DarkTonic.MasterAudio.Examples
{
    // ReSharper disable once CheckNamespace
    // ReSharper disable once InconsistentNaming
    public class MA_Laser : MonoBehaviour
    {
        private Transform _trans;

        // ReSharper disable once UnusedMember.Local
        void Awake()
        {
            useGUILayout = false;
            _trans = transform;

#if !PHY3D_ENABLED
            Debug.LogError("MA_Laser and this example Scene will not work properly without Physics3D package installed. Please enable it in the Master Audio Welcome Window if it's already installed.");
#endif
        }

#if PHY3D_ENABLED
        // ReSharper disable once UnusedMember.Local
        void OnCollisionEnter(Collision collision) {
        if (!collision.gameObject.name.StartsWith("Enemy(")) {
            return;
        }

        Destroy(collision.gameObject);
        Destroy(gameObject);
    }
#endif

        // Update is called once per frame
        // ReSharper disable once UnusedMember.Local
        void Update()
        {
            var moveAmt = 10f * AudioUtil.FrameTime;

            var pos = _trans.position;
            pos.y += moveAmt;
            _trans.position = pos;

            if (_trans.position.y > 7)
            {
                Destroy(gameObject);
            }
        }
    }
}