using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace AssetIO;

/// <summary>
/// Helper methods to efficiently throw exceptions.
/// </summary>
[StackTraceHidden]
internal static class ThrowHelper
{
    /// <inheritdoc cref="SR.ArgumentOutOfRange_Enum"/>
    [DoesNotReturn]
    internal static void ThrowArgumentOutOfRange_Enum(string paramName)
        => throw new ArgumentOutOfRangeException(paramName, SR.ArgumentOutOfRange_Enum);

    /// <inheritdoc cref="SR.ArgumentOutOfRange_Enum"/>
    [DoesNotReturn]
    internal static T ThrowArgumentOutOfRange_Enum<T>(string paramName)
        => throw new ArgumentOutOfRangeException(paramName, SR.ArgumentOutOfRange_Enum);

    /// <inheritdoc cref="SR.Argument_CantInferAssetType"/>
    [DoesNotReturn]
    internal static void ThrowArgument_CantInferAssetType(string fileName)
        => throw new ArgumentException(string.Format(SR.Argument_CantInferAssetType, fileName));

    /// <inheritdoc cref="SR.Argument_InvalidAssetLen"/>
    [DoesNotReturn]
    internal static void ThrowArgument_InvalidAssetLen()
        => throw new ArgumentException(SR.Argument_InvalidAssetLen);

    /// <inheritdoc cref="SR.Argument_InvalidAssetName"/>
    [DoesNotReturn]
    internal static T ThrowArgument_InvalidAssetName<T>(string assetName, string fileName)
        => throw new ArgumentException(string.Format(SR.Argument_InvalidAssetName, assetName, fileName));

    /// <inheritdoc cref="SR.Argument_InvalidAssetType"/>
    [DoesNotReturn]
    internal static void ThrowArgument_InvalidAssetType(AssetType type)
        => throw new ArgumentException(string.Format(SR.Argument_InvalidAssetType, type));

    /// <inheritdoc cref="SR.Argument_InvalidAssetType"/>
    [DoesNotReturn]
    internal static T ThrowArgument_InvalidAssetType<T>(AssetType type)
        => throw new ArgumentException(string.Format(SR.Argument_InvalidAssetType, type));

    /// <inheritdoc cref="SR.Argument_InvalidOffLen"/>
    [DoesNotReturn]
    internal static void ThrowArgument_InvalidOffLen()
        => throw new ArgumentException(SR.Argument_InvalidOffLen);

    /// <inheritdoc cref="SR.Argument_InvalidSeekOrigin"/>
    [DoesNotReturn]
    internal static T ThrowArgument_InvalidSeekOrigin<T>(string paramName)
        => throw new ArgumentException(SR.Argument_InvalidSeekOrigin, paramName);

    /// <inheritdoc cref="SR.Argument_InvalidStringLen"/>
    [DoesNotReturn]
    internal static void ThrowArgument_InvalidStringLen(int numBytes)
        => throw new ArgumentException(string.Format(SR.Argument_InvalidStringLen, numBytes));

    /// <inheritdoc cref="SR.Argument_StreamNotReadable"/>
    [DoesNotReturn]
    internal static void ThrowArgument_StreamNotReadable()
        => throw new ArgumentException(SR.Argument_StreamNotReadable);

    /// <inheritdoc cref="SR.Argument_StreamNotWritable"/>
    [DoesNotReturn]
    internal static void ThrowArgument_StreamNotWritable()
        => throw new ArgumentException(SR.Argument_StreamNotWritable);

    /// <inheritdoc cref="SR.EndOfStream_AssetFile"/>
    [DoesNotReturn]
    internal static void ThrowEndOfStream_AssetFile(string fileName)
        => throw new EndOfStreamException(string.Format(SR.EndOfStream_AssetFile, fileName));

    /// <inheritdoc cref="SR.EndOfStream_AssetFile"/>
    [DoesNotReturn]
    internal static void ThrowEndOfStream_AssetFile(string fileName, Exception innerException)
        => throw new EndOfStreamException(string.Format(SR.EndOfStream_AssetFile, fileName), innerException);

    /// <inheritdoc cref="SR.EndOfStream_AssetFile"/>
    [DoesNotReturn]
    internal static T ThrowEndOfStream_AssetFile<T>(string fileName, Exception innerException)
        => throw new EndOfStreamException(string.Format(SR.EndOfStream_AssetFile, fileName), innerException);

    /// <inheritdoc cref="SR.EndOfStream_Stream"/>
    [DoesNotReturn]
    internal static void ThrowEndOfStream_Stream()
        => throw new EndOfStreamException(SR.EndOfStream_Stream);

    /// <inheritdoc cref="SR.EndOfStream_Stream"/>
    [DoesNotReturn]
    internal static T ThrowEndOfStream_Stream<T>()
        => throw new EndOfStreamException(SR.EndOfStream_Stream);

    /// <inheritdoc cref="SR.InvalidAsset_Name"/>
    [DoesNotReturn]
    internal static int ThrowInvalidAsset_Name(int value)
        => throw new InvalidAssetException(string.Format(SR.InvalidAsset_Name, value), sizeof(int));

    /// <inheritdoc cref="SR.InvalidAsset_Offset"/>
    [DoesNotReturn]
    internal static long ThrowInvalidAsset_Offset(long value)
        => throw new InvalidAssetException(string.Format(SR.InvalidAsset_Offset, value), sizeof(long));

