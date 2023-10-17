using System.Collections.Specialized;
using System.ComponentModel;

namespace UnpackerGui.Collections;

internal static class EventArgsCache
{
    public static readonly PropertyChangedEventArgs CountPropertyChanged;
    public static readonly PropertyChangedEventArgs IndexerPropertyChanged;
    public static readonly NotifyCollectionChangedEventArgs ResetCollectionChanged;

    static EventArgsCache()
    {
        CountPropertyChanged = new PropertyChangedEventArgs("Count");
        IndexerPropertyChanged = new PropertyChangedEventArgs("Item[]");
        ResetCollectionChanged = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset);
    }
}
