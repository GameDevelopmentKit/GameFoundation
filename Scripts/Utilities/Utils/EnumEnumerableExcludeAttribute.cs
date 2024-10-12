// Copyright (c) Microsoft.All Rights Reserved.Licensed under the MIT license.See License.txt in the project root for license information.

namespace GameFoundation.Scripts.Utilities.Utils
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    [AttributeUsage(AttributeTargets.Enum)]
    public class EnumEnumerableExcludeAttribute : Attribute
    {
        private readonly HashSet<string> exclusions;

        public EnumEnumerableExcludeAttribute()
        {
            this.exclusions = new();
        }

        public EnumEnumerableExcludeAttribute(params string[] exclusions)
        {
            this.exclusions = exclusions.ToHashSet();
        }

        public bool IsExcluded<TEnum>(TEnum enumValue) where TEnum : Enum
        {
            return this.exclusions.Contains(enumValue.ToString());
        }
    }
}