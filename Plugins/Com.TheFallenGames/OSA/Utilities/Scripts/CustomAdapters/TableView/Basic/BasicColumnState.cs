using System;

namespace Com.ForbiddenByte.OSA.CustomAdapters.TableView.Basic
{
    public class BasicColumnState : IColumnState
    {
        public IColumnInfo        Info               { get; private set; }
        public TableValueSortType CurrentSortingType { get; set; }
        public bool               CurrentlyReadOnly  { get; set; }
        public float              CurrentSize        { get; set; }

        public BasicColumnState(IColumnInfo info, bool readonlyByDefault)
        {
            this.Info               = info;
            this.CurrentSortingType = TableValueSortType.NONE;
            this.CurrentlyReadOnly  = readonlyByDefault;
            this.CurrentSize        = info.Size;
        }
    }
}