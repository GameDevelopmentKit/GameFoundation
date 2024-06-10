namespace DataManager.LocalData
{
    using System;
    using Cysharp.Threading.Tasks;

    public interface IHandleLocalDataServices
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

        /// <summary>
        ///  Load data from local
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public UniTask<ILocalData> Load(Type type);

        public UniTask<ILocalData[]> Load(params Type[] types);

        public UniTask SaveAll();

        public UniTask DeleteAll();
    }
}