namespace GameFoundation.Scripts.Interfaces
{
    public interface ILocalData
    {
        /// <summary>
        /// Only call once when the data is created.
        /// </summary>
        void Init();

        void OnDataLoaded() { }
    }
}