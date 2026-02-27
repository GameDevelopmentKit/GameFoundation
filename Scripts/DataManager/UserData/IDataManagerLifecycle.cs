namespace DataManager.UserData
{
    using System;
    public interface IDataManagerLifecycle : IDisposable
    {
        void StartInitialize() { }
        
        void OnDataInitialized() { }
    }
}
