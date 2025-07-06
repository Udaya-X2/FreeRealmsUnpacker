using DynamicData.Binding;
using System;
using System.Collections.Generic;
using System.Linq;

namespace UnpackerGui.Collections;

/// <summary>
/// Represents an observable collection of recent files.
/// </summary>
public class RecentFileCollection : ObservableCollectionExtended<string>
{
    private const int DefaultCapacity = 10;

    private int _capacity;

    /// <summary>
    /// Initializes a new instance of <see cref="RecentFileCollection"/> with the default capacity.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException"/>
    public RecentFileCollection()
        : this(DefaultCapacity)
    {
    }

    /// <summary>
    /// Initializes a new instance of <see cref="RecentFileCollection"/> with the specified capacity.
    /// </summary>
    /// <param name="capacity">The maximum number of files allowed.</param>
    /// <exception cref="ArgumentOutOfRangeException"/>
    public RecentFileCollection(int capacity)
    {
        Capacity = capacity;
    }

    /// <summary>
    /// Initializes a new instance of <see cref="RecentFileCollection"/>
    /// from the specified collection, with the default capacity.
    /// </summary>
    /// <param name="collection">The collection whose elements are added to the files.</param>
    /// <exception cref="ArgumentNullException"/>
    public RecentFileCollection(IEnumerable<string> collection)
        : this(collection, DefaultCapacity)
    {

    }

    /// <summary>
    /// Initializes a new instance of <see cref="RecentFileCollection"/>
    /// from the specified collection, with the given capacity.
    /// </summary>
    /// <param name="collection">The collection whose elements are added to the files.</param>
    /// <param name="capacity">The maximum number of files allowed.</param>
    /// <exception cref="ArgumentNullException"/>
    /// <exception cref="ArgumentOutOfRangeException"/>
    public RecentFileCollection(IEnumerable<string> collection, int capacity)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(capacity);

        foreach (string item in collection.TakeLast(capacity))
        {
            base.InsertItem(Count, item);
        }

        Capacity = capacity;
    }

    /// <summary>
    /// The maximum number of files allowed.
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
    /// Adds the specified files to the collection.
    /// </summary>
    /// <param name="collection">The collection of files to add.</param>
    /// <exception cref="ArgumentNullException"><paramref name="collection"/> is null.</exception>
    public void AddFiles(IEnumerable<string> collection)
    {
        using (SuspendNotifications())
        {
            AddRange(collection);
        }
    }

    /// <inheritdoc/>
    protected override void InsertItem(int index, string item)
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
