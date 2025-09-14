using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace AssetIO.EndianBinaryIO;

/// <summary>
/// Writes primitive types in binary to a stream in a specific endianness.
/// </summary>
internal sealed class EndianBinaryWriter
{
    private readonly Stream _stream;
    private readonly bool _isLittleEndian;
    private readonly bool _reverseEndianness;
    private readonly byte[] _buffer;

    /// <summary>
    /// Initializes a new instance of the <see cref="EndianBinaryWriter"/>
    /// class based on the specified stream and endianness.
    /// </summary>
    /// <param name="output">The output stream.</param>
    /// <param name="endianness">Specifies the order in which bytes are written.</param>
    /// <exception cref="ArgumentException">The stream does not support reading or is already closed.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="output"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="endianness"/> is not a valid <see cref="Endian"/> value.
    /// </exception>
    public EndianBinaryWriter(Stream output, Endian endianness)
    {
        ArgumentNullException.ThrowIfNull(output);
        if (!output.CanWrite) ThrowHelper.ThrowArgument_StreamNotWritable();

        _isLittleEndian = endianness switch
        {
            Endian.Little => true,
            Endian.Big => false,
            _ => ThrowHelper.ThrowArgumentOutOfRange_Enum<bool>(nameof(endianness)),
        };
        _reverseEndianness = _isLittleEndian != BitConverter.IsLittleEndian;
        _stream = output;
        _buffer = new byte[sizeof(decimal)];
    }

    /// <summary>
    /// Returns the underlying stream.
    /// </summary>
    public Stream BaseStream => _stream;

    /// <summary>
    /// Gets an enum value indicating the order in which bytes are written.
    /// </summary>
    public Endian Endianness => _isLittleEndian ? Endian.Little : Endian.Big;

    /// <summary>
    /// Writes an unsigned byte to the current stream and advances the stream position by one byte.
    /// </summary>
    /// <param name="value">The unsigned byte to write.</param>
    /// <exception cref="IOException">An I/O error occurs.</exception>
    /// <exception cref="ObjectDisposedException">The stream is closed.</exception>
    public void Write(byte value) => _stream.WriteByte(value);

    /// <summary>
    /// Writes a signed byte to the current stream and advances the stream position by one byte.
    /// </summary>
    /// <param name="value">The signed byte to write.</param>
    /// <exception cref="IOException">An I/O error occurs.</exception>
    /// <exception cref="ObjectDisposedException">The stream is closed.</exception>
    public void Write(sbyte value) => _stream.WriteByte((byte)value);

    /// <summary>
    /// Writes a one-byte <see langword="Boolean"/> value to the current stream, with
    /// 0 representing <see langword="false"/> and 1 representing <see langword="true"/>.
    /// </summary>
    /// <param name="value">The <see langword="Boolean"/> value to write (0 or 1).</param>
    /// <exception cref="IOException">An I/O error occurs.</exception>
    /// <exception cref="ObjectDisposedException">The stream is closed.</exception>
    public void Write(bool value) => _stream.WriteByte((byte)(value ? 1 : 0));

    /// <summary>
    /// Writes a two-byte signed integer to the current stream and advances the stream position by two bytes.
    /// </summary>
    /// <param name="value">The two-byte signed integer to write.</param>
    /// <exception cref="IOException">An I/O error occurs.</exception>
    /// <exception cref="ObjectDisposedException">The stream is closed.</exception>
    public void Write(short value)
    {
        if (_reverseEndianness) value = BinaryPrimitives.ReverseEndianness(value);
        Unsafe.WriteUnaligned(ref MemoryMarshal.GetArrayDataReference(_buffer), value);
        _stream.Write(_buffer, 0, sizeof(short));
    }

    /// <summary>
    /// Writes a two-byte unsigned integer to the current stream and advances the stream position by two bytes.
    /// </summary>
    /// <param name="value">The two-byte unsigned integer to write.</param>
    /// <exception cref="IOException">An I/O error occurs.</exception>
    /// <exception cref="ObjectDisposedException">The stream is closed.</exception>
    public void Write(ushort value)
    {
        if (_reverseEndianness) value = BinaryPrimitives.ReverseEndianness(value);
        Unsafe.WriteUnaligned(ref MemoryMarshal.GetArrayDataReference(_buffer), value);
        _stream.Write(_buffer, 0, sizeof(ushort));
    }

    /// <summary>
    /// Writes a four-byte signed integer to the current stream and advances the stream position by four bytes.
    /// </summary>
    /// <param name="value">The four-byte signed integer to write.</param>
    /// <exception cref="IOException">An I/O error occurs.</exception>
    /// <exception cref="ObjectDisposedException">The stream is closed.</exception>
    public void Write(int value)
    {
        if (_reverseEndianness) value = BinaryPrimitives.ReverseEndianness(value);
        Unsafe.WriteUnaligned(ref MemoryMarshal.GetArrayDataReference(_buffer), value);
        _stream.Write(_buffer, 0, sizeof(int));
    }

