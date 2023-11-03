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
    public static void ForEach<T>(this IEnumerable<T> source, Action<T> action)
    {
        if (source == null) throw new ArgumentNullException(nameof(source));
        
        foreach (T item in source)
        {
            action(item);
        }
    }
}
