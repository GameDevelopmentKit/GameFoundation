namespace GameFoundation.Scripts.BlueprintFlow.BlueprintReader
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using GameFoundation.Scripts.BlueprintFlow.BlueprintReader.CsvHelper;
    using GameFoundation.Scripts.Utilities.Extension;
    using GameFoundation.Scripts.Utilities.Utils;
    using Sylvan.Data.Csv;

    /// <summary> Attribute used to mark the Header Key for GenericDatabaseByRow </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Property)]
    public class CsvHeaderKeyAttribute : Attribute
    {
        public readonly string HeaderKey;

        public CsvHeaderKeyAttribute(string headerKey) { this.HeaderKey = headerKey; }
    }

    /// <summary>
    ///     An abstraction class for databases with row-based header fields
    /// </summary>
    /// <typeparam name="T1">Type of header key</typeparam>
    /// <typeparam name="T2">Type of value</typeparam>
    public abstract class GenericBlueprintReaderByRow<T1, T2> : BlueprintByRow<T1, T2>, IGenericBlueprintReader
        where T2 : class
    {
        public virtual async Task DeserializeFromCsv(string rawCsv)
        {
            this.CleanUp();
            await using var csv =
                await CsvDataReader.CreateAsync(new StringReader(rawCsv), CsvHelper.CsvHelper.CsvDataReaderOptions);
            while (await csv.ReadAsync()) this.Add(csv);
        }

        public virtual List<List<string>> SerializeToRawData() { return this.ToRawData(true); }

        public T2 GetDataById(T1 id)
        {
            if (this.TryGetValue(id, out var result))
                return result;

            throw new InvalidDataException($"Blueprint {this.GetType().Name} doesn't contain Id {id}");
        }
    }

    public interface IBlueprintCollection
    {
        void Add(CsvDataReader inputCsv);

        List<List<string>> ToRawData(bool containHeader = false);

        void CleanUp();
    }

    public class BlueprintByRow<TKey, TRecord> : Dictionary<TKey, TRecord>, IBlueprintCollection where TRecord : class
    {
        private readonly BlueprintRecordReader<TRecord> blueprintRecordReader;

        // Need to be public due to reflection construction
        public BlueprintByRow() { this.blueprintRecordReader = new BlueprintRecordReader<TRecord>(this.GetType()); }

        public void Add(CsvDataReader inputCsv)
        {
            var record = this.blueprintRecordReader.GetRecord(inputCsv);
            if (record != null)
                this.Add(inputCsv.GetField<TKey>(this.blueprintRecordReader.RequireKey), record);
        }
        public List<List<string>> ToRawData(bool containHeader = false)
        {
            var result    = new List<List<string>>();
            var addHeader = containHeader;
            foreach (var record in this)
            {
                result.AddRange(this.blueprintRecordReader.ToRawData(record.Value, addHeader));
                addHeader = false;
            }

            return result;
        }

        public void CleanUp() { this.Clear(); }
    }

    // Need to be public due to reflection construction
    public class BlueprintByRow<TRecord> : List<TRecord>, IBlueprintCollection where TRecord : class
    {
        private readonly BlueprintRecordReader<TRecord> blueprintRecordReader;

        // Need to be public due to reflection construction
        public BlueprintByRow() { this.blueprintRecordReader = new BlueprintRecordReader<TRecord>(this.GetType()); }

        public void Add(CsvDataReader inputCsv) { this.AddNotNull(this.blueprintRecordReader.GetRecord(inputCsv)); }

        public List<List<string>> ToRawData(bool containHeader = false)
        {
            var result    = new List<List<string>>();
            var addHeader = containHeader;
            foreach (var record in this)
            {
                result.AddRange(this.blueprintRecordReader.ToRawData(record, addHeader));
                addHeader = false;
            }

            return result;
        }

        public void CleanUp() { this.Clear(); }
    }

    public class BlueprintRecordReader<TRecord> where TRecord : class
    {
        private readonly Type blueprintType;

        private readonly List<CsvHelper.CsvHelper.MemberInfo> fieldAndProperties;

        private List<IBlueprintCollection>           listSubBlueprintCollections;
        public  string                               RequireKey;
        private List<CsvHelper.CsvHelper.MemberInfo> subBlueprintMemberInfos;

        public BlueprintRecordReader(Type blueprintType)
        {
            this.blueprintType      = blueprintType;
            this.fieldAndProperties = new List<CsvHelper.CsvHelper.MemberInfo>();
            this.Setup();
        }

        private void Setup()
        {
            var csvHeaderKeyAttribute =
                (CsvHeaderKeyAttribute)Attribute.GetCustomAttribute(this.blueprintType, typeof(CsvHeaderKeyAttribute));
            if (csvHeaderKeyAttribute != null)
                this.RequireKey = csvHeaderKeyAttribute.HeaderKey;

            var recordType  = typeof(TRecord);
            var memberInfos = recordType.GetAllFieldAndProperties();
            foreach (var memberInfo in memberInfos)
                if (!this.IsBlueprintCollection(memberInfo.MemberType))
                {
                    //if require key still empty, set default is the first member name
                    if (string.IsNullOrEmpty(this.RequireKey)) this.RequireKey = memberInfo.MemberName;

                    this.fieldAndProperties.Add(memberInfo);
                }
                else
                {
                    this.subBlueprintMemberInfos ??= new List<CsvHelper.CsvHelper.MemberInfo>();
                    this.subBlueprintMemberInfos.Add(memberInfo);
                }
        }

        public TRecord GetRecord(CsvDataReader inputCsv)
        {
            TRecord record = null;
            if (!string.IsNullOrEmpty(inputCsv.GetField(this.RequireKey)))
            {
                record = Activator.CreateInstance<TRecord>();

                foreach (var memberInfo in this.fieldAndProperties)
                    try
                    {
                        var ordinal = inputCsv.GetOrdinal(memberInfo.MemberName);
                        memberInfo.SetValue(record, inputCsv.GetField(memberInfo.MemberType, ordinal));
                    }
                    catch (IndexOutOfRangeException e)
                    {
                        throw new FieldDontExistInBlueprint(
                            $"{this.blueprintType.FullName} - {memberInfo.MemberName}- {e}");
                    }
                    catch (Exception e)
                    {
                        throw new Exception($"{this.blueprintType.FullName} - {memberInfo.MemberName}- {e}");
                    }


                if (this.subBlueprintMemberInfos != null)
                {
                    //Create new sub blueprints if exist
                    this.listSubBlueprintCollections ??= new List<IBlueprintCollection>();
                    this.listSubBlueprintCollections.Clear();

                    foreach (var subBlueprintMemberInfo in this.subBlueprintMemberInfos)
                    {
                        var subCollection =
                            (IBlueprintCollection)Activator.CreateInstance(subBlueprintMemberInfo.MemberType);
                        subBlueprintMemberInfo.SetValue(record, subCollection);
                        this.listSubBlueprintCollections.Add(subCollection);
                    }
                }
            }

            if (this.listSubBlueprintCollections != null)
                foreach (var subCollection in this.listSubBlueprintCollections)
                    subCollection.Add(inputCsv);

            return record;
        }

        public List<List<string>> ToRawData(TRecord inputObject, bool containHeader = false)
        {
            var result = new List<List<string>>();
            if (containHeader) result.Add(this.fieldAndProperties.Select(memberInfo => memberInfo.MemberName).ToList());

            var newRow = new List<string>();
            result.Add(newRow);
            foreach (var memberInfo in this.fieldAndProperties)
            {
                var converter = CsvHelper.CsvHelper.TypeConverterCache.GetConverter(memberInfo.MemberType);
                newRow.Add(converter.ConvertToString(memberInfo.GetValue(inputObject), memberInfo.MemberType));
            }


            if (this.subBlueprintMemberInfos != null)
                foreach (var subBlueprintMemberInfo in this.subBlueprintMemberInfos)
                {
                    var subBlueprintData    = (IBlueprintCollection)subBlueprintMemberInfo.GetValue(inputObject);
                    var subBlueprintRawData = subBlueprintData.ToRawData(containHeader);
                    for (var index = 0; index < subBlueprintRawData.Count; index++)
                    {
                        if (index > result.Count - 1)
                            result.Add(Enumerable.Repeat(string.Empty, this.fieldAndProperties.Count).ToList());

                        result[index].AddRange(subBlueprintRawData[index]);
                    }
                }

            return result;
        }

        private bool IsBlueprintCollection(Type type)
        {
            return (type.IsGenericType || type.BaseType is { IsGenericType: true }) &&
                   typeof(IBlueprintCollection).IsAssignableFrom(type);
        }
    }
}