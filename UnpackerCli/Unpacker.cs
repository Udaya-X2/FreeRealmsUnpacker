using AssetIO;
using McMaster.Extensions.CommandLineUtils;
using ShellProgressBar;
using System.ComponentModel;
using System.Diagnostics;
using System.Text;

namespace UnpackerCli;

/// <summary>
/// The driver class of the command line Free Realms Unpacker.
/// </summary>
public partial class Unpacker
{
    /// <summary>
    /// The entry point of the command line Free Realms Unpacker, following command parsing.
    /// </summary>
    /// <returns>The process exit code.</returns>
    public int OnExecute()
    {
        Encoding consoleEncoding = Console.OutputEncoding;

        try
        {
            if (!ValidateArguments()) return 1;

            // Handle the asset types specified.
            Stopwatch sw = Stopwatch.StartNew();
            Console.OutputEncoding = Encoding.UTF8;
            AssetType assetFilter = GetAssetFilter();
            IEnumerable<AssetFile> assetFiles = ClientDirectory.EnumerateAssetFiles(InputDirectory, assetFilter);
            int numAssets = ListFiles ? ListFilesFormatted(assetFiles) : assetFiles.GroupBy(x => x.DirectoryType)
                                                                                   .Sum(x => HandleAssets(x.Key, x));

            // Print the number of assets or files found.
            string message = (numAssets, ListFiles) switch
            {
                (0, false) => $"\nNo assets found.",
                (0, true) => $"\nNo asset files found.",
                (1, false) => $"\n1 asset found in {sw.Elapsed:hh\\:mm\\:ss\\.fff}",
                (1, true) => $"\n1 asset file found in {sw.Elapsed:hh\\:mm\\:ss\\.fff}",
                (_, false) => $"\n{numAssets} assets found in {sw.Elapsed:hh\\:mm\\:ss\\.fff}",
                (_, true) => $"\n{numAssets} asset files found in {sw.Elapsed:hh\\:mm\\:ss\\.fff}"
            };

            Console.Error.WriteLine(message);
            return 0;
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Error.WriteLine(Debug ? $"\n{ex}" : $"ERROR - {ex.Message}");
            Console.ResetColor();
            return 2;
        }
        finally
        {
            Console.OutputEncoding = consoleEncoding;
        }
    }

    /// <summary>
    /// Dispatches on assets of the specified type, based on the command.
    /// </summary>
    /// <returns>The number of assets of the specified type.</returns>
    private int HandleAssets(AssetType assetType, IEnumerable<AssetFile> assetFiles)
    {
        if (CountAssets) return assetFiles.Sum(x => x.Count);

        using ProgressBar? pbar = CreateProgressBar(assetType, assetFiles);
        int numAssets = 0;
        int numErrors = 0;

        foreach (AssetFile assetFile in assetFiles)
        {
            if (ListAssets)
            {
                numAssets += ListAssetsFormatted(assetFile);
            }
            else if (ValidateAssets)
            {
                numAssets += ValidateChecksums(assetFile, pbar, ref numErrors);
            }
            else
            {
                numAssets += ExtractAssets(assetFile, pbar);
            }
        }

        return numAssets;
    }

    /// <summary>
    /// Lists the specified asset files in either tabular or
    /// line-based form, depending on the command-line options.
    /// </summary>
    /// <returns>The number of asset files.</returns>
    private int ListFilesFormatted(IEnumerable<AssetFile> assetFiles)
    {
        if (DisplayTable)
        {
            Table table = new("Name", "Assets", "Size");

            foreach (AssetFile assetFile in assetFiles)
            {
                table.AddRow(assetFile.FullName, assetFile.Count, assetFile.Info.Length);
            }

            table.Print();
            return table.Count;
        }
        else
        {
            int numAssets = 0;

            foreach (AssetFile assetFile in assetFiles)
            {
                Console.WriteLine(assetFile.FullName);
                numAssets++;
            }

            return numAssets;
        }
    }

    /// <summary>
    /// Lists assets from the specified asset file in either tabular
    /// or line-based form, depending on the command-line options.
    /// </summary>
    /// <returns>The number of assets in the asset file.</returns>
    private int ListAssetsFormatted(AssetFile assetFile)
    {
        if (DisplayTable)
        {
            Table table = new("Name", "Offset", "Size", "CRC-32");

            foreach (Asset asset in assetFile)
            {
                table.AddRow(asset.Name, asset.Offset, asset.Size, asset.Crc32);
            }

            string plural = table.Count == 1 ? "" : "s";
            Console.WriteLine($"\nFound {table.Count} asset{plural} in {assetFile}:\n");
            table.Print();
            return table.Count;
        }
        else
        {
            int numAssets = 0;

            foreach (Asset asset in assetFile)
            {
                Console.WriteLine(asset.Name);
                numAssets++;
            }

            return numAssets;
        }
    }

    /// <summary>
    /// Validates assets in the specified asset file by comparing their checksums.
    /// </summary>
    /// <returns>The number of assets in the asset file.</returns>
    private static int ValidateChecksums(AssetFile assetFile, ProgressBar? pbar, ref int numErrors)
    {
        using AssetReader reader = assetFile.OpenRead();
        int numAssets = 0;

        foreach (Asset asset in assetFile)
        {
            if (asset.Crc32 == reader.GetCrc32(asset))
            {
                pbar?.UpdateProgress($"[{numErrors} CRC errors] Validated {asset.Name}");
            }
            else if (pbar != null)
            {
                pbar.UpdateProgress($"[{++numErrors} CRC errors] Validated {asset.Name}");
            }
            else
            {
                Console.WriteLine($"'{asset.Name}' in '{assetFile}' does not match the expected CRC.");
            }

            numAssets++;
        }

        return numAssets;
    }

