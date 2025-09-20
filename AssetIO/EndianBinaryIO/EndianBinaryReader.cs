using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace AssetIO.EndianBinaryIO;

/// <summary>
/// Reads primitive data types as binary values in a specific endianness.
/// </summary>
internal sealed class EndianBinaryReader
{
    private readonly Stream _stream;
    private readonly bool _isLittleEndian;
    private readonly bool _reverseEndianness;
    private readonly byte[] _buffer;
    private readonly char[] _charBuffer;

    /// <summary>
    /// Initializes a new instance of the <see cref="EndianBinaryReader"/>
    /// class based on the specified stream and endianness.
    /// </summary>
    /// <param name="input">The input stream.</param>
    /// <param name="endianness">Specifies the order in which bytes are read.</param>
    /// <param name="maxStringLength">A non-negative integer value indicating the max string length.</param>
    /// <exception cref="ArgumentException">The stream does not support reading or is already closed.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="input"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="endianness"/> is not a valid <see cref="Endian"/>
    /// value or <paramref name="maxStringLength"/> is negative.
    /// </exception>
    public EndianBinaryReader(Stream input, Endian endianness, int maxStringLength = 0)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentOutOfRangeException.ThrowIfNegative(maxStringLength);
        if (!input.CanRead) ThrowHelper.ThrowArgument_StreamNotReadable();

