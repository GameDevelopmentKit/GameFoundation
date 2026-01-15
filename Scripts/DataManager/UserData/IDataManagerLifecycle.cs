namespace DataManager.UserData
{
    public interface IDataManagerLifecycle
    {
        void StartInitialize() { }
        
        void OnDataInitialized() { }
    }
}