    /// <summary>
    /// Extracts assets in the specified asset file to the output directory.
    /// </summary>
    /// <returns>The number of assets in the asset file.</returns>
    private int ExtractAssets(AssetFile assetFile, ProgressBar? pbar)
    {
        using AssetReader reader = assetFile.OpenRead();
        int numAssets = 0;

        foreach (Asset asset in assetFile)
        {
            string assetPath = $"{OutputDirectory}/{asset.Name}";

            if (SkipExisting && File.Exists(assetPath))
            {
                pbar?.UpdateProgress($"Skipped {asset.Name}");
            }
            else
            {
                Directory.CreateDirectory(Path.GetDirectoryName(assetPath) ?? OutputDirectory);
                using FileStream fs = new(assetPath, FileMode.Create, FileAccess.Write, FileShare.Read);
                reader.CopyTo(asset, fs);
                pbar?.UpdateProgress($"Extracted {asset.Name}");
            }

            numAssets++;
        }

        return numAssets;
    }

    /// <summary>
    /// Creates a <see cref="ProgressBar"/> to display the number of
    /// assets processed in the asset files of the specified type.
    /// </summary>
    /// <returns>
    /// A <see cref="ProgressBar"/> with the number of assets for max
    /// ticks and an <paramref name="assetType"/>-dependent color,
    /// or <see langword="null"/> if progress bars are disabled.
    /// </returns>
    /// <exception cref="InvalidEnumArgumentException"/>
    private ProgressBar? CreateProgressBar(AssetType assetType, IEnumerable<AssetFile> assetFiles)
    {
        if (NoProgressBars || ListAssets || ListFiles || CountAssets) return null;

        (string message, ConsoleColor color) = assetType switch
        {
            AssetType.Game => ("Reading game assets...", ConsoleColor.Green),
            AssetType.Tcg => ("Reading TCG assets...", ConsoleColor.Blue),
            AssetType.Resource => ("Reading resource assets...", ConsoleColor.DarkYellow),
            _ => throw new InvalidEnumArgumentException(nameof(assetType), (int)assetType, assetType.GetType())
        };
        int numAssets = assetFiles.Sum(x => x.Count);
        ProgressBarOptions options = new() { ForegroundColor = color, ProgressCharacter = '─' };
        return new ProgressBar(numAssets, message, options);
    }

    /// <summary>
    /// Returns an enum value specifying the asset types to extract, based on the command-line options.
    /// </summary>
    /// <returns>An enum value specifying the asset types to extract.</returns>
    private AssetType GetAssetFilter()
    {
        AssetType assetType = 0;

        // Set the enum flag corresponding to each command-line option.
        if (ExtractGame) assetType |= AssetType.Game;
        if (ExtractTcg) assetType |= AssetType.Tcg;
        if (ExtractResource) assetType |= AssetType.Resource;
        if (ExtractDat) assetType |= AssetType.Dat;
        if (ExtractPack) assetType |= AssetType.Pack;

        // By default (no options are set), extract all asset types.
        if (assetType.GetDirectoryType() == 0)
        {
            assetType |= AssetType.AllDirectories;
        }
        if (assetType.GetFileType() == 0)
        {
            assetType |= AssetType.AllFiles;
        }

        return assetType;
    }

    /// <summary>
    /// Throws an exception if the provided command-line arguments are invalid.
    /// </summary>
    /// <returns><see langword="true"/> if the arguments are valid; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="Exception"/>
    private bool ValidateArguments() => (ListAssets, ListFiles, ValidateAssets, CountAssets) switch
    {
        (true, true, _, _) => throw new Exception("Cannot both list assets and files."),
        (true, _, true, _) => throw new Exception("Cannot both list and validate assets."),
        (_, true, true, _) => throw new Exception("Cannot both list files and validate assets."),
        (true, _, _, true) => throw new Exception("Cannot both list and count assets."),
        (_, true, _, true) => throw new Exception("Cannot both list files and count assets."),
        (_, _, true, true) => throw new Exception("Cannot both validate and count assets."),
        (false, false, false, false) => SetupOutputDirectory(),
        _ => true,
    };

    /// <summary>
    /// Prompts the user to create the output directory if it does not exist.
    /// </summary>
    /// <returns>
    /// <see langword="true"/> if the output directory was set up successfully; otherwise, <see langword="false"/>.
    /// </returns>
    private bool SetupOutputDirectory()
    {
        DirectoryInfo dir = new(OutputDirectory);

        if (!dir.Exists)
        {
            if (PromptUser($"{dir.FullName} does not exist. Create the directory?"))
            {
                dir.Create();
            }
            else
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Gets a yes/no response from the console after displaying a <paramref name="prompt"/>.
    /// </summary>
    /// <returns><see langword="true"/> if the answer is 'yes'; otherwise, <see langword="false"/>.</returns>
    private bool PromptUser(string prompt) => AnswerYes || Prompt.GetYesNo(prompt, true);
}
