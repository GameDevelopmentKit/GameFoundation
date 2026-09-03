namespace BlueprintFlow.BlueprintReader
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Reflection;
    using BlueprintFlow.BlueprintReader.Converter;
    using BlueprintFlow.BlueprintReader.Converter.TypeConversion;
    using Cysharp.Threading.Tasks;
    using Sylvan.Data.Csv;
    using MemberInfo = BlueprintFlow.BlueprintReader.Converter.MemberInfo;

    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class IgnoreBlueprintAttribute : Attribute
    {
    }

    /// <summary> Attribute used to mark the Header Key for GenericDatabaseByRow </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Property | AttributeTargets.Struct)]
    public class CsvHeaderKeyAttribute : Attribute
    {
        public readonly string HeaderKey;

        public CsvHeaderKeyAttribute(string headerKey) { this.HeaderKey = headerKey; }
    }

    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public class NestedBlueprintAttribute : Attribute
    {
    }

    /// <summary>
    ///     An abstraction class for databases with row-based header fields
    /// </summary>
    /// <typeparam name="T1">Type of header key</typeparam>
    /// <typeparam name="T2">Type of value</typeparam>
    public abstract class GenericBlueprintReaderByRow<T1, T2> : BlueprintByRow<T1, T2>, IGenericBlueprintReader
    {
        public virtual UniTask DeserializeFromCsv(string rawCsv)
        {
            this.CleanUp();

            using var csv = CsvDataReader.Create(new StringReader(rawCsv), CsvHelper.CsvDataReaderOptions);
            while (csv.Read()) this.Add(csv);

            return UniTask.CompletedTask;
        }

        public virtual List<List<string>> SerializeToRawData()
        {
            var rawData = this.ToRawData();
            rawData.Insert(0, this.GetHeader());

            return rawData;
        }

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

        List<List<string>> ToRawData();

        List<string> GetHeader();

        void CleanUp();
    }

    public class BlueprintByRow<TKey, TRecord> : Dictionary<TKey, TRecord>, IBlueprintCollection
    {
        private readonly BlueprintRecordReader<TRecord> blueprintRecordReader;

        // Need to be public due to reflection construction
        public BlueprintByRow() { this.blueprintRecordReader = new BlueprintRecordReader<TRecord>(this.GetType()); }

        public void Add(CsvDataReader inputCsv)
        {
            var (hasValue, record) = this.blueprintRecordReader.GetRecord(inputCsv);

            if (hasValue)
            {
                try
                {
                    this.Add(inputCsv.GetField<TKey>(this.blueprintRecordReader.RequireKey), record);
                }
                catch (Exception e)
                {
                    throw new Exception($"Blueprint {this.GetType().Name} {e}");
                }
            }
        }

        public List<string> GetHeader() { return this.blueprintRecordReader.GetHeader(); }

        public List<List<string>> ToRawData()
        {
            var result = new List<List<string>>();

            foreach (var record in this)
            {
                result.AddRange(this.blueprintRecordReader.ToRawData(record.Value));
            }

            return result;
        }

        public void CleanUp() { this.Clear(); }
    }

    // Need to be public due to reflection construction
    [Serializable]
    public class BlueprintByRow<TRecord> : List<TRecord>, IBlueprintCollection
    {
        private readonly BlueprintRecordReader<TRecord> blueprintRecordReader;

        // Need to be public due to reflection construction
        public BlueprintByRow() { this.blueprintRecordReader = new BlueprintRecordReader<TRecord>(this.GetType()); }

        public void Add(CsvDataReader inputCsv)
        {
            var (hasValue, value) = this.blueprintRecordReader.GetRecord(inputCsv);
            if (hasValue) this.Add(value);
        }

        public List<List<string>> ToRawData()
        {
            var result = new List<List<string>>();

            foreach (var record in this)
            {
                result.AddRange(this.blueprintRecordReader.ToRawData(record));
            }

            return result;
        }

        public List<string> GetHeader() { return this.blueprintRecordReader.GetHeader(); }

        public void CleanUp() { this.Clear(); }
    }

    public class BlueprintRecordReader
    {
        private readonly Type blueprintType;
        private readonly Type recordType;

        private readonly List<MemberInfo>                              fieldAndProperties;
        private          List<CachedBlueprintCollection>               blueprintCollectionMemberInfos;
        private          Dictionary<MemberInfo, BlueprintRecordReader> nestedMemberInfoToRecordReader;

        private List<IBlueprintCollection> listBlueprintCollections;

        public string RequireKey;

        private          CustomTypeConverterAttribute customTypeConverter;
        private          List<CachedMember>           cachedMembers;
        private          bool                         isCacheInitialized;
        private          List<string>                 cachedHeader;
        private readonly Func<object>                 factory;

        public BlueprintRecordReader(Type blueprintType, Type recordType)
        {
            this.blueprintType      = blueprintType;
            this.recordType         = recordType;
            this.fieldAndProperties = new List<MemberInfo>();

            var ctor = recordType.GetConstructor(Type.EmptyTypes)
                       ?? throw new InvalidOperationException($"{recordType.Name} requires a parameterless constructor.");

            this.factory = Expression.Lambda<Func<object>>(Expression.New(ctor)).Compile();
            this.Setup();
        }

        private void InitializeCache(CsvDataReader csv)
        {
            if (this.isCacheInitialized)
                return;

            this.cachedMembers = new List<CachedMember>();

            foreach (var memberInfo in this.fieldAndProperties)
            {
                if (memberInfo.IsDefined(typeof(IgnoreBlueprintAttribute)))
                    continue;

                var converter = CsvHelper.TypeConverterCache.GetConverter(memberInfo.MemberType);

                try
                {
                    this.cachedMembers.Add(new CachedMember
                    {
                        MemberInfo    = memberInfo,
                        Ordinal       = csv.GetOrdinal(memberInfo.MemberName),
                        MemberType    = memberInfo.MemberType,
                        Converter     = converter,
                        SpanConverter = converter as ISpanTypeConverter
                    });
                }
                catch (Exception e)
                {
                    throw new Exception($"{this.blueprintType.FullName} - {csv.GetField(this.RequireKey)} - {memberInfo.MemberName} : {e}");
                }
            }

            this.isCacheInitialized = true;
        }

        private void Setup()
        {
            var csvHeaderKeyAttribute =
                (CsvHeaderKeyAttribute)Attribute.GetCustomAttribute(this.recordType, typeof(CsvHeaderKeyAttribute));

            //todo will remove later, should place all CsvHeaderKeyAttribute on record class instead of the blueprint class
            if (csvHeaderKeyAttribute == null)
            {
                csvHeaderKeyAttribute = (CsvHeaderKeyAttribute)Attribute.GetCustomAttribute(this.blueprintType, typeof(CsvHeaderKeyAttribute));
            }

            if (csvHeaderKeyAttribute != null)
                this.RequireKey = csvHeaderKeyAttribute.HeaderKey;

            var memberInfos = this.recordType.GetAllFieldAndProperties();

            foreach (var memberInfo in memberInfos)
            {
                if (memberInfo.IsDefined(typeof(IgnoreBlueprintAttribute)))
                    continue;

                if (this.IsBlueprintCollection(memberInfo.MemberType))
                {
                    this.blueprintCollectionMemberInfos ??=
                        new List<CachedBlueprintCollection>();

                    var ctor = memberInfo.MemberType.GetConstructor(Type.EmptyTypes);

                    this.blueprintCollectionMemberInfos.Add(new CachedBlueprintCollection
                    {
                        MemberInfo = memberInfo,
                        Factory    = Expression.Lambda<Func<object>>(Expression.New(ctor)).Compile(),
                        FieldCount = memberInfo.MemberType.GetAllFieldAndProperties().Count
                    });
                }
                else if (this.IsBlueprintNested(memberInfo))
                {
                    this.nestedMemberInfoToRecordReader ??= new Dictionary<MemberInfo, BlueprintRecordReader>();
                    this.nestedMemberInfoToRecordReader.Add(memberInfo, new BlueprintRecordReader(memberInfo.MemberType, memberInfo.MemberType));
                }
                else
                {
                    //if require key still empty, set default is the first member name
                    if (string.IsNullOrEmpty(this.RequireKey)) this.RequireKey = memberInfo.MemberName;

                    this.fieldAndProperties.Add(memberInfo);
                }
            }

            this.customTypeConverter = this.recordType.GetCustomAttribute<CustomTypeConverterAttribute>();
        }

        public object GetRecord(CsvDataReader inputCsv)
        {
            if (this.customTypeConverter != null)
                return this.customTypeConverter.TypeConverter.ConvertFromCsv(inputCsv);

            this.InitializeCache(inputCsv);

            object record = null;

            if (!inputCsv.GetFieldSpan(inputCsv.GetOrdinal(this.RequireKey)).IsEmpty)
            {
                record = this.factory();

                foreach (var member in this.cachedMembers)
                {
                    try
                    {
                        object value;

                        if (member.SpanConverter != null)
                        {
                            value = member.SpanConverter.ConvertFromSpan(
                                inputCsv.GetFieldSpan(member.Ordinal),
                                member.MemberType);
                        }
                        else
                        {
                            value = member.Converter.ConvertFromString(
                                inputCsv.GetString(member.Ordinal),
                                member.MemberType);
                        }

                        member.MemberInfo.SetValue(record, value);
                    }
                    catch (IndexOutOfRangeException e)
                    {
                        throw new FieldDontExistInBlueprint(
                            $"{this.recordType.Name} - {inputCsv.GetField(this.RequireKey)} - {member.MemberInfo.MemberName} : {e}");
                    }
                    catch (Exception e)
                    {
                        throw new Exception(
                            $"{this.blueprintType.FullName} - {inputCsv.GetField(this.RequireKey)} - {member.MemberInfo.MemberName} : {e}");
                    }
                }

                if (this.blueprintCollectionMemberInfos != null)
                {
                    this.listBlueprintCollections ??= new List<IBlueprintCollection>();
                    this.listBlueprintCollections.Clear();

                    foreach (var subBlueprint in this.blueprintCollectionMemberInfos)
                    {
                        var subCollection = (IBlueprintCollection)subBlueprint.Factory();
                        subBlueprint.MemberInfo.SetValue(record, subCollection);
                        this.listBlueprintCollections.Add(subCollection);
                    }
                }

                if (this.nestedMemberInfoToRecordReader != null)
                {
                    foreach (var (nestedMemberInfo, recordReader) in this.nestedMemberInfoToRecordReader)
                    {
                        nestedMemberInfo.SetValue(
                            record,
                            recordReader.GetRecord(inputCsv));
                    }
                }
            }
            else
            {
                if (this.nestedMemberInfoToRecordReader != null)
                {
                    foreach (var (_, recordReader) in this.nestedMemberInfoToRecordReader)
                    {
                        recordReader.GetRecord(inputCsv);
                    }
                }
            }

            if (this.listBlueprintCollections != null)
            {
                foreach (var subCollection in this.listBlueprintCollections)
                {
                    subCollection.Add(inputCsv);
                }
            }

            return record;
        }

        public List<string> GetHeader()
        {
            if (this.cachedHeader != null)
                return this.cachedHeader;

            this.cachedHeader =
                new List<string>(
                    this.fieldAndProperties.Select(memberInfo => memberInfo.MemberName));

            if (this.nestedMemberInfoToRecordReader != null)
            {
                foreach (var (_, recordReader)
                         in this.nestedMemberInfoToRecordReader)
                {
                    this.cachedHeader.AddRange(
                        recordReader.GetHeader());
                }
            }

            if (this.blueprintCollectionMemberInfos != null)
            {
                foreach (var subBlueprint
                         in this.blueprintCollectionMemberInfos)
                {
                    var subCollection = (IBlueprintCollection)subBlueprint.Factory();

                    this.cachedHeader.AddRange(
                        subCollection.GetHeader());
                }
            }

            return this.cachedHeader;
        }

        public List<List<string>> ToRawData(object inputObject)
        {
            var result                  = new List<List<string>>();
            var notCollectionFieldCount = this.fieldAndProperties.Count;

            var newRow = new List<string>();
            result.Add(newRow);

            foreach (var memberInfo in this.fieldAndProperties)
            {
                var converter = CsvHelper.TypeConverterCache.GetConverter(memberInfo.MemberType);
                newRow.Add(converter.ConvertToString(memberInfo.GetValue(inputObject), memberInfo.MemberType));
            }

            if (this.nestedMemberInfoToRecordReader != null)
            {
                foreach (var (nestedMemberInfo, recordReader) in this.nestedMemberInfoToRecordReader)
                {
                    notCollectionFieldCount += recordReader.fieldAndProperties.Count;
                    var nestedObj              = nestedMemberInfo.GetValue(inputObject);
                    var nestedBlueprintRawData = recordReader.ToRawData(nestedObj);

                    for (int i = 0; i < nestedBlueprintRawData.Count; i++)
                    {
                        result[i].AddRange(nestedBlueprintRawData[i]);
                    }
                }
            }

            if (this.blueprintCollectionMemberInfos != null)
                foreach (var subBlueprintMemberInfo in this.blueprintCollectionMemberInfos)
                {
                    var subBlueprintData    = (IBlueprintCollection)subBlueprintMemberInfo.MemberInfo.GetValue(inputObject);
                    var subBlueprintRawData = subBlueprintData.ToRawData();

                    for (var index = 0; index < subBlueprintRawData.Count; index++)
                    {
                        if (index > result.Count - 1)
                        {
                            result.Add(Enumerable.Repeat(string.Empty, notCollectionFieldCount).ToList());
                        }
                        else if (result[index].Count < notCollectionFieldCount)
                        {
                            result[index].AddRange(Enumerable.Repeat(string.Empty, notCollectionFieldCount - result[index].Count));
                        }

                        result[index].AddRange(subBlueprintRawData[index]);
                    }

                    notCollectionFieldCount += subBlueprintMemberInfo.FieldCount;
                }

            return result;
        }

        private bool IsBlueprintCollection(Type type) =>
            (type.IsGenericType || type.BaseType is { IsGenericType: true }) &&
            typeof(IBlueprintCollection).IsAssignableFrom(type);

        private bool IsBlueprintNested(MemberInfo typeInfo) => typeInfo.IsDefined(typeof(NestedBlueprintAttribute)) && (typeInfo.MemberType.IsClass || typeInfo.MemberType.IsValueType);
    }

    public class BlueprintRecordReader<TRecord> : BlueprintRecordReader
    {
        public BlueprintRecordReader(Type blueprintType) : base(blueprintType, typeof(TRecord)) { }

        public new (bool, TRecord) GetRecord(CsvDataReader inputCsv)
        {
            var record = base.GetRecord(inputCsv);

            return record != null ? (true, (TRecord)record) : (false, default);
        }
    }
}