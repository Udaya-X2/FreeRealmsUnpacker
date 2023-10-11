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
            int numAssets = 0;

            if (ListFiles)
            {
                numAssets = ListFilesFormatted(ClientDirectory.GetAssetFiles(InputDirectory, assetFilter));
            }
            else
            {
                foreach (var item in ClientDirectory.GetAssetFilesByType(InputDirectory, assetFilter))
                {
                    numAssets += HandleAssets(item.Key, item.Value);
                }
            }

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
    private int HandleAssets(AssetType assetType, List<string> assetFiles)
    {
        if (assetFiles.Count == 0) return 0;
        if (CountAssets) return assetFiles.Sum(ClientFile.GetAssetCount);

        using ProgressBar? pbar = CreateProgressBar(assetType, assetFiles);
        int numAssets = 0;
        int numErrors = 0;

        foreach (string assetFile in assetFiles)
        {
            Asset[] assets = ClientFile.GetAssets(assetFile);

            if (ListAssets)
            {
                ListAssetsFormatted(assetFile, assets);
            }
            else if (ValidateAssets)
            {
                ValidateChecksums(assetFile, assets, pbar, ref numErrors);
            }
            else
            {
                ExtractAssets(assetFile, assets, pbar);
            }

            numAssets += assets.Length;
        }

        return numAssets;
    }

    /// <summary>
    /// Lists the specified asset files in either tabular or
    /// line-based form, depending on the command-line options.
    /// </summary>
    private int ListFilesFormatted(string[] assetFiles)
    {
        if (DisplayTable)
        {
            Table table = new(assetFiles.Length, "Name", "Assets", "Size");

            foreach (string assetFile in assetFiles)
            {
                int numAssets = ClientFile.GetAssetCount(assetFile);
                long size = new FileInfo(assetFile).Length;
                table.AddRow(assetFile, numAssets, size);
            }

            table.Print();
        }
        else
        {
            Array.ForEach(assetFiles, Console.WriteLine);
        }

        return assetFiles.Length;
    }

    /// <summary>
    /// Lists assets from the specified asset file in either tabular
    /// or line-based form, depending on the command-line options.
    /// </summary>
    private void ListAssetsFormatted(string assetFile, Asset[] assets)
    {
        if (DisplayTable)
        {
            Table table = new(assets.Length, "Name", "Offset", "Size", "CRC-32");
            string plural = assets.Length == 1 ? "" : "s";
            Console.WriteLine($"\nFound {assets.Length} asset{plural} in {assetFile}:\n");
            Array.ForEach(assets, x => table.AddRow(x.Name, x.Offset, x.Size, x.Crc32));
            table.Print();
        }
        else
        {
            Array.ForEach(assets, x => Console.WriteLine(x.Name));
        }
    }

    /// <summary>
    /// Validates assets in the specified asset file by comparing their checksums.
    /// </summary>
    private static void ValidateChecksums(string assetFile, Asset[] assets, ProgressBar? pbar, ref int numErrors)
    {
        if (assets.Length == 0) return;

        using AssetReader reader = AssetReader.Create(assetFile);

        foreach (Asset asset in assets)
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
        }
    }

    /// <summary>
    /// Extracts assets in the specified asset file to the output directory.
    /// </summary>
    private void ExtractAssets(string assetFile, Asset[] clientAssets, ProgressBar? pbar)
    {
        if (clientAssets.Length == 0) return;

        using AssetReader reader = AssetReader.Create(assetFile);

        foreach (Asset asset in clientAssets)
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
        }
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
    /// <exception cref="InvalidEnumArgumentException"></exception>
    private ProgressBar? CreateProgressBar(AssetType assetType, IEnumerable<string> assetFiles)
    {
        if (NoProgressBars || ListAssets || ListFiles || CountAssets) return null;

        (string message, ConsoleColor color) = assetType switch
        {
            AssetType.Game => ("Reading game assets...", ConsoleColor.Green),
            AssetType.Tcg => ("Reading TCG assets...", ConsoleColor.Blue),
            AssetType.Resource => ("Reading resource assets...", ConsoleColor.DarkYellow),
            _ => throw new InvalidEnumArgumentException(nameof(assetType), (int)assetType, assetType.GetType())
        };
        int numAssets = assetFiles.Sum(ClientFile.GetAssetCount);
        ProgressBarOptions options = new() { ForegroundColor = color, ProgressCharacter = '─' };
        return new ProgressBar(numAssets, message, options);
    }

    /// <summary>
    /// Returns an enum value specifying the asset types to extract, based on the command-line options.
    /// </summary>
    /// <returns>An enum value specifying the asset types to extract.</returns>
    private AssetType GetAssetFilter()
    {
        Span<bool> options = stackalloc[] { ExtractGame, ExtractTcg, ExtractResource, ExtractDat, ExtractPack };
        AssetType assetType = 0;

        // Set the enum flag corresponding to each command-line option.
        for (int i = 0; i < options.Length; i++)
        {
            if (options[i])
            {
                assetType |= (AssetType)(1 << i);
            }
        }

        // By default (no options are set), extract all asset types.
        if ((assetType & AssetType.AllDirectories) == 0)
        {
            assetType |= AssetType.AllDirectories;
        }
        if ((assetType & AssetType.AllFiles) == 0)
        {
            assetType |= AssetType.AllFiles;
        }

        return assetType;
    }

    /// <summary>
    /// Throws an exception if the provided command-line arguments are invalid.
    /// </summary>
    /// <returns><see langword="true"/> if the arguments are valid; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="Exception"></exception>
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
