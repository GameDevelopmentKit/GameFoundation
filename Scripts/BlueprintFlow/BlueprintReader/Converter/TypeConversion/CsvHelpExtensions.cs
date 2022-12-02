namespace BlueprintFlow.BlueprintReader.Converter.TypeConversion
{
    public static class CsvHelpExtensions
    {
        // /// <summary>
        // /// Register extended type converter for CSVHelper based on the data types contained in the database class 
        // /// </summary>
        // /// <param name="typeConverterCache"></param>
        // /// <typeparam name="T">Type of the database class</typeparam>
        // public static void RegisterTypeConverter<T>(this TypeConverterCache typeConverterCache) { RegisterTypeConverter(typeConverterCache, typeof(T)); }
        //
        // /// <summary>
        // /// Register extended type converter for CSVHelper based on the data types contained in the database class 
        // /// </summary>
        // /// <param name="typeConverterCache"></param>
        // /// <param name="databaseType">Type of the database class</param>
        // public static void RegisterTypeConverter(this TypeConverterCache typeConverterCache, Type databaseType)
        // {
        //     foreach (var property in databaseType.GetProperties())
        //     {
        //         if (property.PropertyType.IsGenericType)
        //         {
        //             var genericTypeDefinition = property.PropertyType.GetGenericTypeDefinition();
        //             if (genericTypeDefinition == typeof(List<>))
        //             {
        //                 typeConverterCache.AddConverter(property.PropertyType, new ListGenericConverter());
        //             }
        //             else if (genericTypeDefinition == typeof(Dictionary<,>))
        //             {
        //                 typeConverterCache.AddConverter(property.PropertyType, new DictionaryGenericConverter());
        //             }
        //             else if (genericTypeDefinition == typeof(Tuple<,>))
        //             {
        //                 typeConverterCache.AddConverter(property.PropertyType, new PairGenericConverter());
        //             }
        //             else if (genericTypeDefinition == typeof(Tuple<,,>))
        //             {
        //                 typeConverterCache.AddConverter(property.PropertyType, new Tuple3GenericConverter());
        //             }
        //         }
        //         else
        //         {
        //             if (property.PropertyType.IsArray)
        //             {
        //                 typeConverterCache.AddConverter(property.PropertyType, new ArrayGenericConverter());
        //             }
        //
        //             if (property.PropertyType == typeof(DateTime))
        //             {
        //                 typeConverterCache.AddConverter(property.PropertyType, new DateConverter("o"));
        //             }
        //         }
        //     }
        // }
    }
}