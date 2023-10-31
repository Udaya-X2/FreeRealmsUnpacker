using System.Text.RegularExpressions;

namespace AssetIO;

/// <summary>
/// Provides static methods for obtaining asset files in a Free Realms client directory.
/// </summary>
public static partial class ClientDirectory
{
    private const string ManifestFileSuffix = "_manifest.dat";

    [GeneratedRegex(@"^_\d{3}\.dat$", RegexOptions.IgnoreCase, "en-US")]
    private static partial Regex DataSuffixRegex();

    /// <summary>
    /// Returns an enumerable collection of the asset files that match a filter on a specified path.
    /// </summary>
    /// <param name="path">The relative or absolute path to the directory to search.</param>
    /// <param name="assetFilter">
    /// A bitwise combination of the enumeration values that specify which asset types are allowed.
    /// </param>
    /// <param name="searchOption">
    /// One of the enumeration values that specifies whether the search operation
    /// should include only the current directory or should include all subdirectories.
    /// </param>
    /// <returns>
    /// An enumerable collection of the asset files in the directory specified
    /// by <paramref name="path"/> that match the specified filter.
    /// </returns>
    /// <exception cref="ArgumentNullException"/>
    public static IEnumerable<AssetFile> EnumerateAssetFiles(string path,
                                                             AssetType assetFilter = AssetType.All,
                                                             SearchOption searchOption = SearchOption.AllDirectories)
    {
        if (path == null) throw new ArgumentNullException(nameof(path));

        foreach (string file in Directory.EnumerateFiles(path, "*", searchOption))
        {
            AssetType assetType = ClientFile.InferAssetType(file);

            if (assetType != 0 && assetFilter.HasFlag(assetType))
            {
                yield return new AssetFile(file, assetType);
            }
        }
    }

    /// <summary>
    /// Returns an enumerable collection of full file names for all asset
    /// .dat files with the specified asset type in a specified path.
    /// </summary>
    /// <param name="path">The relative or absolute path to the directory to search.</param>
    /// <param name="assetFilter">
    /// A bitwise combination of the enumeration values that specify which asset types are allowed.
    /// </param>
    /// <param name="searchOption">
    /// One of the enumeration values that specifies whether the search operation
    /// should include only the current directory or should include all subdirectories.
    /// </param>
    /// <returns>
    /// An enumerable collection of the full file names (including paths) for the asset .dat files
    /// with the specified asset type in the directory specified by <paramref name="path"/>.
    /// </returns>
    /// <exception cref="ArgumentException"/>
    /// <exception cref="ArgumentNullException"/>
    public static IEnumerable<string> EnumerateDataFiles(string path,
                                                         AssetType assetFilter = AssetType.All,
                                                         SearchOption searchOption = SearchOption.AllDirectories)
    {
        if (path == null) throw new ArgumentNullException(nameof(path));

        foreach (string file in Directory.EnumerateFiles(path, "*", searchOption))
        {
            AssetType assetType = ClientFile.InferDataType(file);

            if (assetType != 0 && assetFilter.HasFlag(assetType))
            {
                yield return file;
            }
        }
    }

    /// <summary>
    /// Returns an enumerable collection of full file names for all asset
    /// .dat files corresponding to the specified manifest .dat file.
    /// </summary>
    /// <param name="manifestFile">The manifest .dat file.</param>
    /// <param name="searchOption">
    /// One of the enumeration values that specifies whether the search operation
    /// should include only the current directory or should include all subdirectories.
    /// </param>
    /// <returns>
    /// An enumerable collection of the full file names (including paths) for
    /// the asset .dat files corresponding to the specified manifest.dat file.
    /// </returns>
    /// <exception cref="ArgumentNullException"></exception>
    public static IEnumerable<string> EnumerateDataFiles(FileInfo manifestFile,
                                                         SearchOption searchOption = SearchOption.TopDirectoryOnly)
    {
        if (manifestFile == null) throw new ArgumentNullException(nameof(manifestFile));
        if (!manifestFile.Name.EndsWith(ManifestFileSuffix, StringComparison.OrdinalIgnoreCase)
            || manifestFile.DirectoryName is not string dirName) return Enumerable.Empty<string>();

        string manifestFilePrefix = manifestFile.Name[..^ManifestFileSuffix.Length];
        return Directory.EnumerateFiles(dirName, "*", searchOption)
                        .Where(x => MatchesManifestFile(x, manifestFilePrefix));
    }

    /// <summary>
    /// Returns <see langword="true"/> if the specified path has the same prefix as the specified manifest
    /// file prefix and ends with the suffix of an asset .dat file; otherwise, <see langword="false"/>.
    /// </summary>
    /// <returns>
    /// <see langword="true"/> if the specified path has the same prefix as the specified manifest
    /// file prefix and ends with the suffix of an asset .dat file; otherwise, <see langword="false"/>.
    /// </returns>
    private static bool MatchesManifestFile(string path, string manifestFilePrefix)
    {
        ReadOnlySpan<char> filename = Path.GetFileName(path.AsSpan());
        return filename.StartsWith(manifestFilePrefix, StringComparison.OrdinalIgnoreCase)
            && DataSuffixRegex().IsMatch(filename[manifestFilePrefix.Length..]);
    }
}
