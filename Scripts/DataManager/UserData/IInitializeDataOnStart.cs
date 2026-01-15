namespace DataManager.UserData
{
    using System;

    public interface IInitializeDataOnStart : IDataManagerLifecycle
    {
        internal Type GetDataType();
        public   void InitializeData(IUserData userData);
    }

}