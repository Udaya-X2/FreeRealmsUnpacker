namespace AssetIO;

/// <summary>
/// The exception that is thrown when an asset has invalid data.
/// </summary>
/// <param name="message">The message that describes the error.</param>
/// <param name="size">The size of the invalid data, in bytes.</param>
internal class InvalidAssetException(string message, int size) : Exception(message)
{
    /// <summary>
    /// The size of the invalid data, in bytes.
    /// </summary>
    public int Size { get; } = size;
}
