namespace BlueprintFlow.BlueprintReader
{
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using BlueprintFlow.BlueprintReader.Converter;
    using BlueprintFlow.BlueprintReader.Converter.TypeConversion;
    using Cysharp.Threading.Tasks;
    using Sylvan.Data.Csv;

    /// <summary> An abstraction class for databases with column-based header fields </summary>
    public abstract class GenericBlueprintReaderByCol : IGenericBlueprintReader
    {
        public UniTask DeserializeFromCsv(string rawCsv)
        {
            using var csv =
                CsvDataReader.Create(
                    new StringReader(rawCsv),
                    CsvHelper.CsvDataReaderOptions);

            var allMembers = this.GetType()
                .GetAllFieldAndProperties()
                .ToDictionary(
                    info => info.MemberName,
                    info => new
                    {
                        Member    = info,
                        Converter = CsvHelper.TypeConverterCache.GetConverter(info.MemberType)
                    });

            while (csv.Read())
            {
                if (allMembers.TryGetValue(csv.GetString(0), out var item))
                {
                    if (item.Converter is ISpanTypeConverter spanConverter)
                    {
                        item.Member.SetValue(
                            this,
                            spanConverter.ConvertFromSpan(
                                csv.GetFieldSpan(1),
                                item.Member.MemberType));
                    }
                    else
                    {
                        item.Member.SetValue(
                            this,
                            item.Converter.ConvertFromString(
                                csv.GetString(1),
                                item.Member.MemberType));
                    }
                }
            }

            return UniTask.CompletedTask;
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