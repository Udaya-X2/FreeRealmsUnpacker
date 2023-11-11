using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using UnpackerGui.ViewModels;

namespace UnpackerGui.Collections;

/// <summary>
/// Represents a read-only wrapper for a sequence of <typeparamref name="T"/>
/// filtered by a search setting, with suspensible change notifications.
/// </summary>
/// <typeparam name="T">The type of the item.</typeparam>
public class FilteredReactiveCollection<T> : ReadOnlyReactiveCollection<T>
{
    public SearchOptionsViewModel<T> SearchOptions { get; }

    private readonly IEnumerable<T> _items;

    /// <summary>
    /// Initializes a new instance of the <see cref="FilteredReactiveCollection{T}"/> class from the specified sequence.
    /// </summary>
    public FilteredReactiveCollection(IEnumerable<T> items, SearchOptionsViewModel<T> searchOptions)
    {
        _items = items ?? throw new ArgumentNullException(nameof(items));
        SearchOptions = searchOptions ?? throw new ArgumentNullException(nameof(searchOptions));

        // Refresh the collection when the search options change.
        this.WhenAnyValue(x => x.SearchOptions.IsMatch)
            .Subscribe(_ => Refresh());
    }

    public override int Count => SearchOptions.IsAlwaysMatch
        ? _items.Count()
        : _items.Count(SearchOptions.IsMatch);

    public override IEnumerator<T> GetEnumerator() => SearchOptions.IsAlwaysMatch
        ? _items.GetEnumerator()
        : _items.Where(SearchOptions.IsMatch)
                .GetEnumerator();
}
