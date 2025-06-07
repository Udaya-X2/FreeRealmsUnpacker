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
    /// <param name="requireFullType">
    /// <see langword="true"/> to exclude asset files without a full asset type;
    /// <see langword="false"/> to include asset files with only a file type.
    /// </param>
    /// <returns>
    /// An enumerable collection of the asset files in the directory specified
    /// by <paramref name="path"/> that match the specified filter.
    /// </returns>
    /// <exception cref="ArgumentNullException"/>
    public static IEnumerable<AssetFile> EnumerateAssetFiles(string path,
                                                             AssetType assetFilter = AssetType.All,
                                                             SearchOption searchOption = SearchOption.AllDirectories,
                                                             bool requireFullType = true)
    {
        ArgumentNullException.ThrowIfNull(path, nameof(path));

        foreach (string file in Directory.EnumerateFiles(path, "*", searchOption))
        {
            AssetType assetType = ClientFile.InferAssetType(file, requireFullType);

            if (assetType != 0 && (assetFilter & assetType) == assetType)
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
        ArgumentNullException.ThrowIfNull(path, nameof(path));

        foreach (string file in Directory.EnumerateFiles(path, "*", searchOption))
        {
            AssetType assetType = ClientFile.InferDataType(file);

            if (assetType != 0 && (assetFilter & assetType) == assetType)
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
    /// <exception cref="ArgumentNullException"/>
    public static IEnumerable<string> EnumerateDataFiles(FileInfo manifestFile,
                                                         SearchOption searchOption = SearchOption.TopDirectoryOnly)
    {
        ArgumentNullException.ThrowIfNull(manifestFile, nameof(manifestFile));

        if (!manifestFile.Name.EndsWith(ManifestFileSuffix, StringComparison.OrdinalIgnoreCase)
            || manifestFile.DirectoryName is not string dirName) return [];

        string manifestFilePrefix = manifestFile.Name[..^ManifestFileSuffix.Length];
        return Directory.EnumerateFiles(dirName, "*", searchOption)
                        .Where(x => MatchesManifestFile(x, manifestFilePrefix));
    }

    public static IEnumerable<string> EnumerateDataFilesUnlimited(string manifestFile)
        => EnumerateDataFilesUnlimited(new FileInfo(manifestFile));

    public static IEnumerable<string> EnumerateDataFilesUnlimited(FileInfo manifestFile)
    {
        ArgumentNullException.ThrowIfNull(manifestFile, nameof(manifestFile));

        int i = 0;
        string path = EscapeFormatString(manifestFile.FullName);
        string dataFileFormat = path.EndsWith(ManifestFileSuffix, StringComparison.OrdinalIgnoreCase)
            ? $"{path[..^ManifestFileSuffix.Length]}_{{0:D3}}.dat"
            : $"{Path.ChangeExtension(path, null)}_{{0:D3}}.dat";

        while (true)
        {
            yield return string.Format(dataFileFormat, i++);
        }
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

    /// <summary>
    /// Escapes literal braces ({, }) by doubling them in the result string.
    /// </summary>
    /// <returns>A string of characters with brace characters converted to their escaped form.</returns>
    private static string EscapeFormatString(string str)
    {
        int i = str.IndexOfAny(['{', '}']);

        if (i < 0) return str;

        Span<char> result = str.Length <= 128 ? stackalloc char[2 * str.Length] : new char[2 * str.Length];

        // Copy the characters already proven not to match.
        if (i > 0)
        {
            str.AsSpan(0, i).CopyTo(result);
        }

        // Copy the remaining characters, doing the replacement as we go.
        for (int j = i; i < str.Length; i++)
        {
            char c = str[i];

            if (str[i] is '{' or '}')
            {
                result[j++] = c;
            }

            result[j++] = c;
        }

        return new string(result);
    }
}