    /// <inheritdoc cref="SR.InvalidOperation_NoAssetToFlush"/>
    [DoesNotReturn]
    internal static void ThrowInvalidOperation_NoAssetToFlush()
        => throw new InvalidOperationException(SR.InvalidOperation_NoAssetToFlush);

    /// <inheritdoc cref="SR.InvalidOperation_NoAssetToWrite"/>
    [DoesNotReturn]
    internal static void ThrowInvalidOperation_NoAssetToWrite()
        => throw new InvalidOperationException(SR.InvalidOperation_NoAssetToWrite);

    /// <inheritdoc cref="SR.IO_AssetEOF"/>
    [DoesNotReturn]
    internal static void ThrowIO_AssetEOF(string assetName, string fileName)
        => throw new IOException(string.Format(SR.IO_AssetEOF, assetName, fileName));

    /// <inheritdoc cref="SR.IO_AssetTooLong2GB"/>
    [DoesNotReturn]
    internal static void ThrowIO_AssetTooLong2GB(string name)
        => throw new IOException(string.Format(SR.IO_AssetTooLong2GB, name));

    /// <inheritdoc cref="SR.IO_BadAsset"/>
    [DoesNotReturn]
    internal static void ThrowIO_BadAsset(long offset, string fileName, Exception innerException)
        => throw new IOException(string.Format(SR.IO_BadAsset, offset, fileName), innerException);

    /// <inheritdoc cref="SR.IO_BadAssetDat"/>
    [DoesNotReturn]
    internal static void ThrowIO_BadAssetDat(long size, string fileName)
        => throw new IOException(string.Format(SR.IO_BadAssetDat, size, fileName));

    /// <inheritdoc cref="SR.IO_BadAssetInfo"/>
    [DoesNotReturn]
    internal static void ThrowIO_BadAssetInfo(uint offset, string name)
        => throw new IOException(string.Format(SR.IO_BadAssetInfo, offset, name));

    /// <inheritdoc cref="SR.IO_BadManifest"/>
    [DoesNotReturn]
    internal static void ThrowIO_BadManifest(string fileName)
        => throw new IOException(string.Format(SR.IO_BadManifest, fileName));

    /// <inheritdoc cref="SR.IO_CantCreateTempFile"/>
    [DoesNotReturn]
    internal static T ThrowIO_CantCreateTempFile<T>(string fileName)
        => throw new IOException(string.Format(SR.IO_CantCreateTempFile, fileName));

    /// <inheritdoc cref="SR.IO_CrcMismatch"/>
    [DoesNotReturn]
    internal static void ThrowIO_CrcMismatch(string name, uint assetCrc32, uint fileCrc32, string fileName)
        => throw new IOException(string.Format(SR.IO_CrcMismatch, name, assetCrc32, fileCrc32, fileName));

    /// <inheritdoc cref="SR.IO_InvalidDecimalBits"/>
    [DoesNotReturn]
    internal static T ThrowIO_InvalidDecimalBits<T>(Exception innerException)
        => throw new IOException(SR.IO_InvalidDecimalBits, innerException);

    /// <inheritdoc cref="SR.IO_NoMoreAssetDatFilesRead"/>
    [DoesNotReturn]
    internal static T ThrowIO_NoMoreAssetDatFilesRead<T>(string fileName)
        => throw new IOException(string.Format(SR.IO_NoMoreAssetDatFilesRead, fileName));

    /// <inheritdoc cref="SR.IO_NoMoreAssetDatFilesWrite"/>
    [DoesNotReturn]
    internal static T ThrowIO_NoMoreAssetDatFilesWrite<T>(string fileName, Exception innerException)
        => throw new IOException(string.Format(SR.IO_NoMoreAssetDatFilesWrite, fileName), innerException);

    /// <inheritdoc cref="SR.NotSupported_PackTempWrite"/>
    [DoesNotReturn]
    internal static void ThrowNotSupported_PackTempWrite()
        => throw new NotSupportedException(SR.NotSupported_PackTempWrite);

    /// <inheritdoc cref="SR.NotSupported_PackTempWrite"/>
    [DoesNotReturn]
    internal static T ThrowNotSupported_PackTempWrite<T>()
        => throw new NotSupportedException(SR.NotSupported_PackTempWrite);

    /// <inheritdoc cref="SR.NotSupported_UnwritableStream"/>
    [DoesNotReturn]
    internal static void ThrowNotSupported_UnwritableStream()
        => throw new NotSupportedException(SR.NotSupported_UnwritableStream);

    /// <inheritdoc cref="SR.Overflow_CantAddAsset"/>
    [DoesNotReturn]
    internal static void ThrowOverflow_CantAddAsset(string assetName, string fileName, Exception innerException)
        => throw new OverflowException(string.Format(SR.Overflow_CantAddAsset, assetName, fileName), innerException);

    /// <inheritdoc cref="SR.Overflow_TooManyAssets"/>
    [DoesNotReturn]
    internal static T ThrowOverflow_TooManyAssets<T>(string fileName, Exception innerException)
        => throw new OverflowException(string.Format(SR.Overflow_TooManyAssets, fileName), innerException);
}
