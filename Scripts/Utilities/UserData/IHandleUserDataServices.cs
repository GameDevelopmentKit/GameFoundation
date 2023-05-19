namespace GameFoundation.Scripts.Utilities.UserData
{
    using System;
    using Cysharp.Threading.Tasks;
    using GameFoundation.Scripts.Interfaces;

    public interface IHandleUserDataServices
    {
        /// <summary>
        /// Save a class data to local
        /// </summary>
        /// <param name="data">class data</param>
        /// <param name="force"> if true, save data immediately to local</param>
        /// <typeparam name="T"> type of class</typeparam>
        public UniTask Save<T>(T data, bool force = false) where T : class, ILocalData;

        /// <summary>
        /// Load data from local
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public UniTask<T> Load<T>() where T : class, ILocalData;

        public UniTask<ILocalData[]> Load(params Type[] types);

        public UniTask SaveAll();
    }
}