using System;
using System.Collections.Generic;
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

    /// <summary>
    /// Initializes a new instance of the <see cref="GroupedReactiveCollection{T}"/> class from the specified sequence.
    /// </summary>
    public GroupedReactiveCollection(IEnumerable<IEnumerable<T>> items)
    {
        _items = items ?? throw new ArgumentNullException(nameof(items));
    }

    /// <inheritdoc/>
    public override int Count => _items.Sum(x => x.Count());

    /// <inheritdoc/>
    public override IEnumerator<T> GetEnumerator() => _items.SelectMany(x => x).GetEnumerator();
}
