using System.Buffers;
using System.Buffers.Binary;
using System.ComponentModel;
using System.Text;

namespace AssetIO.EndianBinaryIO;

/// <summary>
/// Writes primitive types in binary to a stream in a specific
/// endianness and supports writing strings in a specific encoding.
/// </summary>
internal class EndianBinaryWriter(Stream output, Endian endianness, Encoding encoding, bool leaveOpen)
    : BinaryWriter(output, encoding, leaveOpen)
{
    private const int MaxArrayPoolRentalSize = 64 * 1024; // try to keep rentals to a reasonable size
    private const int CodePageUTF8 = 65001;

    private readonly Encoding _encoding = encoding;
    private readonly bool _useFastUtf8 = encoding is { CodePage: CodePageUTF8, EncoderFallback.MaxCharCount: <= 1 };
    private readonly bool _isLittleEndian = endianness switch
    {
        Endian.Little => true,
        Endian.Big => false,
        _ => throw new InvalidEnumArgumentException(nameof(endianness), (int)endianness, endianness.GetType()),
    };

    /// <summary>
    /// Initializes a new instance of the <see cref="EndianBinaryWriter"/> class based
    /// on the specified stream, using little endian order and UTF-8 encoding.
    /// </summary>
    /// <inheritdoc cref="BinaryWriter(Stream)"/>
    public EndianBinaryWriter(Stream output)
        : this(output, Endian.Little)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="EndianBinaryWriter"/> class
    /// based on the specified stream and endianness, using UTF-8 encoding.
    /// </summary>
    /// <param name="output">The output stream.</param>
    /// <param name="endianness">Specifies the order in which bytes are read.</param>
    /// <exception cref="InvalidEnumArgumentException">
    /// <paramref name="endianness"/> is not a valid <see cref="Endian"/> value.
    /// </exception>
    /// <inheritdoc cref="BinaryWriter(Stream)"/>
    public EndianBinaryWriter(Stream output, Endian endianness)
        : this(output, endianness, Encoding.UTF8)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="EndianBinaryWriter"/> class based
    /// on the specified stream and character encoding, using little endian order.
    /// </summary>
    /// <inheritdoc cref="BinaryWriter(Stream, Encoding)"/>
    public EndianBinaryWriter(Stream output, Encoding encoding)
        : this(output, Endian.Little, encoding)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="EndianBinaryWriter"/> class
    /// based on the specified stream, endianness, and character encoding.
    /// </summary>
    /// <param name="output">The output stream.</param>
    /// <param name="endianness">Specifies the order in which bytes are read.</param>
    /// <param name="encoding">The character encoding to use.</param>
    /// <exception cref="InvalidEnumArgumentException">
    /// <paramref name="endianness"/> is not a valid <see cref="Endian"/> value.
    /// </exception>
    /// <inheritdoc cref="BinaryWriter(Stream, Encoding)"/>
    public EndianBinaryWriter(Stream output, Endian endianness, Encoding encoding)
        : this(output, endianness, encoding, false)
    {
    }

    /// <summary>
    /// Gets an enum value indicating the order in which bytes are written.
    /// </summary>
    public Endian Endianness => _isLittleEndian ? Endian.Little : Endian.Big;

    /// <inheritdoc/>
    public override void Write(short value)
    {
        Span<byte> buffer = stackalloc byte[sizeof(short)];

        if (_isLittleEndian) BinaryPrimitives.WriteInt16LittleEndian(buffer, value);
        else BinaryPrimitives.WriteInt16BigEndian(buffer, value);

        OutStream.Write(buffer);
    }

    /// <inheritdoc/>
    public override void Write(ushort value)
    {
        Span<byte> buffer = stackalloc byte[sizeof(ushort)];

        if (_isLittleEndian) BinaryPrimitives.WriteUInt16LittleEndian(buffer, value);
        else BinaryPrimitives.WriteUInt16BigEndian(buffer, value);

        OutStream.Write(buffer);
    }

    /// <inheritdoc/>
    public override void Write(int value)
    {
        Span<byte> buffer = stackalloc byte[sizeof(int)];

        if (_isLittleEndian) BinaryPrimitives.WriteInt32LittleEndian(buffer, value);
        else BinaryPrimitives.WriteInt32BigEndian(buffer, value);

        OutStream.Write(buffer);
    }

    /// <inheritdoc/>
    public override void Write(uint value)
    {
        Span<byte> buffer = stackalloc byte[sizeof(uint)];

        if (_isLittleEndian) BinaryPrimitives.WriteUInt32LittleEndian(buffer, value);
        else BinaryPrimitives.WriteUInt32BigEndian(buffer, value);

        OutStream.Write(buffer);
    }

    /// <inheritdoc/>
    public override void Write(long value)
    {
        Span<byte> buffer = stackalloc byte[sizeof(long)];

        if (_isLittleEndian) BinaryPrimitives.WriteInt64LittleEndian(buffer, value);
        else BinaryPrimitives.WriteInt64BigEndian(buffer, value);

        OutStream.Write(buffer);
    }

    /// <inheritdoc/>
    public override void Write(ulong value)
    {
        Span<byte> buffer = stackalloc byte[sizeof(ulong)];

        if (_isLittleEndian) BinaryPrimitives.WriteUInt64LittleEndian(buffer, value);
        else BinaryPrimitives.WriteUInt64BigEndian(buffer, value);

        OutStream.Write(buffer);
    }

    /// <inheritdoc/>
    public override void Write(Half value)
    {
        Span<byte> buffer = stackalloc byte[sizeof(ushort) /* sizeof(Half)) */];

        if (_isLittleEndian) BinaryPrimitives.WriteHalfLittleEndian(buffer, value);
        else BinaryPrimitives.WriteHalfBigEndian(buffer, value);

        OutStream.Write(buffer);
    }

    /// <inheritdoc/>
    public override void Write(float value)
    {
        Span<byte> buffer = stackalloc byte[sizeof(float)];

        if (_isLittleEndian) BinaryPrimitives.WriteSingleLittleEndian(buffer, value);
        else BinaryPrimitives.WriteSingleBigEndian(buffer, value);

        OutStream.Write(buffer);
    }

    /// <inheritdoc/>
    public override void Write(double value)
    {
        Span<byte> buffer = stackalloc byte[sizeof(double)];

        if (_isLittleEndian) BinaryPrimitives.WriteDoubleLittleEndian(buffer, value);
        else BinaryPrimitives.WriteDoubleBigEndian(buffer, value);

        OutStream.Write(buffer);
    }

    /// <inheritdoc/>
    public override void Write(string value)
    {
        ArgumentNullException.ThrowIfNull(value);

        // Common: UTF-8, small string, avoid 2-pass calculation
        // Less common: UTF-8, large string, avoid 2-pass calculation
        // Uncommon: excessively large string or not UTF-8
        if (_useFastUtf8)
        {
            if (value.Length <= 128 / 3)
            {
                // Max expansion: each char -> 3 bytes, so 128 bytes max of data
                Span<byte> buffer = stackalloc byte[128];
                int actualByteCount = _encoding.GetBytes(value, buffer);
                Write(actualByteCount);
                OutStream.Write(buffer[..actualByteCount]);
                return;
            }
            else if (value.Length <= MaxArrayPoolRentalSize / 3)
            {
                byte[] rented = ArrayPool<byte>.Shared.Rent(value.Length * 3); // max expansion: each char -> 3 bytes
                int actualByteCount = _encoding.GetBytes(value, rented);
                Write(actualByteCount);
                OutStream.Write(rented, 0, actualByteCount);
                ArrayPool<byte>.Shared.Return(rented);
                return;
            }
        }

        // Slow path: not fast UTF-8, or data is very large. We need to fall back
        // to a 2-pass mechanism so that we're not renting absurdly large arrays.
        int actualBytecount = _encoding.GetByteCount(value);
        Write(actualBytecount);
        WriteCharsCommonWithoutLengthPrefix(value, useThisWriteOverride: false);
    }

    /// <inheritdoc/>
    public override void Write(ReadOnlySpan<byte> buffer)
    {
        OutStream.Write(buffer);
    }

    private void WriteCharsCommonWithoutLengthPrefix(ReadOnlySpan<char> chars, bool useThisWriteOverride)
    {
        // If our input is truly enormous, the call to GetMaxByteCount might overflow,
        // which we want to avoid. Theoretically, any Encoding could expand from chars -> bytes
        // at an enormous ratio and cause us problems anyway given small inputs, but this is so
        // unrealistic that we needn't worry about it.
        byte[] rented;

        if (chars.Length <= MaxArrayPoolRentalSize)
        {
            // GetByteCount may walk the buffer contents, resulting in 2 passes over the data.
            // We prefer GetMaxByteCount because it's a constant-time operation.
            int maxByteCount = _encoding.GetMaxByteCount(chars.Length);

            if (maxByteCount <= MaxArrayPoolRentalSize)
            {
                rented = ArrayPool<byte>.Shared.Rent(maxByteCount);
                int actualByteCount = _encoding.GetBytes(chars, rented);
                WriteToOutStream(rented, 0, actualByteCount, useThisWriteOverride);
                ArrayPool<byte>.Shared.Return(rented);
                return;
            }
        }

        // We're dealing with an enormous amount of data, so acquire an Encoder.
        // It should be rare that callers pass sufficiently large inputs to hit
        // this code path, and the cost of the operation is dominated by the transcoding
        // step anyway, so it's ok for us to take the allocation here.
        rented = ArrayPool<byte>.Shared.Rent(MaxArrayPoolRentalSize);
        Encoder encoder = _encoding.GetEncoder();
        bool completed;

        do
        {
            encoder.Convert(chars, rented, flush: true, out int charsConsumed, out int bytesWritten, out completed);

            if (bytesWritten != 0)
            {
                WriteToOutStream(rented, 0, bytesWritten, useThisWriteOverride);
            }

            chars = chars[charsConsumed..];
        }
        while (!completed);

        ArrayPool<byte>.Shared.Return(rented);

        void WriteToOutStream(byte[] buffer, int offset, int count, bool useThisWriteOverride)
        {
            if (useThisWriteOverride)
            {
                Write(buffer, offset, count); // bounce through this.Write(...) overridden logic
            }
            else
            {
                OutStream.Write(buffer, offset, count); // ignore this.Write(...) override, go straight to inner stream
            }
        }
    }
}