        _isLittleEndian = endianness switch
        {
            Endian.Little => true,
            Endian.Big => false,
            _ => ThrowHelper.ThrowArgumentOutOfRange_Enum<bool>(nameof(endianness)),
        };
        _reverseEndianness = _isLittleEndian != BitConverter.IsLittleEndian;
        _stream = input;
        _buffer = new byte[Math.Max(maxStringLength, sizeof(decimal))];
        _charBuffer = maxStringLength != 0 ? new char[maxStringLength] : [];
    }

    /// <summary>
    /// Returns the underlying stream.
    /// </summary>
    public Stream BaseStream => _stream;

    /// <summary>
    /// Gets an enum value indicating the order in which bytes are read.
    /// </summary>
    public Endian Endianness => _isLittleEndian ? Endian.Little : Endian.Big;

    /// <summary>
    /// Reads the next byte from the current stream and advances the current position of the stream by one byte.
    /// </summary>
    /// <returns>The next byte read from the current stream.</returns>
    /// <exception cref="EndOfStreamException">The end of the stream is reached.</exception>
    /// <exception cref="IOException">An I/O error occurred.</exception>
    /// <exception cref="ObjectDisposedException">The stream is closed.</exception>
    public byte ReadByte() => _stream.ReadByte() is int b and not -1
        ? (byte)b : ThrowHelper.ThrowEndOfStream_Stream<byte>();

    /// <summary>
    /// Reads a signed byte from this stream and advances the current position of the stream by one byte.
    /// </summary>
    /// <returns>A signed byte read from the current stream.</returns>
    /// <exception cref="EndOfStreamException">The end of the stream is reached.</exception>
    /// <exception cref="IOException">An I/O error occurred.</exception>
    /// <exception cref="ObjectDisposedException">The stream is closed.</exception>
    public sbyte ReadSByte() => _stream.ReadByte() is int b and not -1
        ? (sbyte)b : ThrowHelper.ThrowEndOfStream_Stream<sbyte>();

    /// <summary>
    /// Reads a <see langword="Boolean"/> value from the current stream
    /// and advances the current position of the stream by one byte.
    /// </summary>
    /// <returns><see langword="true"/> if the byte is nonzero; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="EndOfStreamException">The end of the stream is reached.</exception>
    /// <exception cref="IOException">An I/O error occurred.</exception>
    /// <exception cref="ObjectDisposedException">The stream is closed.</exception>
    public bool ReadBoolean() => _stream.ReadByte() is int b and not -1
        ? b != 0 : ThrowHelper.ThrowEndOfStream_Stream<bool>();

    /// <summary>
    /// Reads a 2-byte signed integer from the current stream and
    /// advances the current position of the stream by two bytes.
    /// </summary>
    /// <returns>A 2-byte signed integer read from the current stream.</returns>
    /// <exception cref="EndOfStreamException">The end of the stream is reached.</exception>
    /// <exception cref="IOException">An I/O error occurred.</exception>
    /// <exception cref="ObjectDisposedException">The stream is closed.</exception>
    public short ReadInt16() => _reverseEndianness
        ? BinaryPrimitives.ReverseEndianness(Unsafe.ReadUnaligned<short>(in InternalRead(sizeof(short))))
        : Unsafe.ReadUnaligned<short>(in InternalRead(sizeof(short)));

    /// <summary>
    /// Reads a 2-byte unsigned integer from the current stream
    /// and advances the position of the stream by two bytes.
    /// </summary>
    /// <returns>A 2-byte unsigned integer read from this stream.</returns>
    /// <exception cref="EndOfStreamException">The end of the stream is reached.</exception>
    /// <exception cref="IOException">An I/O error occurred.</exception>
    /// <exception cref="ObjectDisposedException">The stream is closed.</exception>
    public ushort ReadUInt16() => _reverseEndianness
        ? BinaryPrimitives.ReverseEndianness(Unsafe.ReadUnaligned<ushort>(in InternalRead(sizeof(ushort))))
        : Unsafe.ReadUnaligned<ushort>(in InternalRead(sizeof(ushort)));

    /// <summary>
    /// Reads a 4-byte signed integer from the current stream and
    /// advances the current position of the stream by four bytes.
    /// </summary>
    /// <returns>A 4-byte signed integer read from the current stream.</returns>
    /// <exception cref="EndOfStreamException">The end of the stream is reached.</exception>
    /// <exception cref="IOException">An I/O error occurred.</exception>
    /// <exception cref="ObjectDisposedException">The stream is closed.</exception>
    public int ReadInt32() => _reverseEndianness
        ? BinaryPrimitives.ReverseEndianness(Unsafe.ReadUnaligned<int>(in InternalRead(sizeof(int))))
        : Unsafe.ReadUnaligned<int>(in InternalRead(sizeof(int)));

    /// <summary>
    /// Reads a 4-byte unsigned integer from the current stream
    /// and advances the position of the stream by four bytes.
    /// </summary>
    /// <returns>A 4-byte unsigned integer read from this stream.</returns>
    /// <exception cref="EndOfStreamException">The end of the stream is reached.</exception>
    /// <exception cref="IOException">An I/O error occurred.</exception>
    /// <exception cref="ObjectDisposedException">The stream is closed.</exception>
    public uint ReadUInt32() => _reverseEndianness
        ? BinaryPrimitives.ReverseEndianness(Unsafe.ReadUnaligned<uint>(in InternalRead(sizeof(uint))))
        : Unsafe.ReadUnaligned<uint>(in InternalRead(sizeof(uint)));

    /// <summary>
    /// Reads an 8-byte signed integer from the current stream and
    /// advances the current position of the stream by eight bytes.
    /// </summary>
    /// <returns>An 8-byte signed integer read from the current stream.</returns>
    /// <exception cref="EndOfStreamException">The end of the stream is reached.</exception>
    /// <exception cref="IOException">An I/O error occurred.</exception>
    /// <exception cref="ObjectDisposedException">The stream is closed.</exception>
    public long ReadInt64() => _reverseEndianness
        ? BinaryPrimitives.ReverseEndianness(Unsafe.ReadUnaligned<long>(in InternalRead(sizeof(long))))
        : Unsafe.ReadUnaligned<long>(in InternalRead(sizeof(long)));

    /// <summary>
    /// Reads an 8-byte unsigned integer from the current stream
    /// and advances the position of the stream by eight bytes.
    /// </summary>
    /// <returns>An 8-byte unsigned integer read from this stream.</returns>
    /// <exception cref="EndOfStreamException">The end of the stream is reached.</exception>
    /// <exception cref="IOException">An I/O error occurred.</exception>
    /// <exception cref="ObjectDisposedException">The stream is closed.</exception>
    public ulong ReadUInt64() => _reverseEndianness
        ? BinaryPrimitives.ReverseEndianness(Unsafe.ReadUnaligned<ulong>(in InternalRead(sizeof(ulong))))
        : Unsafe.ReadUnaligned<ulong>(in InternalRead(sizeof(ulong)));

    /// <summary>
    /// Reads a 2-byte floating point value from the current stream
    /// and advances the current position of the stream by two bytes.
    /// </summary>
    /// <returns>A 2-byte floating point value read from the current stream.</returns>
    /// <exception cref="EndOfStreamException">The end of the stream is reached.</exception>
    /// <exception cref="IOException">An I/O error occurred.</exception>
    /// <exception cref="ObjectDisposedException">The stream is closed.</exception>
    public Half ReadHalf() => _reverseEndianness
        ? Unsafe.BitCast<short, Half>(
            BinaryPrimitives.ReverseEndianness(Unsafe.ReadUnaligned<short>(in InternalRead(sizeof(short)))))
        : Unsafe.ReadUnaligned<Half>(in InternalRead(sizeof(short)));

    /// <summary>
    /// Reads a 4-byte floating point value from the current stream
    /// and advances the current position of the stream by four bytes.
    /// </summary>
    /// <returns>A 4-byte floating point value read from the current stream.</returns>
    /// <exception cref="EndOfStreamException">The end of the stream is reached.</exception>
    /// <exception cref="IOException">An I/O error occurred.</exception>
    /// <exception cref="ObjectDisposedException">The stream is closed.</exception>
    public float ReadSingle() => _reverseEndianness
        ? Unsafe.BitCast<int, float>(
            BinaryPrimitives.ReverseEndianness(Unsafe.ReadUnaligned<int>(in InternalRead(sizeof(int)))))
        : Unsafe.ReadUnaligned<float>(in InternalRead(sizeof(float)));

    /// <summary>
    /// Reads an 8-byte floating point value from the current stream
    /// and advances the current position of the stream by eight bytes.
    /// </summary>
    /// <returns>An 8-byte floating point value read from the current stream.</returns>
    /// <exception cref="EndOfStreamException">The end of the stream is reached.</exception>
    /// <exception cref="IOException">An I/O error occurred.</exception>
    /// <exception cref="ObjectDisposedException">The stream is closed.</exception>
    public double ReadDouble() => _reverseEndianness
        ? Unsafe.BitCast<long, double>(
            BinaryPrimitives.ReverseEndianness(Unsafe.ReadUnaligned<long>(in InternalRead(sizeof(long)))))
        : Unsafe.ReadUnaligned<double>(in InternalRead(sizeof(double)));

    /// <summary>
    /// Reads a decimal value from the current stream and advances
    /// the current position of the stream by sixteen bytes.
    /// </summary>
    /// <returns>A decimal value read from the current stream.</returns>
    /// <exception cref="EndOfStreamException">The end of the stream is reached.</exception>
    /// <exception cref="IOException">An I/O error occurred.</exception>
    /// <exception cref="ObjectDisposedException">The stream is closed.</exception>
    public decimal ReadDecimal()
    {
        try
        {
            Span<int> bits = MemoryMarshal.CreateSpan(ref Unsafe.As<byte, int>(ref InternalRead(sizeof(decimal))), 4);

            if (_reverseEndianness)
            {
                (bits[0], bits[3]) = (BinaryPrimitives.ReverseEndianness(bits[3]),
                                      BinaryPrimitives.ReverseEndianness(bits[0]));
                (bits[1], bits[2]) = (BinaryPrimitives.ReverseEndianness(bits[2]),
                                      BinaryPrimitives.ReverseEndianness(bits[1]));
            }

            return new decimal(bits);
        }
        catch (ArgumentException ex)
        {
            // ReadDecimal cannot leak out ArgumentException.
            return ThrowHelper.ThrowIO_InvalidDecimalBits<decimal>(ex);
        }
    }

    /// <summary>
    /// Reads a string with the specified byte length from the current stream.
    /// </summary>
    /// <param name="length">The length of the string, in bytes.</param>
    /// <returns>The string being read.</returns>
    /// <exception cref="ArgumentException"><paramref name="length"/> exceeds the maximum string length.</exception>
    /// <exception cref="EndOfStreamException">The end of the stream is reached.</exception>
    /// <exception cref="IOException">An I/O error occurred.</exception>
    /// <exception cref="ObjectDisposedException">The stream is closed.</exception>
    public string ReadString(int length)
    {
        if ((uint)length > (uint)_charBuffer.Length) ThrowHelper.ThrowArgument_InvalidStringLen(length);
        if (length == 0) return string.Empty;

        ReadOnlySpan<byte> bytes = MemoryMarshal.CreateReadOnlySpan(ref InternalRead(length), length);
        Span<char> chars = MemoryMarshal.CreateSpan(ref MemoryMarshal.GetArrayDataReference(_charBuffer), length);
        return new string(chars[..Encoding.UTF8.GetChars(bytes, chars)]);
    }

    /// <summary>
    /// Reads the specified number of bytes into the internal buffer.
    /// </summary>
    /// <param name="numBytes">The number of bytes to read.</param>
    /// <returns>A reference to the first element in the buffer.</returns>
    /// <exception cref="EndOfStreamException">The end of the stream is reached.</exception>
    /// <exception cref="IOException">An I/O error occurred.</exception>
    /// <exception cref="ObjectDisposedException">The stream is closed.</exception>
    private ref byte InternalRead(int numBytes)
    {
        int totalRead = 0;

        do
        {
            int read = _stream.Read(_buffer, totalRead, numBytes - totalRead);

            if (read == 0) ThrowHelper.ThrowEndOfStream_Stream();

            totalRead += read;
        }
        while (totalRead < numBytes);

        return ref MemoryMarshal.GetArrayDataReference(_buffer);
    }
}
