//-----------------------------------------------------------------------
// <copyright file="LocalizationSupport.cs" company="Sirenix ApS">
// Copyright (c) Sirenix ApS. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

#if UNITY_EDITOR

namespace Sirenix.OdinInspector.Modules.Localization.Editor
{
    using UnityEngine.Localization;
    using Sirenix.OdinInspector.Editor;
    using System;
    using Sirenix.Utilities.Editor;

    public class LocalizedReferenceResolver : OdinPropertyResolver<LocalizedReference>
    {
        public override int ChildNameToIndex(string name)
        {
            throw new NotSupportedException();
        }

        public override int ChildNameToIndex(ref StringSlice name)
        {
            throw new NotSupportedException();
        }

        public override InspectorPropertyInfo GetChildInfo(int childIndex)
        {
            throw new NotSupportedException();
        }

        protected override int GetChildCount(LocalizedReference value)
        {
            return 0;
        }
    }
}
#endif