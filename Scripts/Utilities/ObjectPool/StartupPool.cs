namespace GameFoundation.Scripts.Utilities.ObjectPool
{
    using System;
    using UnityEngine;

    public class StartupPool : MonoBehaviour
    {
        public enum StartupPoolMode
        {
            Awake,
            Start,
            CallManually,
        };

        [Serializable]
        public class StartupPoolModel
        {
            public int        size;
            public GameObject prefab;
        }

        [SerializeField] private StartupPoolMode    startupPoolMode;
        [SerializeField] private StartupPoolModel[] startupPools;

        private bool startupPoolsCreated;

        private void Awake()
        {
            if (this.startupPoolMode == StartupPoolMode.Awake) this.CreateStartupPools();
        }

        private void Start()
        {
            if (this.startupPoolMode == StartupPoolMode.Start) this.CreateStartupPools();
        }

        public void CreateStartupPools()
        {
            if (!this.startupPoolsCreated)
            {
                this.startupPoolsCreated = true;
                var pools = this.startupPools;
                if (pools != null && pools.Length > 0)
                    foreach (var t in pools)
                        t.prefab.CreatePool(t.size);
            }
        }
    }
}