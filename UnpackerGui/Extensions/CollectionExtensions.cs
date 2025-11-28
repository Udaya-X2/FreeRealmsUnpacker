using DynamicData;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using UnpackerGui.Collections;
using UnpackerGui.ViewModels;

namespace UnpackerGui.Extensions;

/// <summary>
/// Provides extension methods for specialized collections.
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

    /// <summary>
    /// Creates a <see cref="FilteredReactiveCollection{T}"/> from a collection with change notifications.
    /// </summary>
    /// <returns>A new instance of <see cref="FilteredReactiveCollection{T}"/>.</returns>
    public static FilteredReactiveCollection<T> Filter<T>(
        this ReadOnlyReactiveCollection<T> items,
        Func<T, bool> predicate)
        => new(items, new FilterViewModel<T>(predicate));

    /// <summary>
    /// Creates a <see cref="FilteredReactiveCollection{T}"/> from a collection with change notifications.
    /// </summary>
    /// <returns>A new instance of <see cref="FilteredReactiveCollection{T}"/>.</returns>
    public static FilteredReactiveCollection<T> Filter<T>(this ReadOnlyReactiveCollection<T> items)
        => new(items, new FilterViewModel<T>());

    /// <summary>
    /// Creates a <see cref="FilteredReactiveCollection{T}"/> from a collection with change notifications.
    /// </summary>
    /// <returns>A new instance of <see cref="FilteredReactiveCollection{T}"/>.</returns>
    public static FilteredReactiveCollection<TResult> Filter<TParam, TResult>(
        this TParam items,
        Func<TResult, bool> predicate)
        where TParam : IEnumerable<TResult>, INotifyCollectionChanged, INotifyPropertyChanged
        => new(items, new FilterViewModel<TResult>(predicate));

    /// <summary>
    /// Removes the first element of the list that matches the specified predicate.
    /// </summary>
    public static void Remove<T>(this ISourceList<T> items, Predicate<T> match) where T : notnull
        => items.Edit(x => x.Remove(match));

    /// <summary>
    /// Removes the first element of the list that matches the specified predicate.
    /// </summary>
    public static void Remove<T>(this IList<T> list, Predicate<T> match)
    {
        for (int i = 0; i < list.Count; i++)
        {
            if (match(list[i]))
            {
                list.RemoveAt(i);
                break;
            }
        }
    }
}
