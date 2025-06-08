namespace AssetIO;

internal static class EnumeratorExtensions
{
    /// <summary>
    /// Advances the enumerator to the next element of the collection and returns it.
    /// </summary>
    /// <remarks><inheritdoc cref="SafeMoveNext{T}(IEnumerator{T})"/></remarks>
    /// <returns>The next element of the collection.</returns>
    internal static T SafeGetNext<T>(this IEnumerator<T> enumerator)
    {
        enumerator.SafeMoveNext();
        return enumerator.Current;
    }

    /// <inheritdoc cref="System.Collections.IEnumerator.MoveNext"/>
    /// <remarks>
    /// If an error occurs, the exception is nested within a generic <see cref="Exception"/>
    /// to distinguish it from other runtime exceptions.
    /// </remarks>
    internal static bool SafeMoveNext<T>(this IEnumerator<T> enumerator)
    {
        try
        {
            return enumerator.MoveNext();
        }
        catch (Exception ex)
        {
            throw new Exception("Unable to advance enumerator to next element.", ex);
        }
    }
}
