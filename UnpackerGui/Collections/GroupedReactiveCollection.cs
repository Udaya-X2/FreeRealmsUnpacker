using DynamicData.Binding;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;

namespace UnpackerGui.Collections;

/// <summary>
/// Represents a read-only wrapper for a sequence of subsequences of <typeparamref name="T"/>
/// flattened into a single sequence, with suspensible change notifications.
/// </summary>
/// <typeparam name="T">The type of the item in the subsequences.</typeparam>
public class GroupedReactiveCollection<T> : ReadOnlyReactiveCollection<T>
{
    private readonly IEnumerable<IEnumerable<T>> _items;

    private Lazy<int> _count;

    /// <summary>
    /// Initializes a new instance of the <see cref="GroupedReactiveCollection{T}"/> class from the specified sequence.
    /// </summary>
    public GroupedReactiveCollection(IEnumerable<IEnumerable<T>> items)
    {
        _items = items ?? throw new ArgumentNullException(nameof(items));
        _count = new Lazy<int>(GetGroupedCount);

        // Refresh the collection when the underlying collection changes.
        (items as INotifyCollectionChanged)?.ObserveCollectionChanges()
                                            .Subscribe(_ => Refresh());
    }

    /// <inheritdoc/>
    public override int Count => _count.Value;

    /// <inheritdoc/>
    public override IEnumerator<T> GetEnumerator() => _items.SelectMany(x => x).GetEnumerator();

    /// <inheritdoc/>
    public override void Refresh()
    {
        if (NotificationsEnabled)
        {
            _count = new Lazy<int>(GetGroupedCount);
        }

        base.Refresh();
    }

    /// <summary>
    /// Returns the number of items in each group.
    /// </summary>
    /// <returns>The number of items in each group.</returns>
    private int GetGroupedCount() => _items.Sum(x => x.Count());
}
