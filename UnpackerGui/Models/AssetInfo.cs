using AssetIO;
using System;
using System.Collections.Generic;
using System.IO;
using UnpackerGui.Converters;

namespace UnpackerGui.Models;

/// <summary>
/// Represents a Free Realms asset in terms of its file properties.
/// </summary>
/// <param name="Name">The name of the asset.</param>
/// <param name="Offset">The byte offset of the asset in the asset file.</param>
/// <param name="Size">The size of the asset, in bytes.</param>
/// <param name="Crc32">The CRC-32 checksum of the asset.</param>
/// <param name="AssetFile">The file containing the asset.</param>
public record AssetInfo(string Name, long Offset, uint Size, uint Crc32, AssetFile AssetFile)
    : Asset(Name, Offset, Size, Crc32)
{
    /// <summary>
    /// Gets an <see cref="EqualityComparer{T}"/> that compares
    /// assets for equality by value, regardless of derived type.
    /// </summary>
    public static readonly EqualityComparer<Asset> Comparer = EqualityComparer<Asset>.Create(Equals, GetHashCode);

    /// <summary>
    /// Gets the file type of the asset.
    /// </summary>
    public string Type { get; } = Path.GetExtension(Name);

    /// <summary>
    /// Gets or sets the CRC-32 value of the asset in the corresponding asset file.
    /// </summary>
    public uint FileCrc32 { get; set; }

    /// <summary>
    /// Gets whether the asset's CRC-32 value is correct.
    /// </summary>
    public bool IsValid => Crc32 == FileCrc32;

    /// <summary>
    /// Initializes a new instance of <see cref="AssetInfo"/> by copying the properties of the specified asset and file.
    /// </summary>
    /// <param name="asset">The Free Realms asset.</param>
    /// <param name="assetFile">The file containing the asset.</param>
    public AssetInfo(Asset asset, AssetFile assetFile)
        : this(asset.Name, asset.Offset, asset.Size, asset.Crc32, assetFile)
    {
    }

    /// <summary>
    /// Returns a string representation of the asset's properties.
    /// </summary>
    /// <returns>A string representation of the asset's properties.</returns>
    public override string ToString()
        => $"{Name}\nSize: {FileSizeConverter.GetFileSize(Size)}\n"
           + $"File: {AssetFile.Name}\nLocation: {AssetFile.DirectoryName}";

    /// <summary>
    /// Checks whether the two assets are equal by value, regardless of derived type.
    /// </summary>
    private static bool Equals(Asset? x, Asset? y) => ReferenceEquals(x, y)
                                                      || x is not null
                                                      && y is not null
                                                      && x.Name == y.Name
                                                      && x.Offset == y.Offset
                                                      && x.Size == y.Size
                                                      && x.Crc32 == y.Crc32;

    /// <summary>
    /// Returns a hash code based on the asset's properties.
    /// </summary>
    private static int GetHashCode(Asset x) => HashCode.Combine(x.Name, x.Offset, x.Size, x.Crc32);
}
