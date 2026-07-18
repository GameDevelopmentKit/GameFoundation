namespace Localization.Blueprint
{
    using System;
    using UnityEngine;
    using UnityEngine.Scripting.APIUpdating;

    [Serializable]
    [MovedFrom(true, "Localization.Blueprint", "Game.Scripts", "LocalizationElementData")]
    public class LocalizationElementData
    {
        [SerializeField] private string tableName ="Default Table";
        [SerializeField] private string key;

        public string TableName { get => this.tableName; set => this.tableName = value; }
        public string Key       { get => this.key;       set => this.key = value; }
    }
}
