using System;
using System.Collections.Generic;
using System.Linq;

namespace UnpackerGui.Collections;

/// <summary>
/// Represents a read-only wrapper for a sequence of <typeparamref name="T"/>
/// filtered by a search setting, with suspensible change notifications.
/// </summary>
/// <typeparam name="T">The type of the item.</typeparam>
public class FilteredReactiveCollection<T> : ReadOnlyReactiveCollection<T>
{
    private readonly IEnumerable<T> _items;
    private readonly SearchOptions<T> _searchOptions;

    /// <summary>
    /// Initializes a new instance of the <see cref="FilteredReactiveCollection{T}"/> class from the specified sequence.
    /// </summary>
    public FilteredReactiveCollection(IEnumerable<T> items, SearchOptions<T> searchOptions)
    {
        _items = items ?? throw new ArgumentNullException(nameof(items));
        _searchOptions = searchOptions ?? throw new ArgumentNullException(nameof(searchOptions));
    }

    public override int Count => _searchOptions.IsAlwaysMatch
        ? _items.Count()
        : _items.Where(_searchOptions.IsMatch).Count();

    public override IEnumerator<T> GetEnumerator() => _items.Where(_searchOptions.IsMatch)
                                                            .GetEnumerator();
}
