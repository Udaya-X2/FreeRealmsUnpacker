using System;
using System.Collections;
using System.Collections.Specialized;
using System.ComponentModel;

namespace UnpackerGui.Collections;

/// <summary>
/// Represents an <see cref="IList"/> that provides notifications upon calls to <see cref="OnCollectionChanged"/>.
/// </summary>
public class ControlledObservableList : IList, INotifyCollectionChanged, INotifyPropertyChanged
{
    /// <summary>
    /// Gets or sets the items in the <see cref="ControlledObservableList"/>.
    /// </summary>
    public IList Items { get; set; } = Array.Empty<object>();

    /// <inheritdoc/>
    public object? this[int index]
    {
        get => Items[index];
        set => Items[index] = value;
    }

    /// <inheritdoc/>
    public bool IsFixedSize => Items.IsFixedSize;

    /// <inheritdoc/>
    public bool IsReadOnly => Items.IsReadOnly;

    /// <inheritdoc/>
    public int Count => Items.Count;

    /// <inheritdoc/>
    public bool IsSynchronized => Items.IsSynchronized;

    /// <inheritdoc/>
    public object SyncRoot => Items.SyncRoot;

    /// <inheritdoc/>
    public event NotifyCollectionChangedEventHandler? CollectionChanged;

    /// <inheritdoc/>
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// Fires a notification indicating the collection and its properties were changed.
    /// </summary>
    public void OnCollectionChanged(object? sender, EventArgs args)
    {
        CollectionChanged?.Invoke(this, EventArgsCache.ResetCollectionChanged);
        PropertyChanged?.Invoke(this, EventArgsCache.CountPropertyChanged);
        PropertyChanged?.Invoke(this, EventArgsCache.IndexerPropertyChanged);
    }

    /// <inheritdoc/>
    public int Add(object? value) => Items.Add(value);

    /// <inheritdoc/>
    public void Clear() => Items.Clear();

    /// <inheritdoc/>
    public bool Contains(object? value) => Items.Contains(value);

    /// <inheritdoc/>
    public void CopyTo(Array array, int index) => Items.CopyTo(array, index);

    /// <inheritdoc/>
    public IEnumerator GetEnumerator() => Items.GetEnumerator();

    /// <inheritdoc/>
    public int IndexOf(object? value) => Items.IndexOf(value);

    /// <inheritdoc/>
    public void Insert(int index, object? value) => Items.Insert(index, value);

    /// <inheritdoc/>
    public void Remove(object? value) => Items.Remove(value);

    /// <inheritdoc/>
    public void RemoveAt(int index) => Items.RemoveAt(index);
}
