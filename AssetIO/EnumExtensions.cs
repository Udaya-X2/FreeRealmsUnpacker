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
    /// <param name="assetType">The asset type.</param>
    /// <returns>The asset type with only the file flags set.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static AssetType GetFileType(this AssetType assetType) => assetType & AssetType.AllFiles;

    /// <summary>
    /// Returns the asset type with only the directory flags set.
    /// </summary>
    /// <param name="assetType">The asset type.</param>
    /// <returns>The asset type with only the directory flags set.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static AssetType GetDirectoryType(this AssetType assetType) => assetType & AssetType.AllDirectories;

    /// <summary>
    /// Returns <see langword="true"/> if the asset type has exactly one file flag (except <see cref="AssetType.Temp"/>)
    /// and at most one directory flag set; otherwise, <see langword="false"/>.
    /// </summary>
    /// <param name="assetType">The asset type.</param>
    /// <returns>
    /// <see langword="true"/> if the asset type has exactly one file flag (except <see cref="AssetType.Temp"/>)
    /// and at most one directory flag set; otherwise, <see langword="false"/>.
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsValid(this AssetType assetType)
        => BitOperations.IsPow2((uint)(assetType.GetFileType() & ~AssetType.Temp))
        && IsPow2OrZero((uint)assetType.GetDirectoryType())
        && (AssetType.All | assetType) == AssetType.All;

    /// <summary>
    /// Evaluates whether the specified <see langword="uint"/> value is a power of two or zero.
    /// </summary>
    /// <param name="value">An unsigned integer value.</param>
    /// <returns>
    /// <see langword="true"/> if the specified value is a power of two; <see langword="false"/> otherwise.
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsPow2OrZero(uint value) => (value & (value - 1)) == 0;
}
