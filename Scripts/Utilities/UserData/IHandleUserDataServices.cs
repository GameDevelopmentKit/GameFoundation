namespace GameFoundation.Scripts.Utilities.UserData
{
    using System;
    using GameFoundation.Scripts.Interfaces;

    public interface IHandleUserDataServices
    {
        /// <summary>
        /// Save a class data to local
        /// </summary>
        /// <param name="data">class data</param>
        /// <param name="force"> if true, save data immediately to local</param>
        /// <typeparam name="T"> type of class</typeparam>
        public void Save<T>(T data, bool force = false) where T : class;

        /// <summary>
        /// Load data from local
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T Load<T>() where T : class, ILocalData, new();

        public object Load(Type localDataType);

        public void SaveAll();
    }
}