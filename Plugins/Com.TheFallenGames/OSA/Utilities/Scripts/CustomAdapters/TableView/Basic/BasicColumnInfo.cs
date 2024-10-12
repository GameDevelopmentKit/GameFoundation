using System;

namespace Com.ForbiddenByte.OSA.CustomAdapters.TableView.Basic
{
    public class BasicColumnInfo : IColumnInfo
    {
        public string Name
        {
            get => this._Name;
            set
            {
                if (this._Name == value) return;

                this._Name = value;
                this.ReconstructDisplayName();
            }
        }

        public string         DisplayName   { get; set; }
        public TableValueType ValueType     { get; private set; }
        public Type           EnumValueType { get; private set; }
        public float          Size          => this._Size;

        private string _Name;
        private float  _Size = -1;

        public BasicColumnInfo(string name, TableValueType valueType, Type enumValueType = null, float? customSize = null)
        {
            this.ValueType     = valueType;
            this.EnumValueType = enumValueType;
            if (customSize != null) this._Size = customSize.Value;

            // Setting it last, so the display name will be reconstructed using the other properties
            this.Name = name;
        }

        private void ReconstructDisplayName()
        {
            this.DisplayName = ConstructColumnDisplayName(this.Name, this.ValueType, this.EnumValueType);
        }

        public static string ConstructColumnDisplayName(string name, TableValueType valueType, Type enumValueType = null)
        {
            string innerStr;
            if (valueType == TableValueType.ENUMERATION)
                innerStr = "ENUM <i>" + (enumValueType == null ? "<Unknown>" : enumValueType.Name) + "</i>";
            else
                innerStr = valueType.ToString();

            return name + "\n<color=#00000070><size=12>" + innerStr + "</size></color>";
        }
    }
}