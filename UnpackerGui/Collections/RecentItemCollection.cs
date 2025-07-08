using DynamicData.Binding;
using System;
using System.Collections.Generic;
using System.Reactive.Disposables;

namespace UnpackerGui.Collections;

/// <summary>
/// Represents an observable collection of unique recent items. New items are
/// added to the start of the collection while old items are removed from the end.
/// </summary>
public class RecentItemCollection<T> : ObservableCollectionExtended<T>
{
    private const int DefaultCapacity = 10;

    private int _capacity;
    private bool _insertStart = true;

    /// <summary>
    /// Initializes a new instance of <see cref="RecentItemCollection{T}"/> with the default capacity.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException"/>
    public RecentItemCollection()
        : this(DefaultCapacity)
    {
    }

    /// <summary>
    /// Initializes a new instance of <see cref="RecentItemCollection{T}"/> with the specified capacity.
    /// </summary>
    /// <param name="capacity">The maximum number of items allowed.</param>
    /// <exception cref="ArgumentOutOfRangeException"/>
    public RecentItemCollection(int capacity)
    {
        Capacity = capacity;
    }

    /// <summary>
    /// Initializes a new instance of <see cref="RecentItemCollection{T}"/>
    /// from the specified collection, with the default capacity.
    /// </summary>
    /// <param name="collection">The collection whose elements are added to the items.</param>
    /// <exception cref="ArgumentNullException"/>
    public RecentItemCollection(IEnumerable<T> collection)
        : this(collection, DefaultCapacity)
    {

    }

    /// <summary>
    /// Initializes a new instance of <see cref="RecentItemCollection{T}"/>
    /// from the specified collection, with the given capacity.
    /// </summary>
    /// <param name="collection">The collection whose elements are added to the items.</param>
    /// <param name="capacity">The maximum number of items allowed.</param>
    /// <exception cref="ArgumentNullException"/>
    /// <exception cref="ArgumentOutOfRangeException"/>
    public RecentItemCollection(IEnumerable<T> collection, int capacity)
    {
        Capacity = capacity;

        using (ReverseInsert())
        {
            foreach (T item in collection)
            {
                InsertItem(Count, item);
            }
        }
    }

    /// <summary>
    /// The maximum number of items allowed.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException"/>
    public int Capacity
    {
        get => _capacity;
        set
        {
            ArgumentOutOfRangeException.ThrowIfNegative(value);

            _capacity = value;
        }
    }

    /// <summary>
    /// Overrides the default <see cref="InsertItem(int, T)"/>, placing new items at the end of the
    /// collection while removing old items from the start. When disposed, insertion returns to normal.
    /// </summary>
    /// <returns>A disposable that, when disposed, returns insertion back to normal.</returns>
    public IDisposable ReverseInsert()
    {
        _insertStart = false;
        return Disposable.Create(() => _insertStart = true);
    }

    /// <inheritdoc/>
    protected override void SetItem(int index, T item)
    {
        int idx = IndexOf(item);

        if (idx == index) return;

        base.SetItem(index, item);

        if (idx >= 0) RemoveAt(idx);
    }

    /// <inheritdoc/>
    protected override void InsertItem(int index, T item)
    {
        if (_insertStart)
        {
            int idx = IndexOf(item);

            if (idx >= 0)
            {
                if (idx == 0) return;

                RemoveAt(idx);
            }

            base.InsertItem(0, item);

            if (Count > Capacity)
            {
                RemoveItem(Count - 1);
            }
        }
        else
        {
            int idx = IndexOf(item);

            if (idx >= 0)
            {
                if (idx == Count - 1) return;

                RemoveAt(idx);
            }

            base.InsertItem(Count, item);

            if (Count > Capacity)
            {
                RemoveItem(0);
            }
        }
    }
}
