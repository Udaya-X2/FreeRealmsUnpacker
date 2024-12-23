using AssetIO;

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
    /// Gets or sets the CRC-32 value of the asset in the corresponding asset file.
    /// </summary>
    public uint FileCrc32 { get; set; }

    /// <summary>
    /// Gets whether the asset's CRC-32 value is correct.
    /// </summary>
    public bool IsValid => Crc32 == FileCrc32;

    public AssetInfo(Asset asset, AssetFile assetFile)
        : this(asset.Name, asset.Offset, asset.Size, asset.Crc32, assetFile)
    {
    }
}
