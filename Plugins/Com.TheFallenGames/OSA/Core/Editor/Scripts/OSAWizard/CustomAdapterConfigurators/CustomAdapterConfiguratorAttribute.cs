using System;

namespace Com.ForbiddenByte.OSA.Editor.OSAWizard.CustomAdapterConfigurators
{
    public class CustomAdapterConfiguratorAttribute : Attribute
    {
        public readonly Type ConfiguredType;

        public CustomAdapterConfiguratorAttribute(Type configuredType)
        {
            this.ConfiguredType = configuredType;
        }
    }
}