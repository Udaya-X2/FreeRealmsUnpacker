using System.Numerics;
using System.Runtime.CompilerServices;

namespace AssetIO;

/// <summary>
/// Provides extension methods for the <see cref="AssetType"/> class.
/// </summary>
public static class EnumExtensions
{
    /// <summary>
    /// Returns the asset type with only the file flags set.
    /// </summary>
    /// <returns>The asset type with only the file flags set.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static AssetType GetFileType(this AssetType assetType) => assetType & AssetType.AllFiles;

    /// <summary>
    /// Returns the asset type with only the directory flags set.
    /// </summary>
    /// <returns>The asset type with only the directory flags set.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static AssetType GetDirectoryType(this AssetType assetType) => assetType & AssetType.AllDirectories;

    /// <summary>
    /// Returns <see langword="true"/> if the asset type has exactly one file flag
    /// and at most one directory flag set; otherwise, <see langword="false"/>.
    /// </summary>
    /// <returns>
    /// <see langword="true"/> if the asset type has exactly one file flag and
    /// at most one directory flag set; otherwise, <see langword="false"/>.
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsValid(this AssetType assetType)
        => BitOperations.IsPow2((uint)assetType.GetFileType())
        && IsPow2OrZero((uint)assetType.GetDirectoryType());

    /// <summary>
    /// Evaluates whether the specified <typeparamref name="T"/> value is a power of two or zero.
    /// </summary>
    /// <returns>
    /// <see langword="true"/> if the specified value is a power of two; <see langword="false"/> otherwise.
    /// </returns>
    private static bool IsPow2OrZero<T>(T value) where T : IBinaryInteger<T> => (value & (value - T.One)) == T.Zero;
}
