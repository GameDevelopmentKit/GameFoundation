namespace DataManager.Blueprint.BlueprintReader
{
    using System.Collections.Generic;
    using Cysharp.Threading.Tasks;

    /// <summary> Interface of database class </summary>
    public interface IGenericBlueprintReader
    {
        /// <summary>
        ///     Auto binding data from the raw Csv file to properties of database
        /// </summary>
        /// <param name="rawCsv"></param>
        /// <returns></returns>
        public UniTask DeserializeFromCsv(string rawCsv);

        public List<List<string>> SerializeToRawData();
    }
}