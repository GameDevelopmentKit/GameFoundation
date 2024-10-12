namespace GameFoundation.Scripts.AssetLibrary
{
    using UnityEngine;
    using UnityEngine.AddressableAssets;
    using UnityEngine.ResourceManagement.AsyncOperations;

    public class ComponentReference<TComponent> : AssetReference where TComponent : Object
    {
        public ComponentReference(string guid) : base(guid)
        {
        }

        public new AsyncOperationHandle<TComponent> InstantiateAsync(
            Vector3    position,
            Quaternion rotation,
            Transform  parent = null
        )
        {
            return Addressables.ResourceManager.CreateChainOperation(
                base.InstantiateAsync(position, Quaternion.identity, parent),
                this.GameObjectReady);
        }

        public new AsyncOperationHandle<TComponent> InstantiateAsync(
            Transform parent                  = null,
            bool      instantiateInWorldSpace = false
        )
        {
            return Addressables.ResourceManager.CreateChainOperation(
                base.InstantiateAsync(parent, instantiateInWorldSpace),
                this.GameObjectReady);
        }

        private AsyncOperationHandle<TComponent> GameObjectReady(AsyncOperationHandle<GameObject> arg)
        {
            var comp = arg.Result.GetComponent<TComponent>();
            return Addressables.ResourceManager.CreateCompletedOperation(comp, string.Empty);
        }

        public void ReleaseInstance(AsyncOperationHandle<TComponent> op)
        {
            // Release the instance
            var component = op.Result as Component;
            if (component != null) Addressables.ReleaseInstance(component.gameObject);

            // Release the handle
            Addressables.Release(op);
        }
    }
}