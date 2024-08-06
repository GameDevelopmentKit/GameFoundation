namespace DataManager.Blueprint.BlueprintReader
{
    using System;
    using DataManager.Blueprint.BlueprintController;

    /// <summary> attributes to store basic information of a blueprint </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public class BlueprintReaderAttribute : Attribute
    {
        public BlueprintReaderAttribute(string dataPath, BlueprintSourceType customSource = BlueprintSourceType.None, BlueprintScope blueprintScope = BlueprintScope.Both)
        {
            this.DataPath       = dataPath;
            this.CustomSource   = customSource;
            this.BlueprintScope = blueprintScope;
        }
        public string          DataPath       { get; }
        public BlueprintSourceType CustomSource   { get; }
        public BlueprintScope  BlueprintScope { get; }
    }

    public enum BlueprintScope
    {
        Client,
        Server,
        Both,
        CLI
    }
}