using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using UnpackerGui.Collections;
using UnpackerGui.ViewModels;

namespace UnpackerGui.Extensions;

/// <summary>
/// Provides extension methods for converting collections with change notifications.
/// </summary>
public static class CollectionExtensions
{
    /// <summary>
    /// Creates a <see cref="GroupedReactiveCollection{T}"/> from a collection with change notifications.
    /// </summary>
    /// <returns>A new instance of <see cref="GroupedReactiveCollection{T}"/>.</returns>
    public static GroupedReactiveCollection<TItem> Flatten<TCollection, TItem>(
        this TCollection items)
        where TCollection : IEnumerable<IEnumerable<TItem>>, INotifyCollectionChanged, INotifyPropertyChanged
        => new(items);

    /// <summary>
    /// Creates a <see cref="FilteredReactiveCollection{T}"/> from a collection with change notifications.
    /// </summary>
    /// <returns>A new instance of <see cref="FilteredReactiveCollection{T}"/>.</returns>
    public static FilteredReactiveCollection<TResult> Filter<TParam, TResult>(
        this TParam items,
        FilterViewModel<TResult> filter)
        where TParam : IEnumerable<TResult>, INotifyCollectionChanged, INotifyPropertyChanged
        => new(items, filter);
}
