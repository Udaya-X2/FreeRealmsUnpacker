using System;
using System.Runtime;

namespace UnpackerGui.Utils;

/// <summary>
/// Provides static helper methods related to system garbage collection.
/// </summary>
public static class GCUtils
{
    /// <summary>
    /// Forces a full garbage collection of all generations, which includes compacting the large object heap (LOH).
    /// </summary>
    public static void Collect()
    {
        GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
        GC.Collect(GC.MaxGeneration, GCCollectionMode.Aggressive, true, true);
    }
}
