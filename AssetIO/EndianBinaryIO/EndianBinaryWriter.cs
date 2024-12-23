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
}
