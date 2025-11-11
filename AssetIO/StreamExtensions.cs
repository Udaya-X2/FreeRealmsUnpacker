namespace AssetIO;

/// <summary>
/// Provides extension methods for the <see cref="Stream"/> class.
/// </summary>
internal static class StreamExtensions
{
    /// <summary>
    /// Attempts to read <paramref name="count"/> number of bytes from the current
    /// stream and advance the position within the stream, blocking if necessary.
    /// </summary>
    /// <param name="stream">The stream to read.</param>
    /// <param name="buffer">
    /// An array of bytes. When this method returns, the buffer contains the specified byte
    /// array with the values between <paramref name="offset"/> and (<paramref name="offset"/>
    /// + <paramref name="count"/> - 1) replaced by the bytes read from the current stream.
    /// </param>
    /// <param name="offset">
    /// The byte offset in <paramref name="buffer"/> at which to begin storing the data read from the current stream.
    /// </param>
    /// <param name="count">The number of bytes to be read from the current stream.</param>
    /// <returns>
    /// The total number of bytes read into the buffer. This will be less
    /// than <paramref name="count"/> when the end of the stream is reached.
    /// </returns>
    /// <exception cref="ArgumentException"/>
    /// <exception cref="ArgumentNullException"/>
    /// <exception cref="ArgumentOutOfRangeException"/>
    /// <exception cref="IOException"/>
    /// <exception cref="NotSupportedException"/>
    /// <exception cref="ObjectDisposedException"/>
    internal static int ReadBlocking(this Stream stream, byte[] buffer, int offset, int count)
    {
        int totalRead = 0;

        do
        {
            int read = stream.Read(buffer, offset + totalRead, count - totalRead);

            if (read == 0) break;

            totalRead += read;
        }
        while (totalRead != count);

        return totalRead;
    }

    /// <returns>
    /// <see langword="true"/> if <paramref name="count"/> bytes were
    /// read from the stream; otherwise, <see langword="false"/>.
    /// </returns>
    /// <inheritdoc cref="ReadBlocking(Stream, byte[], int, int)"/>
    internal static bool TryReadExactly(this Stream stream, byte[] buffer, int offset, int count)
        => ReadBlocking(stream, buffer, offset, count) == count;
}
