namespace BlueprintFlow.BlueprintReader
{
    using System.Threading.Tasks;

    /// <summary> Interface of database class </summary>
    public interface IGenericBlueprintReader
    {
        /// <summary>
        ///     Auto binding data from the raw Csv file to properties of database
        /// </summary>
        /// <param name="rawCsv"></param>
        /// <returns></returns>
        public Task DeserializeFromCsv(string rawCsv);
    }
}