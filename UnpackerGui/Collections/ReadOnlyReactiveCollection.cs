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
/// Represents a read-only wrapper for a collection with suspensible change notifications.
/// </summary>
/// <typeparam name="T">The type of the item.</typeparam>
public abstract class ReadOnlyReactiveCollection<T>
    : IReadOnlyCollection<T>,
      ICollection<T>,
      INotifyCollectionChanged,
      INotifyPropertyChanged,
      INotifyCollectionChangedSuspender
{
    private int _notificationSuppressors;
    private bool _notificationSuppressed;

    /// <inheritdoc/>
    public abstract int Count { get; }

    /// <inheritdoc/>
    public bool IsReadOnly => true;

    /// <summary>
    /// Gets or sets whether change notifications are enabled.
    /// </summary>
    protected virtual bool NotificationsEnabled
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
    public event NotifyCollectionChangedEventHandler? CollectionChanged;

    /// <inheritdoc/>
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <inheritdoc/>
    public virtual bool Contains(T item) => item == null
        ? throw new ArgumentNullException(nameof(item))
        : this.AsEnumerable().Contains(item);

    /// <inheritdoc/>
    public virtual void CopyTo(T[] array, int arrayIndex)
    {
        if (array == null) throw new ArgumentNullException(nameof(array));
        if (array.Length - arrayIndex < Count) throw new ArgumentException(SR.Argument_InvalidOffLen);
        
        foreach (T item in this)
        {
            array[arrayIndex++] = item;
        }
    }

    /// <inheritdoc/>
    public abstract IEnumerator<T> GetEnumerator();

    /// <inheritdoc/>
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    /// <summary>
    /// Fires a notification indicating the collection and its properties were changed.
    /// </summary>
    public virtual void Refresh()
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
    public virtual IDisposable SuspendNotifications()
    {
        NotificationsEnabled = false;
        return Disposable.Create(() => NotificationsEnabled = true);
    }

    /// <inheritdoc/>
    IDisposable INotifyCollectionChangedSuspender.SuspendCount() => throw new NotSupportedException();

    /// <inheritdoc/>
    void ICollection<T>.Add(T item) => throw new NotSupportedException();

    /// <inheritdoc/>
    void ICollection<T>.Clear() => throw new NotSupportedException();

    /// <inheritdoc/>
    bool ICollection<T>.Remove(T item) => throw new NotSupportedException();
}
