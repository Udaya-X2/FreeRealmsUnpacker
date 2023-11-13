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
    /// Returns <see langword="true"/> if the asset type has exactly one file
    /// flag and one directory flag set; otherwise, <see langword="false"/>.
    /// </summary>
    /// <returns>
    /// <see langword="true"/> if the asset type has exactly one file flag
    /// and one directory flag set; otherwise, <see langword="false"/>.
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsValid(this AssetType assetType)
        => BitOperations.IsPow2((uint)assetType.GetFileType())
        && BitOperations.IsPow2((uint)assetType.GetDirectoryType());
}
