namespace DataManager.UserData
{
    using System;

    public interface IInitializeDataOnStart
    {
        internal Type GetDataType();
        public   void InitializeData(IUserData userData);
    }
}