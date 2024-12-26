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

            // Use UTF-8 encoding to display tables properly.
            Stopwatch sw = Stopwatch.StartNew();
            Console.OutputEncoding = Encoding.UTF8;

            // Print the header line, if displaying information as a CSV file.
            if (DisplayCsv)
            {
                Console.WriteLine(ListAssets ? "Name,Offset,Size,CRC-32" : "Name,Count,Size");
            }

            // Get the asset files to process from the input directory or file.
            IEnumerable<AssetFile> assetFiles = (Directory.Exists(InputFile), !FixErrors) switch
            {
                (true, true) => ClientDirectory.EnumerateAssetFiles(InputFile,
                                                                    GetAssetFilter(),
                                                                    requireFullType: !ExtractUnknown),
                (true, false) => ClientDirectory.EnumerateTempFiles(InputFile,
                                                                    AssetType.Pack | GetAssetFilter(),
                                                                    requireFullType: !ExtractUnknown),
                (false, true) => [new AssetFile(InputFile)],
                (false, false) => [new TempAssetFile(InputFile)]
            };

            // Handle the asset types specified.
            int count = ListFiles
                      ? ListFilesFormatted(assetFiles)
                      : assetFiles.OrderBy(x => x.DirectoryType)
                                  .GroupBy(x => x.DirectoryType)
                                  .Sum(x => HandleAssets(x.Key, x));

            // Print the number of assets or files found.
            string message = (count, ListFiles) switch
            {
                (0, false) => $"\nNo assets found.",
                (0, true) => $"\nNo asset files found.",
                (1, false) => $"\n1 asset found in {sw.Elapsed:hh\\:mm\\:ss\\.fff}",
                (1, true) => $"\n1 asset file found in {sw.Elapsed:hh\\:mm\\:ss\\.fff}",
                (_, false) => $"\n{count} assets found in {sw.Elapsed:hh\\:mm\\:ss\\.fff}",
                (_, true) => $"\n{count} asset files found in {sw.Elapsed:hh\\:mm\\:ss\\.fff}"
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
            else if (FixErrors)
            {
                numAssets += TryFixAssetFile((TempAssetFile)assetFile);
            }
            else
            {
                numAssets += ExtractAssets(assetFile, pbar);
            }
        }

        return numAssets;
    }

    /// <summary>
    /// Lists the specified asset files in tabular, CSV, or
    /// line-based form, depending on the command-line options.
    /// </summary>
    /// <returns>The number of asset files.</returns>
    private int ListFilesFormatted(IEnumerable<AssetFile> assetFiles)
    {
        if (DisplayTable)
        {
            Table table = new("Name", "Count", "Size");

            foreach (AssetFile assetFile in assetFiles)
            {
                string size = FormatFileSize(assetFile.Info.Length);
                table.AddRow(assetFile.FullName, assetFile.Count, size);
            }

            table.Print();
            return table.Count;
        }
        else
        {
            int numAssets = 0;

            foreach (AssetFile assetFile in assetFiles)
            {
                string size = FormatFileSize(assetFile.Info.Length);
                string value = DisplayCsv
                             ? $"{EscapeCsvString(assetFile.FullName)},{assetFile.Count},{size}"
                             : assetFile.FullName;
                Console.WriteLine(value);
                numAssets++;
            }

            return numAssets;
        }
    }

    /// <summary>
    /// Lists assets from the specified asset file in tabular, CSV,
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
            Console.WriteLine($"\nFound {table.Count} asset{plural} in {assetFile.FullName}:\n");
            table.Print();
            return table.Count;
        }
        else
        {
            int numAssets = 0;

            foreach (Asset asset in assetFile)
            {
                string value = DisplayCsv
                             ? $"{EscapeCsvString(asset.Name)},{asset.Offset},{asset.Size},{asset.Crc32}"
                             : asset.Name;
                Console.WriteLine(value);
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
                Console.WriteLine($"'{asset.Name}' in '{assetFile.FullName}' does not match the expected CRC.");
            }

            numAssets++;
        }

        return numAssets;
    }

    /// <summary>
    /// Attempts to fix the specified asset file.
    /// </summary>
    /// <returns>The number of assets in the fixed asset file.</returns>
    private static int TryFixAssetFile(TempAssetFile assetFile)
    {
        string oldPath = assetFile.FullName;

        if (assetFile.TryFixAndRename(out AssetFile? newAssetFile))
        {
            if (oldPath != newAssetFile.FullName)
            {
                Console.WriteLine($"Fixed '{oldPath}' -> {newAssetFile.Name}");
            }
            else
            {
                Console.WriteLine($"Fixed '{assetFile.FullName}'");
            }

            return newAssetFile.Count;
        }

        Console.WriteLine($"Unable to fix '{assetFile.FullName}'");
        return assetFile.Count;
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
            reader.ExtractTo(asset, OutputDirectory, HandleConflicts, out bool fileExtracted);

            if (fileExtracted)
            {
                pbar?.UpdateProgress($"Extracted {asset.Name}");
            }
            else if (HandleConflicts is FileConflictOptions.Skip)
            {
                pbar?.UpdateProgress($"Skipped {asset.Name}");
            }
            else
            {
                pbar?.UpdateProgress($"Skipped duplicate {asset.Name}");
            }

            numAssets++;
        }

        return numAssets;
    }

    /// <summary>
    /// Converts a string to a CSV-compatible cell.
    /// </summary>
    /// <returns>The CSV cell formatted string.</returns>
    private static string EscapeCsvString(string value)
    {
        if (value.StartsWith(' ') || value.EndsWith(' ') || value.Any(x => x is ',' or '"' or '\r' or '\n'))
        {
            StringBuilder sb = new(2 * value.Length + 2);
            sb.Append('"');

            foreach (char c in value)
            {
                if (c == '"')
                {
                    sb.Append(c);
                }

                sb.Append(c);
            }

            sb.Append('"');
            return sb.ToString();
        }

        return value;
    }

    /// <summary>
    /// Formats the specified number as a file size string.
    /// </summary>
    /// <returns>The formatted string.</returns>
    private static string FormatFileSize(long i) => (i < 0 ? -i : i) switch
    {
        >= 1L << 60 => $"{(i >> 50) / 1024.0:0.##} EB",
        >= 1L << 50 => $"{(i >> 40) / 1024.0:0.##} PB",
        >= 1L << 40 => $"{(i >> 30) / 1024.0:0.##} TB",
        >= 1L << 30 => $"{(i >> 20) / 1024.0:0.##} GB",
        >= 1L << 20 => $"{(i >> 10) / 1024.0:0.##} MB",
        >= 1L << 10 => $"{i / 1024.0:0.##} KB",
        _ => $"{i} B"
    };

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
        if (NoProgressBars || ListAssets || ListFiles || CountAssets || FixErrors) return null;

        (string message, ConsoleColor color) = assetType switch
        {
            AssetType.Game => ("Reading game assets...", ConsoleColor.Green),
            AssetType.Tcg => ("Reading TCG assets...", ConsoleColor.Cyan),
            AssetType.Resource => ("Reading resource assets...", ConsoleColor.Blue),
            AssetType.PS3 => ("Reading PS3 assets...", ConsoleColor.Yellow),
            _ => ("Reading unknown assets...", ConsoleColor.Magenta)
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
        if (ExtractPS3) assetType |= AssetType.PS3;
        if (ExtractPack) assetType |= AssetType.Pack;
        if (ExtractDat) assetType |= AssetType.Dat;

        // By default (no options are set), extract all asset types.
        if (assetType.GetDirectoryType() == 0 && !ExtractUnknown)
        {
            assetType |= AssetType.AllDirectories;
        }
        if (assetType.GetFileType() == 0 && !FixErrors)
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
    private bool ValidateArguments()
    {
        if (!Enum.IsDefined(HandleConflicts)) throw new Exception("Invalid value specified for handle-conflicts.");
        if (FixErrors && ExtractPack) throw new Exception("Cannot both fix errors and handle .pack assets.");
        if (FixErrors && ExtractDat) throw new Exception("Cannot both fix errors and handle .dat assets.");

        const bool T = true, F = false;
        return (ListAssets, ListFiles, ValidateAssets, CountAssets, DisplayCsv, DisplayTable, FixErrors) switch
        {
            (T, T, _, _, _, _, _) => throw new Exception("Cannot both list assets and files."),
            (T, _, T, _, _, _, _) => throw new Exception("Cannot both list and validate assets."),
            (_, T, T, _, _, _, _) => throw new Exception("Cannot both list files and validate assets."),
            (T, _, _, T, _, _, _) => throw new Exception("Cannot both list and count assets."),
            (_, T, _, T, _, _, _) => throw new Exception("Cannot both list files and count assets."),
            (_, _, T, T, _, _, _) => throw new Exception("Cannot both validate and count assets."),
            (_, _, _, _, T, T, _) => throw new Exception("Cannot display info as both a CSV file and a table."),
            (F, F, _, _, T, F, _) => throw new Exception("Cannot display CSV without a list option."),
            (F, F, _, _, F, T, _) => throw new Exception("Cannot display table without a list option."),
            (F, F, F, F, F, F, F) => SetupOutputDirectory(),
            _ => true,
        };
    }

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