    /// <summary>
    /// Writes a four-byte unsigned integer to the current stream and advances the stream position by four bytes.
    /// </summary>
    /// <param name="value">The four-byte unsigned integer to write.</param>
    /// <exception cref="IOException">An I/O error occurs.</exception>
    /// <exception cref="ObjectDisposedException">The stream is closed.</exception>
    public void Write(uint value)
    {
        if (_reverseEndianness) value = BinaryPrimitives.ReverseEndianness(value);
        Unsafe.WriteUnaligned(ref MemoryMarshal.GetArrayDataReference(_buffer), value);
        _stream.Write(_buffer, 0, sizeof(uint));
    }

    /// <summary>
    /// Writes an eight-byte signed integer to the current stream and advances the stream position by eight bytes.
    /// </summary>
    /// <param name="value">The eight-byte signed integer to write.</param>
    /// <exception cref="IOException">An I/O error occurs.</exception>
    /// <exception cref="ObjectDisposedException">The stream is closed.</exception>
    public void Write(long value)
    {
        if (_reverseEndianness) value = BinaryPrimitives.ReverseEndianness(value);
        Unsafe.WriteUnaligned(ref MemoryMarshal.GetArrayDataReference(_buffer), value);
        _stream.Write(_buffer, 0, sizeof(long));
    }

    /// <summary>
    /// Writes an eight-byte unsigned integer to the current stream and advances the stream position by eight bytes.
    /// </summary>
    /// <param name="value">The eight-byte unsigned integer to write.</param>
    /// <exception cref="IOException">An I/O error occurs.</exception>
    /// <exception cref="ObjectDisposedException">The stream is closed.</exception>
    public void Write(ulong value)
    {
        if (_reverseEndianness) value = BinaryPrimitives.ReverseEndianness(value);
        Unsafe.WriteUnaligned(ref MemoryMarshal.GetArrayDataReference(_buffer), value);
        _stream.Write(_buffer, 0, sizeof(ulong));
    }

    /// <summary>
    /// Writes a two-byte floating-point value to the current stream and advances the stream position by two bytes.
    /// </summary>
    /// <param name="value">The two-byte floating-point value to write.</param>
    /// <exception cref="IOException">An I/O error occurs.</exception>
    /// <exception cref="ObjectDisposedException">The stream is closed.</exception>
    public void Write(Half value)
    {
        if (_reverseEndianness)
        {
            Unsafe.WriteUnaligned(ref MemoryMarshal.GetArrayDataReference(_buffer),
                                  BinaryPrimitives.ReverseEndianness(Unsafe.BitCast<Half, short>(value)));
        }
        else
        {
            Unsafe.WriteUnaligned(ref MemoryMarshal.GetArrayDataReference(_buffer), value);
        }

        _stream.Write(_buffer, 0, sizeof(short));
    }

    /// <summary>
    /// Writes a four-byte floating-point value to the current stream and advances the stream position by four bytes.
    /// </summary>
    /// <param name="value">The four-byte floating-point value to write.</param>
    /// <exception cref="IOException">An I/O error occurs.</exception>
    /// <exception cref="ObjectDisposedException">The stream is closed.</exception>
    public void Write(float value)
    {
        if (_reverseEndianness)
        {
            Unsafe.WriteUnaligned(ref MemoryMarshal.GetArrayDataReference(_buffer),
                                  BinaryPrimitives.ReverseEndianness(Unsafe.BitCast<float, int>(value)));
        }
        else
        {
            Unsafe.WriteUnaligned(ref MemoryMarshal.GetArrayDataReference(_buffer), value);
        }

        _stream.Write(_buffer, 0, sizeof(float));
    }

    /// <summary>
    /// Writes an eight-byte floating-point value to the current stream and advances the stream position by eight bytes.
    /// </summary>
    /// <param name="value">The eight-byte floating-point value to write.</param>
    /// <exception cref="IOException">An I/O error occurs.</exception>
    /// <exception cref="ObjectDisposedException">The stream is closed.</exception>
    public void Write(double value)
    {
        if (_reverseEndianness)
        {
            Unsafe.WriteUnaligned(ref MemoryMarshal.GetArrayDataReference(_buffer),
                                  BinaryPrimitives.ReverseEndianness(Unsafe.BitCast<double, long>(value)));
        }
        else
        {
            Unsafe.WriteUnaligned(ref MemoryMarshal.GetArrayDataReference(_buffer), value);
        }

        _stream.Write(_buffer, 0, sizeof(double));
    }

    /// <summary>
    /// Writes a decimal value to the current stream and advances the stream position by sixteen bytes.
    /// </summary>
    /// <param name="value">The decimal value to write.</param>
    /// <exception cref="IOException">An I/O error occurs.</exception>
    /// <exception cref="ObjectDisposedException">The stream is closed.</exception>
    public void Write(decimal value)
    {
        if (_reverseEndianness)
        {
            Span<int> bits = MemoryMarshal.CreateSpan(ref Unsafe.As<decimal, int>(ref value), 4);
            (bits[0], bits[3]) = (BinaryPrimitives.ReverseEndianness(bits[3]),
                                  BinaryPrimitives.ReverseEndianness(bits[0]));
            (bits[1], bits[2]) = (BinaryPrimitives.ReverseEndianness(bits[2]),
                                  BinaryPrimitives.ReverseEndianness(bits[1]));
        }

        Unsafe.WriteUnaligned(ref MemoryMarshal.GetArrayDataReference(_buffer), value);
        _stream.Write(_buffer, 0, sizeof(decimal));
    }
}
