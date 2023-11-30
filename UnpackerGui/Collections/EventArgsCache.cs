using System.Collections.Specialized;
using System.ComponentModel;

namespace UnpackerGui.Collections;

internal static class EventArgsCache
{
    public static readonly PropertyChangedEventArgs CountPropertyChanged = new("Count");
    public static readonly PropertyChangedEventArgs IndexerPropertyChanged = new("Item[]");
    public static readonly PropertyChangedEventArgs UnfilteredCountPropertyChanged = new("UnfilteredCount");
    public static readonly NotifyCollectionChangedEventArgs ResetCollectionChanged
        = new(NotifyCollectionChangedAction.Reset);
}
