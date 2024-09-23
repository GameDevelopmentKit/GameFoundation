namespace VContainer.Context
{
    using UnityEngine;

    public abstract class MonoInstaller : MonoBehaviour
    {
        public abstract void Install(IContainerBuilder builder);
    }
}