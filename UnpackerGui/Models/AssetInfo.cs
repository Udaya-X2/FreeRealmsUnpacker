using AssetIO;
using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using UnpackerGui.Converters;
using UnpackerGui.Services;

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
    private static readonly FrozenSet<string> s_imageFormats = FrozenSet.Create(
    [
        "3FR",
        "AFPHOTO",
        "AI",
        "APNG",
        "ARI",
        "ARW",
        "ASTC",
        "AVIF",
        "BAY",
        "BMP",
        "BPG",
        "BRAW",
        "CAP",
        "CD5",
        "CDR",
        "CLIP",
        "CPT",
        "CR2",
        "CR3",
        "CRI",
        "CRW",
        "DCR",
        "DCS",
        "DDS",
        "DIB",
        "DNG",
        "DRF",
        "DXF",
        "EIP",
        "EMF",
        "EPS",
        "ERF",
        "FFF",
        "FITS",
        "FLIF",
        "GIF",
        "GPR",
        "GTF",
        "HDR",
        "HEIC",
        "HEIF",
        "ICO",
        "IIQ",
        "IND",
        "INDD",
        "INDT",
        "J2K",
        "JFI",
        "JFIF",
        "JIF",
        "JP2",
        "JPE",
        "JPEG",
        "JPF",
        "JPG",
        "JPM",
        "JPX",
        "JXL",
        "K25",
        "KDC",
        "KRA",
        "KTX",
        "MDC",
        "MDP",
        "MEF",
        "MJ2",
        "MOS",
        "MRW",
        "NEF",
        "NRW",
        "ORF",
        "PDF",
        "PDN",
        "PEF",
        "PKM",
        "PLD",
        "PNG",
        "PSD",
        "PSP",
        "PTX",
        "PXN",
        "R3D",
        "RAF",
        "RAW",
        "RW2",
        "RWL",
        "RWZ",
        "SAI",
        "SR2",
        "SRF",
        "SRW",
        "SVG",
        "SVGZ",
        "TCO",
        "TGA",
        "TIF",
        "TIFF",
        "WBMP",
        "WEBP",
        "WMF",
        "X3F",
        "XCF"
    ]);
    private static readonly FrozenSet<string> s_audioFormats = FrozenSet.Create(
    [
        "AIF",
        "AIFF",
        "BINKA",
        "BWF",
        "IT",
        "ITZ",
        "M1A",
        "M2A",
        "MDZ",
        "MO3",
        "MOD",
        "MP1",
        "MP2",
        "MP3",
        "MP3PRO",
        "MPA",
        "MPEG",
        "MPG",
        "MTM",
        "MUS",
        "OGG",
        "S3M",
        "S3Z",
        "UMX",
        "WAV",
        "XM",
        "XMZ"
    ]);

    /// <summary>
    /// Gets an <see cref="EqualityComparer{T}"/> that compares
    /// assets for equality by value, regardless of derived type.
    /// </summary>
    public static readonly EqualityComparer<Asset> Comparer = EqualityComparer<Asset>.Create(Equals, GetHashCode);

    /// <summary>
    /// Gets the file type of the asset.
    /// </summary>
    public string Type { get; } = GetUpperExtension(Name);

    /// <summary>
    /// Gets or sets the CRC-32 value of the asset in the corresponding asset file.
    /// </summary>
    public uint FileCrc32 { get; set; }

    /// <summary>
    /// Gets whether the asset's CRC-32 value is correct.
    /// </summary>
    public bool IsValid => Crc32 == FileCrc32;

    /// <summary>
    /// Gets whether the asset is an image.
    /// </summary>
    public bool IsImage => s_imageFormats.Contains(Type);

    /// <summary>
    /// Gets whether the asset is an audio file.
    /// </summary>
    public bool IsAudio => s_audioFormats.Contains(Type);

    /// <summary>
    /// Gets the file size of the asset.
    /// </summary>
    public string FileSize => FileSizeConverter.GetFileSize(Size);

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
    /// Extracts the asset to a temporary location.
    /// </summary>
    /// <returns>The extracted file.</returns>
    public FileInfo ExtractTempFile()
    {
        using AssetReader reader = AssetFile.OpenRead();
        return reader.ExtractTo(this, App.GetService<IFilesService>().CreateTempFolder());
    }

    /// <summary>
    /// Extracts the asset to a temporary location and opens it.
    /// </summary>
    public void Open() => Process.Start(new ProcessStartInfo
    {
        UseShellExecute = true,
        FileName = ExtractTempFile().FullName
    });

    /// <summary>
    /// Returns a string representation of the asset's properties.
    /// </summary>
    /// <returns>A string representation of the asset's properties.</returns>
    public override string ToString()
        => $"{Name}\nSize: {FileSize}\nFile: {AssetFile.Name}\nLocation: {AssetFile.DirectoryName}";

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

    /// <summary>
    /// Returns the uppercase extension of the specified file path (excluding the period, ".").
    /// </summary>
    private static string GetUpperExtension(ReadOnlySpan<char> path)
    {
        ReadOnlySpan<char> extension = Path.GetExtension(path);

        if (extension.Length == 0) return "";

        Span<char> upperExtension = stackalloc char[extension.Length - 1];
        extension[1..].ToUpperInvariant(upperExtension);
        return upperExtension.ToString();
    }
}
