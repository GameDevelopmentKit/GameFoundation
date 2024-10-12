namespace BlueprintFlow.BlueprintReader
{
    using System;

    /// <summary> attributes to store basic information of a blueprint </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public class BlueprintReaderAttribute : Attribute
    {
        public BlueprintReaderAttribute(
            string         dataPath,
            bool           isLoadFromResource = false,
            BlueprintScope blueprintScope     = BlueprintScope.Both
        )
        {
            this.DataPath           = dataPath;
            this.IsLoadFromResource = isLoadFromResource;
            this.BlueprintScope     = blueprintScope;
        }

        public string         DataPath           { get; }
        public bool           IsLoadFromResource { get; }
        public BlueprintScope BlueprintScope     { get; }
    }

    public enum BlueprintScope
    {
        Client,
        Server,
        Both,
        CLI,
    }
}