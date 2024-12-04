namespace Utilities.Utils
{
    using System;
    using System.Runtime;

    public class GCUtils
    {
        public static void ForceGCWithLOH()
        {
            // Set LOH to compact during the next full collection
            GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;

            // Force a full GC, including LOH compaction
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
        }
    }
}