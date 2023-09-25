using System.Buffers.Binary;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;

namespace AssetReader
{
    /// <summary>
    /// Reads primitive data types as binary values in a specific encoding and endianness.
    /// </summary>
    internal class EndianBinaryReader : IDisposable
    {
        private const int MinBufferSize = 16;
        private const int MaxCharBytesSize = 128;

        private readonly Stream _stream;
        private readonly Decoder _decoder;
        private readonly int _maxCharsSize; // From MaxCharBytesSize & Encoding.
        private readonly byte[] _buffer;
        private readonly bool _2BytesPerChar; // Performance optimization for Read() w/ Unicode.
        private readonly bool _leaveOpen;
        private readonly bool _isLittleEndian;

        private byte[]? _charBytes;
        private char[]? _charBuffer;
        private bool _disposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="EndianBinaryReader"/> class
        /// based on the specified stream, using little endian order and UTF-8 encoding.
        /// </summary>
        /// <param name="input">The input stream.</param>
        /// <exception cref="ArgumentException">The stream does not support reading or is already closed.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="input"/> is <see langword="null"/>.</exception>
        public EndianBinaryReader(Stream input)
            : this(input, Endian.Little)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="EndianBinaryReader"/> class
        /// based on the specified stream and endianness, using UTF-8 encoding.
        /// </summary>
        /// <param name="input">The input stream.</param>
        /// <param name="endianness">Specifies the order in which bytes are read.</param>
        /// <exception cref="ArgumentException">The stream does not support reading or is already closed.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="input"/> is <see langword="null"/>.</exception>
        /// <exception cref="InvalidEnumArgumentException">
        /// <paramref name="endianness"/> is not a valid <see cref="Endian"/> value.
        /// </exception>
        public EndianBinaryReader(Stream input, Endian endianness)
            : this(input, endianness, Encoding.UTF8)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="EndianBinaryReader"/> class based
        /// on the specified stream and character encoding, using little endian order.
        /// </summary>
        /// <param name="input">The input stream.</param>
        /// <param name="encoding">The character encoding to use.</param>
        /// <exception cref="ArgumentException">The stream does not support reading or is already closed.</exception>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="encoding"/> or <paramref name="input"/> is <see langword="null"/>.
        /// </exception>
        public EndianBinaryReader(Stream input, Encoding encoding)
            : this(input, Endian.Little, encoding)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="EndianBinaryReader"/> class
        /// based on the specified stream, endianness, and character encoding.
        /// </summary>
        /// <param name="input">The input stream.</param>
        /// <param name="endianness">Specifies the order in which bytes are read.</param>
        /// <param name="encoding">The character encoding to use.</param>
        /// <exception cref="ArgumentException">The stream does not support reading or is already closed.</exception>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="encoding"/> or <paramref name="input"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="InvalidEnumArgumentException">
        /// <paramref name="endianness"/> is not a valid <see cref="Endian"/> value.
        /// </exception>
        public EndianBinaryReader(Stream input, Endian endianness, Encoding encoding)
            : this(input, endianness, encoding, false)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="EndianBinaryReader"/> class based on the specified
        /// stream, endianness, and character encoding, and optionally leaves the stream open.
        /// </summary>
        /// <param name="input">The input stream.</param>
        /// <param name="endianness">Specifies the order in which bytes are read.</param>
        /// <param name="encoding">The character encoding to use.</param>
        /// <param name="leaveOpen">
        /// <see langword="true"/> to leave the stream open after the <see cref="EndianBinaryReader"/>
        /// has been disposed; otherwise, <see langword="false"/>.
        /// </param>
        /// <exception cref="ArgumentException">The stream does not support reading or is already closed.</exception>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="encoding"/> or <paramref name="input"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="InvalidEnumArgumentException">
        /// <paramref name="endianness"/> is not a valid <see cref="Endian"/> value.
        /// </exception>
        public EndianBinaryReader(Stream input, Endian endianness, Encoding encoding, bool leaveOpen)
        {
            if (input == null)
            {
                throw new ArgumentNullException(nameof(input));
            }
            if (encoding == null)
            {
                throw new ArgumentNullException(nameof(encoding));
            }
            if (!input.CanRead)
            {
                throw new ArgumentException(SR.Argument_StreamNotReadable, nameof(input));
            }

            _stream = input;
            _decoder = encoding.GetDecoder();
            _maxCharsSize = encoding.GetMaxCharCount(MaxCharBytesSize);
            _buffer = new byte[Math.Max(MinBufferSize, encoding.GetMaxByteCount(1))];

            // For Encodings that always use 2 bytes per char (or more),
            // special case them here to make Read() & Peek() faster.
            _2BytesPerChar = encoding is UnicodeEncoding;
            _leaveOpen = leaveOpen;
            _isLittleEndian = endianness switch
            {
                Endian.Little => true,
                Endian.Big => false,
                _ => throw new InvalidEnumArgumentException(nameof(endianness), (int)endianness, endianness.GetType()),
            };
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
        /// Releases all resources used by the current instance of the <see cref="EndianBinaryReader"/> class.
        /// </summary>
        public void Dispose()
        {
            if (!_disposed)
            {
                if (!_leaveOpen)
                {
                    _stream.Close();
                }

                _disposed = true;
                GC.SuppressFinalize(this);
            }
        }

        /// <summary>
        /// Throws an exception if this object has been disposed.
        /// </summary>
        /// <exception cref="ObjectDisposedException"></exception>
        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(EndianBinaryReader));
            }
        }

        /// <summary>
        /// Returns the next available character and does not advance the byte or character position.
        /// </summary>
        /// <returns>
        /// The next available character, or -1 if no more characters
        /// are available or the stream does not support seeking.
        /// </returns>
        /// <exception cref="ArgumentException">
        /// The current character cannot be decoded into the internal character
        /// buffer by using the <see cref="Encoding"/> selected for the stream.
        /// </exception>
        /// <exception cref="IOException">An I/O error occurred.</exception>
        /// <exception cref="ObjectDisposedException">The stream is closed.</exception>
        public int PeekChar()
        {
            ThrowIfDisposed();

            if (!_stream.CanSeek)
            {
                return -1;
            }

            long origPos = _stream.Position;
            int ch = Read();
            _stream.Position = origPos;
            return ch;
        }

        /// <summary>
        /// Reads characters from the underlying stream and advances the current
        /// position of the stream in accordance with the <see langword="Encoding"/>
        /// used and the specific character being read from the stream.
        /// </summary>
        /// <returns>
        /// The next character from the input stream, or -1 if no characters are currently available.
        /// </returns>
        /// <exception cref="IOException">An I/O error occurred.</exception>
        /// <exception cref="ObjectDisposedException">The stream is closed.</exception>
        public int Read()
        {
            ThrowIfDisposed();

            _charBytes ??= new byte[MaxCharBytesSize];
            Span<char> singleChar = stackalloc char[1];
            long posSav = _stream.CanSeek ? _stream.Position : 0L;
            int charsRead = 0;

            do
            {
                // We really want to know what the minimum number of bytes
                // per char is for our encoding. Otherwise for UnicodeEncoding
                // we'd have to do ~1+log(n) reads to read n characters.
                //
                // Assume 1 byte can be 1 char unless _2BytesPerChar is true.
                int numBytes = _2BytesPerChar ? 2 : 1;
                int r = _stream.ReadByte();
                _charBytes[0] = (byte)r;

                if (r == -1)
                {
                    return -1;
                }
                if (numBytes == 2)
                {
                    r = _stream.ReadByte();
                    _charBytes[1] = (byte)r;

                    if (r == -1)
                    {
                        numBytes = 1;
                    }
                }

                try
                {
                    ReadOnlySpan<byte> bytes = new(_charBytes, 0, numBytes);
                    charsRead = _decoder.GetChars(bytes, singleChar, flush: false);
                }
                catch
                {
                    // Handle surrogate char, if possible.
                    if (_stream.CanSeek)
                    {
                        _stream.Seek(posSav - _stream.Position, SeekOrigin.Current);
                    }

                    throw;
                }
            } while (charsRead == 0);

            return singleChar[0];
        }

        /// <summary>
        /// Reads the next byte from the current stream and advances the current position of the stream by one byte.
        /// </summary>
        /// <returns>The next byte read from the current stream.</returns>
        /// <exception cref="EndOfStreamException">The end of the stream is reached.</exception>
        /// <exception cref="IOException">An I/O error occurred.</exception>
        /// <exception cref="ObjectDisposedException">The stream is closed.</exception>
        public byte ReadByte() => InternalReadByte();

        /// <inheritdoc cref="ReadByte"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private byte InternalReadByte()
        {
            ThrowIfDisposed();
            int b = _stream.ReadByte();
            return b == -1 ? throw new EndOfStreamException(SR.EndOfStream_Stream) : (byte)b;
        }

        /// <summary>
        /// Reads a signed byte from this stream and advances the current position of the stream by one byte.
        /// </summary>
        /// <returns>A signed byte read from the current stream.</returns>
        /// <exception cref="EndOfStreamException">The end of the stream is reached.</exception>
        /// <exception cref="IOException">An I/O error occurred.</exception>
        /// <exception cref="ObjectDisposedException">The stream is closed.</exception>
        public sbyte ReadSByte() => (sbyte)InternalReadByte();

        /// <summary>
        /// Reads a <see langword="Boolean"/> value from the current stream
        /// and advances the current position of the stream by one byte.
        /// </summary>
        /// <returns><see langword="true"/> if the byte is nonzero; otherwise, <see langword="false"/>.</returns>
        /// <exception cref="EndOfStreamException">The end of the stream is reached.</exception>
        /// <exception cref="IOException">An I/O error occurred.</exception>
        /// <exception cref="ObjectDisposedException">The stream is closed.</exception>
        public bool ReadBoolean() => InternalReadByte() != 0;

        /// <summary>
        /// Reads the next character from the current stream and advances the current
        /// position of the stream in accordance with the <see langword="Encoding"/>
        /// used and the specific character being read from the stream.
        /// </summary>
        /// <returns>A character read from the current stream.</returns>
        /// <exception cref="ArgumentException">A surrogate character was read.</exception>
        /// <exception cref="EndOfStreamException">The end of the stream is reached.</exception>
        /// <exception cref="IOException">An I/O error occurred.</exception>
        /// <exception cref="ObjectDisposedException">The stream is closed.</exception>
        public char ReadChar()
        {
            int value = Read();
            return value == -1 ? throw new EndOfStreamException(SR.EndOfStream_Stream) : (char)value;
        }

        /// <summary>
        /// Reads a 2-byte signed integer from the current stream and
        /// advances the current position of the stream by two bytes.
        /// </summary>
        /// <returns>A 2-byte signed integer read from the current stream.</returns>
        /// <exception cref="EndOfStreamException">The end of the stream is reached.</exception>
        /// <exception cref="IOException">An I/O error occurred.</exception>
        /// <exception cref="ObjectDisposedException">The stream is closed.</exception>
        public short ReadInt16() => _isLittleEndian
            ? BinaryPrimitives.ReadInt16LittleEndian(InternalRead(sizeof(short)))
            : BinaryPrimitives.ReadInt16BigEndian(InternalRead(sizeof(short)));

        /// <summary>
        /// Reads a 2-byte unsigned integer from the current stream
        /// and advances the position of the stream by two bytes.
        /// </summary>
        /// <returns>A 2-byte unsigned integer read from this stream.</returns>
        /// <exception cref="EndOfStreamException">The end of the stream is reached.</exception>
        /// <exception cref="IOException">An I/O error occurred.</exception>
        /// <exception cref="ObjectDisposedException">The stream is closed.</exception>
        public ushort ReadUInt16() => _isLittleEndian
            ? BinaryPrimitives.ReadUInt16LittleEndian(InternalRead(sizeof(short)))
            : BinaryPrimitives.ReadUInt16BigEndian(InternalRead(sizeof(short)));

        /// <summary>
        /// Reads a 4-byte signed integer from the current stream and
        /// advances the current position of the stream by four bytes.
        /// </summary>
        /// <returns>A 4-byte signed integer read from the current stream.</returns>
        /// <exception cref="EndOfStreamException">The end of the stream is reached.</exception>
        /// <exception cref="IOException">An I/O error occurred.</exception>
        /// <exception cref="ObjectDisposedException">The stream is closed.</exception>
        public int ReadInt32() => _isLittleEndian
            ? BinaryPrimitives.ReadInt32LittleEndian(InternalRead(sizeof(int)))
            : BinaryPrimitives.ReadInt32BigEndian(InternalRead(sizeof(int)));

        /// <summary>
        /// Reads a 4-byte unsigned integer from the current stream
        /// and advances the position of the stream by four bytes.
        /// </summary>
        /// <returns>A 4-byte unsigned integer read from this stream.</returns>
        /// <exception cref="EndOfStreamException">The end of the stream is reached.</exception>
        /// <exception cref="IOException">An I/O error occurred.</exception>
        /// <exception cref="ObjectDisposedException">The stream is closed.</exception>
        public uint ReadUInt32() => _isLittleEndian
            ? BinaryPrimitives.ReadUInt32LittleEndian(InternalRead(sizeof(int)))
            : BinaryPrimitives.ReadUInt32BigEndian(InternalRead(sizeof(int)));

        /// <summary>
        /// Reads an 8-byte signed integer from the current stream and
        /// advances the current position of the stream by eight bytes.
        /// </summary>
        /// <returns>An 8-byte signed integer read from the current stream.</returns>
        /// <exception cref="EndOfStreamException">The end of the stream is reached.</exception>
        /// <exception cref="IOException">An I/O error occurred.</exception>
        /// <exception cref="ObjectDisposedException">The stream is closed.</exception>
        public long ReadInt64() => _isLittleEndian
            ? BinaryPrimitives.ReadInt64LittleEndian(InternalRead(sizeof(long)))
            : BinaryPrimitives.ReadInt64BigEndian(InternalRead(sizeof(long)));

        /// <summary>
        /// Reads an 8-byte unsigned integer from the current stream
        /// and advances the position of the stream by eight bytes.
        /// </summary>
        /// <returns>An 8-byte unsigned integer read from this stream.</returns>
        /// <exception cref="EndOfStreamException">The end of the stream is reached.</exception>
        /// <exception cref="IOException">An I/O error occurred.</exception>
        /// <exception cref="ObjectDisposedException">The stream is closed.</exception>
        public ulong ReadUInt64() => _isLittleEndian
            ? BinaryPrimitives.ReadUInt64LittleEndian(InternalRead(sizeof(long)))
            : BinaryPrimitives.ReadUInt64BigEndian(InternalRead(sizeof(long)));

        /// <summary>
        /// Reads a 2-byte floating point value from the current stream
        /// and advances the current position of the stream by two bytes.
        /// </summary>
        /// <returns>A 2-byte floating point value read from the current stream.</returns>
        /// <exception cref="EndOfStreamException">The end of the stream is reached.</exception>
        /// <exception cref="IOException">An I/O error occurred.</exception>
        /// <exception cref="ObjectDisposedException">The stream is closed.</exception>
        public Half ReadHalf() => _isLittleEndian
            ? BitConverter.Int16BitsToHalf(BinaryPrimitives.ReadInt16LittleEndian(InternalRead(sizeof(short))))
            : BitConverter.Int16BitsToHalf(BinaryPrimitives.ReadInt16BigEndian(InternalRead(sizeof(short))));

        /// <summary>
        /// Reads a 4-byte floating point value from the current stream
        /// and advances the current position of the stream by four bytes.
        /// </summary>
        /// <returns>A 4-byte floating point value read from the current stream.</returns>
        /// <exception cref="EndOfStreamException">The end of the stream is reached.</exception>
        /// <exception cref="IOException">An I/O error occurred.</exception>
        /// <exception cref="ObjectDisposedException">The stream is closed.</exception>
        public float ReadSingle() => _isLittleEndian
            ? BitConverter.Int32BitsToSingle(BinaryPrimitives.ReadInt32LittleEndian(InternalRead(sizeof(int))))
            : BitConverter.Int32BitsToSingle(BinaryPrimitives.ReadInt32BigEndian(InternalRead(sizeof(int))));

        /// <summary>
        /// Reads an 8-byte floating point value from the current stream
        /// and advances the current position of the stream by eight bytes.
        /// </summary>
        /// <returns>An 8-byte floating point value read from the current stream.</returns>
        /// <exception cref="EndOfStreamException">The end of the stream is reached.</exception>
        /// <exception cref="IOException">An I/O error occurred.</exception>
        /// <exception cref="ObjectDisposedException">The stream is closed.</exception>
        public double ReadDouble() => _isLittleEndian
            ? BitConverter.Int64BitsToDouble(BinaryPrimitives.ReadInt64LittleEndian(InternalRead(sizeof(long))))
            : BitConverter.Int64BitsToDouble(BinaryPrimitives.ReadInt64BigEndian(InternalRead(sizeof(long))));

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
                return _isLittleEndian
                    ? ReadDecimalLittleEndian(InternalRead(sizeof(decimal)))
                    : ReadDecimalBigEndian(InternalRead(sizeof(decimal)));
            }
            catch (ArgumentException ex)
            {
                // ReadDecimal cannot leak out ArgumentException.
                throw new IOException(SR.IO_InvalidDecimalBits, ex);
            }
        }

        /// <summary>
        /// Reads a decimal from the beginning of a read-only span of bytes, as little endian.
        /// </summary>
        /// <returns>The little endian value.</returns>
        /// <exception cref="ArgumentException">
        /// The representation of the decimal value in <paramref name="span"/> is not valid.
        /// </exception>
        private decimal ReadDecimalLittleEndian(ReadOnlySpan<byte> span) => new(stackalloc[]
        {
            BinaryPrimitives.ReadInt32LittleEndian(span),
            BinaryPrimitives.ReadInt32LittleEndian(span[4..]),
            BinaryPrimitives.ReadInt32LittleEndian(span[8..]),
            BinaryPrimitives.ReadInt32LittleEndian(span[12..])
        });

        /// <summary>
        /// Reads a decimal from the beginning of a read-only span of bytes, as big endian.
        /// </summary>
        /// <returns>The big endian value.</returns>
        /// <exception cref="ArgumentException">
        /// The representation of the decimal value in <paramref name="span"/> is not valid.
        /// </exception>
        private decimal ReadDecimalBigEndian(ReadOnlySpan<byte> span) => new(stackalloc[]
        {
            BinaryPrimitives.ReadInt32BigEndian(span[12..]),
            BinaryPrimitives.ReadInt32BigEndian(span[8..]),
            BinaryPrimitives.ReadInt32BigEndian(span[4..]),
            BinaryPrimitives.ReadInt32BigEndian(span)
        });

        /// <summary>
        /// Reads a string from the current stream. The string is prefixed
        /// with the length, encoded as an integer seven bits at a time.
        /// </summary>
        /// <exception cref="EndOfStreamException">The end of the stream is reached.</exception>
        /// <exception cref="FormatException">The stream is corrupted.</exception>
        /// <exception cref="IOException">An I/O error occurred.</exception>
        /// <exception cref="ObjectDisposedException">The stream is closed.</exception>
        public string ReadString() => ReadString(Read7BitEncodedInt());

        /// <summary>
        /// Reads a string with the specified byte length from the current stream.
        /// </summary>
        /// <param name="stringLength">The length of the string, in bytes.</param>
        /// <returns>The string being read.</returns>
        /// <exception cref="EndOfStreamException">The end of the stream is reached.</exception>
        /// <exception cref="IOException">An I/O error occurred.</exception>
        /// <exception cref="ObjectDisposedException">The stream is closed.</exception>
        public string ReadString(int stringLength)
        {
            ThrowIfDisposed();

            if (stringLength < 0)
            {
                throw new IOException(string.Format(SR.IO_InvalidStringLen, stringLength));
            }
            if (stringLength == 0)
            {
                return string.Empty;
            }

            int currPos = 0;
            int n;
            int readLength;
            int charsRead;
            _charBytes ??= new byte[MaxCharBytesSize];
            _charBuffer ??= new char[_maxCharsSize];
            StringBuilder? sb = null;

            do
            {
                readLength = Math.Min(MaxCharBytesSize, stringLength - currPos);
                n = _stream.Read(_charBytes, 0, readLength);

                if (n == 0)
                {
                    throw new EndOfStreamException(SR.EndOfStream_Stream);
                }

                charsRead = _decoder.GetChars(_charBytes, 0, n, _charBuffer, 0);

                if (currPos == 0 && n == stringLength)
                {
                    return new string(_charBuffer, 0, charsRead);
                }

                // Since we could be reading from an untrusted data source, limit the initial size of the
                // StringBuilder instance we're about to get or create. It'll expand automatically as needed.
                const int maxBuilderSize = 360;
                sb ??= new(Math.Min(stringLength, maxBuilderSize)); // Actual string length in chars may be smaller.
                sb.Append(_charBuffer, 0, charsRead);
                currPos += n;
            } while (currPos < stringLength);

            return sb.ToString();
        }

        /// <summary>
        /// Reads the specified number of characters from the stream,
        /// starting from a specified point in the character array.
        /// </summary>
        /// <param name="buffer">The buffer to read data into.</param>
        /// <param name="index">The starting point in the buffer at which to begin reading into the buffer.</param>
        /// <param name="count">The number of characters to read.</param>
        /// <returns>
        /// The total number of characters read into the buffer. This might be less than
        /// the number of characters requested if that many characters are not currently
        /// available, or it might be zero if the end of the stream is reached.
        /// </returns>
        /// <exception cref="ArgumentException">
        /// The buffer length minus <paramref name="index"/> is less than <paramref name="count"/>.
        /// <br/>-or-<br/>
        /// The number of decoded characters to read is greater than <paramref name="count"/>.
        /// This can happen if a Unicode decoder returns fallback characters or a surrogate pair.
        /// </exception>
        /// <exception cref="ArgumentNullException"><paramref name="buffer"/> is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="index"/> or <paramref name="count"/> is negative.
        /// </exception>
        /// <exception cref="IOException">An I/O error occurred.</exception>
        /// <exception cref="ObjectDisposedException">The stream is closed.</exception>
        public int Read(char[] buffer, int index, int count)
        {
            if (buffer == null)
            {
                throw new ArgumentNullException(nameof(buffer), SR.ArgumentNull_Buffer);
            }
            if (index < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(index), SR.ArgumentOutOfRange_NeedNonNegNum);
            }
            if (count < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(count), SR.ArgumentOutOfRange_NeedNonNegNum);
            }
            if (buffer.Length - index < count)
            {
                throw new ArgumentException(SR.Argument_InvalidOffLen);
            }

            ThrowIfDisposed();
            return InternalReadChars(new Span<char>(buffer, index, count));
        }

        /// <summary>
        /// Reads, from the current stream, the same number of characters as the length of the provided
        /// buffer, writes them in the provided buffer, and advances the current position in accordance
        /// with the <see langword="Encoding"/> used and the specific character being read from the stream.
        /// </summary>
        /// <param name="buffer">
        /// A span of characters. When this method returns, the contents of this
        /// region are replaced by the characters read from the current source.
        /// </param>
        /// <returns>
        /// The total number of characters read into the buffer. This might be less than
        /// the number of characters requested if that many characters are not currently
        /// available, or it might be zero if the end of the stream is reached.
        /// </returns>
        /// <exception cref="IOException">An I/O error occurred.</exception>
        /// <exception cref="ObjectDisposedException">The stream is closed.</exception>
        public int Read(Span<char> buffer)
        {
            ThrowIfDisposed();
            return InternalReadChars(buffer);
        }

        /// <inheritdoc cref="Read(Span{char})"/>
        private int InternalReadChars(Span<char> buffer)
        {
            int totalCharsRead = 0;

            while (!buffer.IsEmpty)
            {
                int numBytes = buffer.Length;

                // We really want to know what the minimum number of bytes
                // per char is for our encoding. Otherwise for UnicodeEncoding
                // we'd have to do ~1+log(n) reads to read n characters.
                if (_2BytesPerChar)
                {
                    numBytes <<= 1;
                }

                // We do not want to read even a single byte more than necessary.
                //
                // Subtract pending bytes that the decoder may be holding onto. This assumes that
                // each decoded char corresponds to one or more bytes. Note that custom encodings
                // or encodings with a custom replacement sequence may violate this assumption.
                if (numBytes > 1)
                {
                    // Assume that the decoder has a pending state.
                    numBytes--;

                    // The worst case is charsRemaining = 2 and UTF32Decoder holding
                    // onto 3 pending bytes. We need to read just one byte in this case.
                    if (_2BytesPerChar && numBytes > 2)
                        numBytes -= 2;
                }

                ReadOnlySpan<byte> byteBuffer;
                _charBytes ??= new byte[MaxCharBytesSize];

                if (numBytes > MaxCharBytesSize)
                {
                    numBytes = MaxCharBytesSize;
                }

                numBytes = _stream.Read(_charBytes, 0, numBytes);
                byteBuffer = new ReadOnlySpan<byte>(_charBytes, 0, numBytes);

                if (byteBuffer.IsEmpty)
                {
                    break;
                }

                int charsRead = _decoder.GetChars(byteBuffer, buffer, flush: false);
                buffer = buffer[charsRead..];
                totalCharsRead += charsRead;
            }

            // We may have read fewer than the number of characters requested if end of stream reached
            // or if the encoding makes the char count too big for the buffer (e.g. fallback sequence).
            return totalCharsRead;
        }

        /// <summary>
        /// Reads the specified number of characters from the current stream, returns the data
        /// in a character array, and advances the current position in accordance with the
        /// <see langword="Encoding"/> used and the specific character being read from the stream.
        /// </summary>
        /// <param name="count">The number of characters to read.</param>
        /// <returns>
        /// A character array containing data read from the underlying stream. This might be
        /// less than the number of characters requested if the end of the stream is reached.
        /// </returns>
        /// <exception cref="ArgumentException">
        /// The number of decoded characters to read is greater than <paramref name="count"/>.
        /// This can happen if a Unicode decoder returns fallback characters or a surrogate pair.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="count"/> is negative.</exception>
        /// <exception cref="IOException">An I/O error occurred.</exception>
        /// <exception cref="ObjectDisposedException">The stream is closed.</exception>
        public char[] ReadChars(int count)
        {
            if (count < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(count), SR.ArgumentOutOfRange_NeedNonNegNum);
            }

            ThrowIfDisposed();

            if (count == 0)
            {
                return Array.Empty<char>();
            }

            char[] chars = new char[count];
            int n = InternalReadChars(new Span<char>(chars));

            if (n != count)
            {
                char[] copy = new char[n];
                Buffer.BlockCopy(chars, 0, copy, 0, sizeof(char) * n);
                chars = copy;
            }

            return chars;
        }

        /// <summary>
        /// Reads the specified number of bytes from the stream, starting from a specified point in the byte array.
        /// </summary>
        /// <param name="buffer">The buffer to read data into.</param>
        /// <param name="index">The starting point in the buffer at which to begin reading into the buffer.</param>
        /// <param name="count">The number of bytes to read.</param>
        /// <returns>
        /// The number of bytes read into <paramref name="buffer"/>. This might be less than the number of bytes
        /// requested if that many bytes are not available, or it might be zero if the end of the stream is reached.
        /// </returns>
        /// <exception cref="ArgumentException">
        /// The buffer length minus <paramref name="index"/> is less than <paramref name="count"/>.
        /// </exception>
        /// <exception cref="ArgumentNullException"><paramref name="buffer"/> is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="index"/> or <paramref name="count"/> is negative.
        /// </exception>
        /// <exception cref="IOException">An I/O error occurred.</exception>
        /// <exception cref="ObjectDisposedException">The stream is closed.</exception>
        public int Read(byte[] buffer, int index, int count)
        {
            if (buffer == null)
            {
                throw new ArgumentNullException(nameof(buffer), SR.ArgumentNull_Buffer);
            }
            if (index < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(index), SR.ArgumentOutOfRange_NeedNonNegNum);
            }
            if (count < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(count), SR.ArgumentOutOfRange_NeedNonNegNum);
            }
            if (buffer.Length - index < count)
            {
                throw new ArgumentException(SR.Argument_InvalidOffLen);
            }

            ThrowIfDisposed();
            return _stream.Read(buffer, index, count);
        }

        /// <summary>
        /// Reads a sequence of bytes from the current stream and advances
        /// the position within the stream by the number of bytes read.
        /// </summary>
        /// <param name="buffer">
        /// A region of memory. When this method returns, the contents of this
        /// region are replaced by the bytes read from the current source.
        /// </param>
        /// <returns>
        /// The total number of bytes read into the buffer. This can be less than
        /// the number of bytes allocated in the buffer if that many bytes are not
        /// currently available, or zero (0) if the end of the stream has been reached.
        /// </returns>
        /// <exception cref="IOException">An I/O error occurred.</exception>
        /// <exception cref="ObjectDisposedException">The stream is closed.</exception>
        public int Read(Span<byte> buffer)
        {
            ThrowIfDisposed();
            return _stream.Read(buffer);
        }

        /// <summary>
        /// Reads the specified number of bytes from the current stream into a
        /// byte array and advances the current position by that number of bytes.
        /// </summary>
        /// <param name="count">The number of bytes to read.</param>
        /// <returns>
        /// A byte array containing data read from the underlying stream. This might be
        /// less than the number of bytes requested if the end of the stream is reached.
        /// </returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="count"/> is negative.</exception>
        /// <exception cref="IOException">An I/O error occurred.</exception>
        /// <exception cref="ObjectDisposedException">The stream is closed.</exception>
        public byte[] ReadBytes(int count)
        {
            if (count < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(count), SR.ArgumentOutOfRange_NeedNonNegNum);
            }

            ThrowIfDisposed();

            if (count == 0)
            {
                return Array.Empty<byte>();
            }

            byte[] result = new byte[count];
            int numRead = 0;

            do
            {
                int n = _stream.Read(result, numRead, count);

                if (n == 0)
                {
                    break;
                }

                numRead += n;
                count -= n;
            } while (count > 0);

            if (numRead != result.Length)
            {
                // Trim array. This should happen on EOF & possibly net streams.
                byte[] copy = new byte[numRead];
                Buffer.BlockCopy(result, 0, copy, 0, numRead);
                result = copy;
            }

            return result;
        }

        /// <summary>
        /// Reads the specified number of bytes into the internal buffer.
        /// </summary>
        /// <returns>A read-only span of the internal buffer with the bytes read.</returns>
        /// <exception cref="EndOfStreamException">The end of the stream is reached.</exception>
        /// <exception cref="IOException">An I/O error occurred.</exception>
        /// <exception cref="ObjectDisposedException">The stream is closed.</exception>
        private ReadOnlySpan<byte> InternalRead(int numBytes)
        {
            ThrowIfDisposed();
            Debug.Assert(numBytes is > 0 and < 16, "Number of bytes must be in the range [1, 16].");

            int bytesRead = 0;

            do
            {
                int n = _stream.Read(_buffer, bytesRead, numBytes - bytesRead);

                if (n == 0)
                {
                    throw new EndOfStreamException(SR.EndOfStream_Stream);
                }

                bytesRead += n;
            } while (bytesRead < numBytes);

            return _buffer;
        }

        /// <summary>
        /// Reads in a 32-bit integer in compressed format.
        /// </summary>
        /// <returns>A 32-bit integer in compressed format.</returns>
        /// <exception cref="EndOfStreamException">The end of the stream is reached.</exception>
        /// <exception cref="FormatException">The stream is corrupted.</exception>
        /// <exception cref="IOException">An I/O error occurred.</exception>
        /// <exception cref="ObjectDisposedException">The stream is closed.</exception>
        public int Read7BitEncodedInt()
        {
            // Unlike writing, we can't delegate to the 64-bit read on
            // 64-bit platforms. The reason for this is that we want to
            // stop consuming bytes if we encounter an integer overflow.
            uint result = 0;
            byte byteReadJustNow;

            // Read the integer 7 bits at a time. The high bit of
            // the byte when on means to continue reading more bytes.
            //
            // There are two failure cases: we've read more than 5 bytes,
            // or the fifth byte is about to cause integer overflow.
            // This means that we can read the first 4 bytes without
            // worrying about integer overflow.
            const int MaxBytesWithoutOverflow = 4;

            for (int shift = 0; shift < MaxBytesWithoutOverflow * 7; shift += 7)
            {
                // ReadByte handles end of stream cases for us.
                byteReadJustNow = ReadByte();
                result |= (byteReadJustNow & 0x7Fu) << shift;

                if (byteReadJustNow <= 0x7Fu)
                {
                    return (int)result;
                }
            }

            // Read the 5th byte. Since we already read 28 bits,
            // the value of this byte must fit within 4 bits (32 - 28),
            // and it must not have the high bit set.
            byteReadJustNow = ReadByte();

            if (byteReadJustNow > 0b_1111u)
            {
                throw new FormatException(SR.Format_Bad7BitInt);
            }

            result |= (uint)byteReadJustNow << (MaxBytesWithoutOverflow * 7);
            return (int)result;
        }

        /// <summary>
        /// Reads a number 7 bits at a time.
        /// </summary>
        /// <returns>The number that is read from this binary reader instance.</returns>
        /// <exception cref="EndOfStreamException">The end of the stream is reached.</exception>
        /// <exception cref="FormatException">The stream is corrupted.</exception>
        /// <exception cref="IOException">An I/O error occurred.</exception>
        /// <exception cref="ObjectDisposedException">The stream is closed.</exception>
        public long Read7BitEncodedInt64()
        {
            ulong result = 0;
            byte byteReadJustNow;

            // Read the integer 7 bits at a time. The high bit of
            // the byte when on means to continue reading more bytes.
            //
            // There are two failure cases: we've read more than 10 bytes,
            // or the tenth byte is about to cause integer overflow.
            // This means that we can read the first 9 bytes without
            // worrying about integer overflow.
            const int MaxBytesWithoutOverflow = 9;

            for (int shift = 0; shift < MaxBytesWithoutOverflow * 7; shift += 7)
            {
                // ReadByte handles end of stream cases for us.
                byteReadJustNow = ReadByte();
                result |= (byteReadJustNow & 0x7Ful) << shift;

                if (byteReadJustNow <= 0x7Fu)
                {
                    return (long)result;
                }
            }

            // Read the 10th byte. Since we already read 63 bits,
            // the value of this byte must fit within 1 bit (64 - 63),
            // and it must not have the high bit set.
            byteReadJustNow = ReadByte();

            if (byteReadJustNow > 0b_1u)
            {
                throw new FormatException(SR.Format_Bad7BitInt);
            }

            result |= (ulong)byteReadJustNow << (MaxBytesWithoutOverflow * 7);
            return (long)result;
        }
    }
}
