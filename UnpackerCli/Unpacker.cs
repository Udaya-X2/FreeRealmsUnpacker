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
            IEnumerable<AssetFile> assetFiles = Directory.Exists(InputFile)
                                              ? ClientDirectory.EnumerateAssetFiles(InputFile,
                                                                                    GetAssetFilter(),
                                                                                    requireFullType: !ExtractUnknown)
                                              : new[] { new AssetFile(InputFile) };
            
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
    /// Extracts assets in the specified asset file to the output directory.
    /// </summary>
    /// <returns>The number of assets in the asset file.</returns>
    private int ExtractAssets(AssetFile assetFile, ProgressBar? pbar)
    {
        using AssetReader reader = assetFile.OpenRead();
        int numAssets = 0;

        foreach (Asset asset in assetFile)
        {
            if (TryGetExtractionPath(reader, asset, out string path))
            {
                FileInfo file = new(path);
                file.Directory?.Create();
                using FileStream fs = file.Open(FileMode.Create, FileAccess.Write, FileShare.Read);
                reader.CopyTo(asset, fs);
                pbar?.UpdateProgress($"Extracted {asset.Name}");
            }
            else if (HandleConflicts is ConflictOptions.Skip)
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
    /// Determines where to extract the specified asset. A return value indicates
    /// whether the path is valid, according to the command-line options.
    /// </summary>
    /// <returns>The path to extract the specified asset.</returns>
    /// <exception cref="InvalidEnumArgumentException"/>
    private bool TryGetExtractionPath(AssetReader reader, Asset asset, out string path)
    {
        path = Path.Combine(OutputDirectory, asset.Name);

        switch (HandleConflicts)
        {
            case ConflictOptions.Overwrite:
                break;
            case ConflictOptions.Skip:
                return !File.Exists(path);
            case ConflictOptions.Rename:
                {
                    string? newPath = path, extension = null, pathWithoutExtension = null;

                    for (int digit = 2; File.Exists(newPath); digit++)
                    {
                        if (AssetEquals(reader, asset, newPath)) return false;

                        extension ??= Path.GetExtension(path);
                        pathWithoutExtension ??= path[..^extension.Length];
                        newPath = $"{pathWithoutExtension} ({digit}){extension}";
                    }

                    path = newPath;
                }
                break;
            case ConflictOptions.MkDir:
                for (int digit = 2; File.Exists(path); digit++)
                {
                    if (AssetEquals(reader, asset, path)) return false;

                    path = Path.Combine($"{OutputDirectory} ({digit})", asset.Name);
                }
                break;
            case ConflictOptions.MkSubdir:
                if (File.Exists(path))
                {
                    if (AssetEquals(reader, asset, path)) return false;

                    string extension = Path.GetExtension(asset.Name);
                    MoveFileDown(path, $"1{extension}");
                    path = Path.Combine(path, $"2{extension}");
                }
                else if (Directory.Exists(path))
                {
                    string extension = Path.GetExtension(asset.Name);
                    string subdirAsset = Path.Combine(path, $"1{extension}");

                    for (int digit = 2; File.Exists(subdirAsset); digit++)
                    {
                        if (AssetEquals(reader, asset, subdirAsset)) return false;

                        subdirAsset = Path.Combine(path, $"{digit}{extension}");
                    }

                    path = subdirAsset;
                }
                break;
            case ConflictOptions.MkTree:
                if (File.Exists(path))
                {
                    if (AssetEquals(reader, asset, path)) return false;

                    string fileName = Path.GetFileName(asset.Name);
                    MoveFileDown(path, Path.Combine("1", fileName));
                    path = Path.Combine(path, "2", fileName);
                }
                else if (Directory.Exists(path))
                {
                    string fileName = Path.GetFileName(asset.Name);
                    string subdirAsset = Path.Combine(path, "1", fileName);

                    for (int digit = 2; File.Exists(subdirAsset); digit++)
                    {
                        if (AssetEquals(reader, asset, subdirAsset)) return false;

                        subdirAsset = Path.Combine(path, $"{digit}", fileName);
                    }

                    path = subdirAsset;
                }
                break;
            default:
                throw new InvalidEnumArgumentException(nameof(HandleConflicts),
                                                       (int)HandleConflicts,
                                                       HandleConflicts.GetType());
        }

        return true;
    }

    /// <summary>
    /// Determines whether the specified asset is equal to the given file.
    /// </summary>
    /// <returns>
    /// <see langword="true"/> if the specified asset equals the specified file; otherwise, <see langword="false"/>.
    /// </returns>
    private static bool AssetEquals(AssetReader reader, Asset asset, string path)
    {
        FileInfo file = new(path);

        if (asset.Size != file.Length) return false;

        using FileStream fs = file.OpenRead();
        return reader.StreamEqualsAsync(asset, fs).Result;
    }

    /// <summary>
    /// Replaces the specified file with a directory and moves
    /// the file inside of the directory with the given name.
    /// </summary>
    private static void MoveFileDown(string path, string fileName)
    {
        string tempPath = Path.GetTempFileName();
        File.Move(path, tempPath, overwrite: true);
        FileInfo file = new(Path.Combine(path, fileName));
        file.Directory?.Create();
        File.Move(tempPath, file.FullName);
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
        if (NoProgressBars || ListAssets || ListFiles || CountAssets) return null;

        (string message, ConsoleColor color) = assetType switch
        {
            AssetType.Game => ("Reading game assets...", ConsoleColor.Green),
            AssetType.Tcg => ("Reading TCG assets...", ConsoleColor.Blue),
            AssetType.Resource => ("Reading resource assets...", ConsoleColor.DarkYellow),
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
        if (ExtractDat) assetType |= AssetType.Dat;
        if (ExtractPack) assetType |= AssetType.Pack;

        // By default (no options are set), extract all asset types.
        if (assetType.GetDirectoryType() == 0 && !ExtractUnknown)
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
    private bool ValidateArguments()
    {
        if (!Enum.IsDefined(HandleConflicts)) throw new Exception("Invalid value specified for handle-conflicts.");
        return (ListAssets, ListFiles, ValidateAssets, CountAssets, DisplayCsv, DisplayTable) switch
        {
            (true, true, _, _, _, _) => throw new Exception("Cannot both list assets and files."),
            (true, _, true, _, _, _) => throw new Exception("Cannot both list and validate assets."),
            (_, true, true, _, _, _) => throw new Exception("Cannot both list files and validate assets."),
            (true, _, _, true, _, _) => throw new Exception("Cannot both list and count assets."),
            (_, true, _, true, _, _) => throw new Exception("Cannot both list files and count assets."),
            (_, _, true, true, _, _) => throw new Exception("Cannot both validate and count assets."),
            (_, _, _, _, true, true) => throw new Exception("Cannot display info as both a CSV file and a table."),
            (false, false, _, _, true, false) => throw new Exception("Cannot display CSV without a list option."),
            (false, false, _, _, false, true) => throw new Exception("Cannot display table without a list option."),
            (false, false, false, false, false, false) => SetupOutputDirectory(),
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
