using DynamicData;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;

namespace UnpackerGui.Collections;

/// <summary>
/// Represents an <see cref="ObservableCollection{T}"/> with suppressible
/// change notifications and efficient methods to add or remove many elements.
/// </summary>
public partial class RangeObservableCollection<T> : ObservableCollection<T>
{
    private int _activeSuppressors;
    private bool _collectionChanged;

    /// <summary>
    /// Gets or sets whether to suppress change notifications.
    /// </summary>
    private bool SuppressNotifications
    {
        get => _activeSuppressors > 0;
        set
        {
            if (value)
            {
                _activeSuppressors++;
            }
            else
            {
                _activeSuppressors--;

                if (_collectionChanged)
                {
                    _collectionChanged = false;
                    OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
                }
            }
        }
    }

    /// <inheritdoc/>
    protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
    {
        if (SuppressNotifications)
        {
            _collectionChanged = true;
        }
        else
        {
            base.OnCollectionChanged(e);
        }
    }

    /// <summary>
    /// Suppresses change notifications until the return value is disposed.
    /// </summary>
    /// <returns>An object that, when disposed, reenables change notifications.</returns>
    public IDisposable SuppressChangeNotifications() => new NotificationSuppressor<T>(this);

    /// <summary>
    /// Performs the specified action on the collection with change notifications suppressed during the action.
    /// </summary>
    public void DoWhileSuppressed(Action<IList<T>> action)
    {
        using (SuppressChangeNotifications())
        {
            action(this);
        }
    }

    /// <inheritdoc cref="ListEx.Add{T}(IList{T}, IEnumerable{T})"/>
    public void Add(IEnumerable<T> items) => DoWhileSuppressed(x => x.Add(items));

    /// <inheritdoc cref="ListEx.AddOrInsertRange{T}(IList{T}, IEnumerable{T}, int)"/>
    public void AddOrInsertRange(IEnumerable<T> items, int index)
        => DoWhileSuppressed(x => x.AddOrInsertRange(items, index));

    /// <inheritdoc cref="ListEx.AddRange{T}(IList{T}, IEnumerable{T})"/>
    public void AddRange(IEnumerable<T> items)
        => DoWhileSuppressed(x => x.AddRange(items));

    /// <inheritdoc cref="ListEx.AddRange{T}(IList{T}, IEnumerable{T}, int)"/>
    public void AddRange(IEnumerable<T> items, int index)
        => DoWhileSuppressed(x => x.AddRange(items, index));

    /// <inheritdoc cref="ListEx.Remove{T}(IList{T}, IEnumerable{T})"/>
    public void Remove(IEnumerable<T> items)
        => DoWhileSuppressed(x => x.Remove(items));

    /// <inheritdoc cref="ListEx.RemoveMany{T}(IList{T}, IEnumerable{T})"/>
    public void RemoveMany(IEnumerable<T> items)
        => DoWhileSuppressed(x => x.RemoveMany(items));
}
