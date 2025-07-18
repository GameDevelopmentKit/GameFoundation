//-----------------------------------------------------------------------
// <copyright file="OdinLocalizationReflectionValidator.cs" company="Sirenix ApS">
// Copyright (c) Sirenix ApS. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

#if SIRENIX_INTERNAL
using System.Collections;
using System.Reflection;
using Sirenix.OdinInspector.Editor.Validation;
using Sirenix.OdinInspector.Modules.Localization.Editor.Internal;

[assembly: RegisterValidator(typeof(OdinLocalizationReflectionValidator))]
namespace Sirenix.OdinInspector.Modules.Localization.Editor.Internal
{
    public class OdinLocalizationReflectionValidator : GlobalValidator
    {
        public override IEnumerable RunValidation(ValidationResult result)
        {
            OdinLocalizationReflectionValues.EnsureInit();
            
            FieldInfo[] fields = typeof(OdinLocalizationReflectionValues).GetFields(BindingFlags.Static | BindingFlags.Public);

            for (var i = 0; i < fields.Length; i++)
            {
                if (fields[i].IsLiteral)
                {
                    continue;
                }

                if (fields[i].GetValue(null) != null)
                {
                    continue;
                }

                result.AddError($"[Odin Localization Module]: {fields[i].Name} was not found.");
            }

            return null;
        }
    }
}
#endif