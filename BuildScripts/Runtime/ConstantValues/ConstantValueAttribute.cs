namespace BuildScripts.Runtime.ConstantValues
{
    using System;

    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Class)]
    public class ConstantValueAttribute : Attribute
    {
        public string Key { get; }

        public ConstantValueAttribute(string key = null)
        {
            this.Key = key;
        }
    }
}