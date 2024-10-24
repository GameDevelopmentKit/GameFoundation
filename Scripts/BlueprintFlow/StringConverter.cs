#nullable enable
namespace GameFoundation.BlueprintFlow
{
    using System;
    using TheOne.Data.Conversion;
    using UnityEngine.Scripting;

    [Preserve]
    public sealed class StringConverter : Converter<string>
    {
        protected override object? GetDefaultValue(Type type) => string.Empty;

        protected override object ConvertFromString(string str, Type type) => str;

        protected override string ConvertToString(object obj, Type type) => (string)obj;
    }
}