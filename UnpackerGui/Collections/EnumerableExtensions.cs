using System;
using System.Collections.Generic;

namespace UnpackerGui.Collections;

public static class EnumerableExtensions
{
    /// <summary>
    /// Performs the specified action on each element of the <see cref="IEnumerable{T}"/>.
    /// </summary>
    /// <typeparam name="T">The type of the item.</typeparam>
    /// <param name="source">The enumerable source.</param>
    /// <param name="action">
    /// The <see cref="Action{T}"/> delegate to perform on each item of the <see cref="IEnumerable{T}"/>.
    /// </param>
    /// <exception cref="ArgumentNullException"/>
    public static void ForEach<T>(this IEnumerable<T> source, Action<T> action)
    {
        ArgumentNullException.ThrowIfNull(source, nameof(source));
        ArgumentNullException.ThrowIfNull(action, nameof(action));

        foreach (T item in source)
        {
            action(item);
        }
    }

    /// <summary>
    /// Filters <see langword="null"/> values from a sequence of values.
    /// </summary>
    /// <typeparam name="T">The type of the item.</typeparam>
    /// <param name="source">The enumerable source.</param>
    /// <returns>
    /// An <see cref="IEnumerable{T}"/> that contains elements from
    /// the input sequence that are not <see langword="null"/>.
    /// </returns>
    /// <exception cref="ArgumentNullException"/>
    public static IEnumerable<T> WhereNotNull<T>(this IEnumerable<T?> source)
    {
        ArgumentNullException.ThrowIfNull(source, nameof(source));

        foreach (T? item in source)
        {
            if (item != null)
            {
                yield return item;
            }
        }
    }
}
