using DynamicData.Binding;
using System;
using System.Collections.Generic;
using System.Linq;

namespace UnpackerGui.Collections;

/// <summary>
/// Represents an observable collection of recent items.
/// </summary>
public class RecentItemCollection<T> : ObservableCollectionExtended<T>
{
    private const int DefaultCapacity = 10;

    private int _capacity;

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
        ArgumentOutOfRangeException.ThrowIfNegative(capacity);

        foreach (T item in collection.TakeLast(capacity))
        {
            base.InsertItem(Count, item);
        }

        Capacity = capacity;
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
    /// Adds the specified items to the collection.
    /// </summary>
    /// <param name="collection">The collection of items to add.</param>
    /// <exception cref="ArgumentNullException"><paramref name="collection"/> is null.</exception>
    public void AddItems(IEnumerable<T> collection)
    {
        using (SuspendNotifications())
        {
            AddRange(collection);
        }
    }

    /// <inheritdoc/>
    protected override void InsertItem(int index, T item)
    {
        int idx = IndexOf(item);

        if (idx != -1)
        {
            RemoveAt(idx);
        }

        base.InsertItem(0, item);

        if (Count > Capacity)
        {
            RemoveItem(Count - 1);
        }
    }
}
