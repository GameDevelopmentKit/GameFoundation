namespace BlueprintFlow.BlueprintReader
{
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using BlueprintFlow.BlueprintReader.Converter;
    using Cysharp.Threading.Tasks;
    using Sylvan.Data.Csv;

    /// <summary> An abstraction class for databases with column-based header fields </summary>
    public abstract class GenericBlueprintReaderByCol : IGenericBlueprintReader
    {
        public async UniTask DeserializeFromCsv(string rawCsv)
        {
            await using var csv =
                await CsvDataReader.CreateAsync(new StringReader(rawCsv), CsvHelper.CsvDataReaderOptions);

            var allMembers = this.GetType().GetAllFieldAndProperties()
                .ToDictionary(info => info.MemberName, info => info);

            while (await csv.ReadAsync())
                if (allMembers.TryGetValue(csv.GetString(0), out var memberInfo))
                    memberInfo.SetValue(this, CsvHelper.TypeConverterCache.GetConverter(memberInfo.MemberType)
                            .ConvertFromString(csv.GetString(1), memberInfo.MemberType));
        }

        public List<List<string>> SerializeToRawData()
        {
            var allMembers = this.GetType().GetAllFieldAndProperties()
                .ToDictionary(info => info.MemberName, info => info);

            var rawData = new List<List<string>> { new List<string>(allMembers.Keys) };

            foreach (var member in allMembers.Values)
            {
                var value = member.GetValue(this);
                var convertedValue = CsvHelper.TypeConverterCache.GetConverter(member.MemberType)
                    .ConvertToString(value, member.MemberType);
                rawData.Add(new List<string> { member.MemberName, convertedValue });
            }

            return rawData;
        }
    }
}