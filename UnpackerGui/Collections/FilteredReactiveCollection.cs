using DynamicData.Binding;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Reactive.Linq;
using UnpackerGui.ViewModels;

namespace UnpackerGui.Collections;

/// <summary>
/// Represents a read-only wrapper for a sequence of <typeparamref name="T"/>
/// filtered by a condition, with suspensible change notifications.
/// </summary>
/// <typeparam name="T">The type of the item.</typeparam>
public class FilteredReactiveCollection<T> : ReadOnlyReactiveCollection<T>
{
    private readonly IEnumerable<T> _items;
    private readonly FilterViewModel<T> _filter;

    private Lazy<int> _count;
    private Lazy<int> _unfilteredCount;

    /// <summary>
    /// Initializes a new instance of the <see cref="FilteredReactiveCollection{T}"/>
    /// class from the specified sequence and filter.
    /// </summary>
    public FilteredReactiveCollection(IEnumerable<T> items, FilterViewModel<T> filter)
    {
        _items = items ?? throw new ArgumentNullException(nameof(items));
        _filter = filter ?? throw new ArgumentNullException(nameof(filter));
        _count = new Lazy<int>(GetFilteredCount);
        _unfilteredCount = new Lazy<int>(_items.Count);

        // Refresh the collection when the underlying collection changes.
        (items as INotifyCollectionChanged)?.ObserveCollectionChanges()
                                            .Subscribe(_ => Refresh());

        // Refresh the collection when the filter changes.
        this.WhenAnyValue(x => x._filter.IsMatch)
            .Subscribe(_ => Refresh());
    }

    /// <inheritdoc/>
    public override int Count => _count.Value;

    /// <summary>
    /// Gets the number of elements in the underlying collection.
    /// </summary>
    /// <returns>The number of elements in the underlying collection.</returns>
    public int UnfilteredCount => _unfilteredCount.Value;

    /// <inheritdoc/>
    public override IEnumerator<T> GetEnumerator() => _filter.IsAlwaysMatch
        ? _items.GetEnumerator()
        : _items.Where(_filter.IsMatch).GetEnumerator();

    /// <inheritdoc/>
    public override void Refresh()
    {
        if (NotificationsEnabled)
        {
            _count = new Lazy<int>(GetFilteredCount);
            _unfilteredCount = new Lazy<int>(_items.Count);
            OnPropertyChanged(EventArgsCache.UnfilteredCountPropertyChanged);
        }

        base.Refresh();
    }

    /// <summary>
    /// Returns the number of items that match the filter.
    /// </summary>
    /// <returns>The number of items that match the filter.</returns>
    private int GetFilteredCount() => _filter.IsAlwaysMatch ? _unfilteredCount.Value : _items.Count(_filter.IsMatch);
}
