using DynamicData.Binding;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Disposables;

namespace UnpackerGui.Collections;

/// <summary>
/// Represents a read-only wrapper for a collection of <typeparamref name="TSource"/>
/// flattened into a sequence of <typeparamref name="TResult"/>.
/// <para/>
/// Provides suspensible notifications upon calls to <see cref="Refresh"/>.
/// </summary>
public class GroupedReactiveCollection<TSource, TResult>
    : IReadOnlyCollection<TResult>, INotifyCollectionChanged, INotifyPropertyChanged, INotifyCollectionChangedSuspender
    where TSource : IEnumerable<TResult>
{
    private readonly IEnumerable<TSource> _items;

    private int _notificationSuppressors;
    private bool _notificationSuppressed;

    /// <summary>
    /// Initializes a new instance of the <see cref="GroupedReactiveCollection{TSource, TResult}"/>
    /// class from the specified sequence.
    /// </summary>
    public GroupedReactiveCollection(IEnumerable<TSource> items)
    {
        _items = items;
    }

    /// <summary>
    /// Gets or sets whether change notifications are enabled.
    /// </summary>
    private bool NotificationsEnabled
    {
        get => _notificationSuppressors == 0;
        set
        {
            if (!value)
            {
                _notificationSuppressors++;
            }
            else if (--_notificationSuppressors == 0 && _notificationSuppressed)
            {
                _notificationSuppressed = false;
                Refresh();
            }
        }
    }

    /// <inheritdoc/>
    public int Count => _items.Sum(x => x.Count());

    /// <inheritdoc/>
    public IEnumerator<TResult> GetEnumerator() => _items.SelectMany(x => x).GetEnumerator();

    /// <inheritdoc/>
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    /// <inheritdoc/>
    public event NotifyCollectionChangedEventHandler? CollectionChanged;

    /// <inheritdoc/>
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// Fires a notification indicating the collection and its properties were changed.
    /// </summary>
    public void Refresh()
    {
        if (NotificationsEnabled)
        {
            CollectionChanged?.Invoke(this, EventArgsCache.ResetCollectionChanged);
            PropertyChanged?.Invoke(this, EventArgsCache.CountPropertyChanged);
        }
        else
        {
            _notificationSuppressed = true;
        }
    }

    /// <inheritdoc/>
    public IDisposable SuspendNotifications()
    {
        NotificationsEnabled = false;
        return Disposable.Create(() => NotificationsEnabled = true);
    }

    IDisposable INotifyCollectionChangedSuspender.SuspendCount() => throw new NotImplementedException();
}
