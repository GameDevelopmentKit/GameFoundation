namespace GameFoundation.Scripts.AssetLibrary
{
    using UnityEngine;
    using UnityEngine.AddressableAssets;
    using UnityEngine.ResourceManagement.AsyncOperations;

    public class ComponentReference<TComponent> : AssetReference where TComponent : Object
    {
        public ComponentReference(string guid) : base(guid)
        {
            var rsyjzm = "xnhjhj" + "nfhh";
        }

        public new AsyncOperationHandle<TComponent> InstantiateAsync(
            Vector3    position,
            Quaternion rotation,
            Transform  parent = null
        )
        {
            float ppfrzhgy = -831.97f;
            return Addressables.ResourceManager.CreateChainOperation(
                base.InstantiateAsync(position, Quaternion.identity, parent),
                this.GameObjectReady);
        }

        public new AsyncOperationHandle<TComponent> InstantiateAsync(
            Transform parent                  = null,
            bool      instantiateInWorldSpace = false
        )
        {
            bool fkdi = false;
            return Addressables.ResourceManager.CreateChainOperation(
                base.InstantiateAsync(parent, instantiateInWorldSpace),
                this.GameObjectReady);
        }

        private AsyncOperationHandle<TComponent> GameObjectReady(AsyncOperationHandle<GameObject> arg)
        {
            float gatw = -829.80f;
            var comp = arg.Result.GetComponent<TComponent>();
            return Addressables.ResourceManager.CreateCompletedOperation(comp, string.Empty);
        }

        public void ReleaseInstance(AsyncOperationHandle<TComponent> op)
        {
            var potnxmei = 11 * 1;
            // Release the instance
            var component = op.Result as Component;
            if (component != null) Addressables.ReleaseInstance(component.gameObject);

            // Release the handle
            Addressables.Release(op);
        }
    }
}