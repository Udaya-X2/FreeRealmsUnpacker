using McMaster.Extensions.CommandLineUtils;
using ShellProgressBar;

namespace FreeRealmsUnpacker
{
    public partial class Unpacker
    {
        /// <summary>
        /// The entry point of <see href="FreeRealmsUnpacker"/>, following command parsing.
        /// </summary>
        /// <returns>The process exit code.</returns>
        public int OnExecute()
        {
            try
            {
                // Set up the input and output directories.
                if (!SetupInputDirectory() || !SetupOutputDirectory()) return 1;

                // By default, extract all assets.
                bool extractAll = !ExtractGame && !ExtractTCG && !ExtractResource;

                // Extract the asset type(s) specified to the output directory.
                if (extractAll || ExtractGame) ExtractAssets(AssetType.Game);
                if (extractAll || ExtractTCG) ExtractAssets(AssetType.TCG);
                if (extractAll || ExtractResource) ExtractAssets(AssetType.Resource);
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Error.WriteLine(Debug ? $"\n{ex}" : $"ERROR - {ex.Message}");
                Console.ResetColor();
                return 2;
            }

            return 0;
        }

        /// <summary>
        /// Extracts assets of the specified type to the output directory.
        /// </summary>
        private void ExtractAssets(AssetType assetType)
        {
            Asset[] clientAssets = ManifestReader.GetClientAssets(InputDirectory, assetType);
            byte[] buffer = new byte[clientAssets.Select(x => x.Size).DefaultIfEmpty().Max()];
            using AssetPackReader reader = new(InputDirectory, assetType);
            using ProgressBar? pbar = GetExtractionProgressBar(clientAssets.Length, assetType);
            CreateDirectoryStructure(clientAssets, assetType);

            foreach (Asset asset in clientAssets)
            {
                reader.Read(asset, buffer);
                using FileStream assetWriter = File.Open($"{OutputDirectory}/{asset.Name}", FileMode.Create);
                assetWriter.Write(buffer, 0, asset.Size);
                UpdateProgress(pbar, $"Extracted {asset.Name}");
            }
        }

        /// <summary>
        /// Creates a <see cref="ProgressBar"/> with the specified number of assets
        /// for max ticks and an <paramref name="assetType"/>-dependent color.
        /// </summary>
        /// <returns>
        /// A <see cref="ProgressBar"/> with the number of assets for max ticks and an
        /// <paramref name="assetType"/>-dependent color, or null if the number of assets is 0.
        /// </returns>
        /// <exception cref="ArgumentException"></exception>
        private ProgressBar? GetExtractionProgressBar(int numAssets, AssetType assetType) => assetType switch
        {
            _ when numAssets == 0 => null,
            AssetType.Game => CreateProgressBar(numAssets, "Reading game assets...", ConsoleColor.Green),
            AssetType.TCG => CreateProgressBar(numAssets, "Reading TCG assets...", ConsoleColor.Blue),
            AssetType.Resource => CreateProgressBar(numAssets, "Reading resource assets...", ConsoleColor.DarkYellow),
            _ => throw new ArgumentException("Invalid enum value for extraction type", nameof(assetType))
        };

        /// <summary>
        /// Creates the directory structure used by the client assets in the output directory.
        /// </summary>
        private void CreateDirectoryStructure(Asset[] clientAssets, AssetType assetType)
        {
            // Only TCG assets require subdirectories.
            if (assetType == AssetType.TCG)
            {
                foreach (string? assetDir in clientAssets.Select(x => Path.GetDirectoryName(x.Name)).ToHashSet())
                {
                    Directory.CreateDirectory($"{OutputDirectory}/{assetDir ?? ""}");
                }
            }
        }

        /// <summary>
        /// Checks whether the input directory exists.
        /// </summary>
        /// <returns>True if the input directory exists.</returns>
        private bool SetupInputDirectory()
        {
            if (!Directory.Exists(InputDirectory))
            {
                Console.Error.WriteLine($"{InputDirectory} is not a directory.");
                return false;
            }

            return true;
        }

        /// <summary>
        /// Prompts the user to create the output directory or remove files from it if the directory is not empty.
        /// </summary>
        /// <returns>True if the output directory was set up successfully.</returns>
        private bool SetupOutputDirectory()
        {
            DirectoryInfo dir = new(OutputDirectory);

            if (!dir.Exists)
            {
                if (PromptUser($"{OutputDirectory} does not exist. Create the directory?"))
                {
                    dir.Create();
                }
                else
                {
                    return false;
                }
            }
            else if ((dir.EnumerateFiles().Any() || dir.EnumerateDirectories().Any())
                     && PromptUser($"{OutputDirectory} contains files. Delete them?"))
            {
                IEnumerable<FileInfo> files = dir.EnumerateFiles();
                IEnumerable<DirectoryInfo> subdirs = dir.EnumerateDirectories();
                using ProgressBar? pbar = CreateProgressBar(files.Count() + subdirs.Count(),
                                                            "Deleting files...",
                                                            ConsoleColor.Red);

                foreach (FileInfo file in files)
                {
                    file.Delete();
                    UpdateProgress(pbar, $"Deleted {file.Name}");
                }
                foreach (DirectoryInfo subdir in subdirs)
                {
                    subdir.Delete(true);
                    UpdateProgress(pbar, $"Deleted {subdir.Name}");
                }
            }

            return true;
        }

        /// <summary>
        /// Gets a yes/no response from the console after displaying a <paramref name="prompt"/>.
        /// </summary>
        /// <returns>True if the answer is 'yes', or --answer-yes is enabled.</returns>
        private bool PromptUser(string prompt) => AnswerYes || Prompt.GetYesNo(prompt, false);

        /// <summary>
        /// Creates a <see cref="ProgressBar"/> with the specified parameters.
        /// </summary>
        /// <returns>A <see cref="ProgressBar"/> with the specified parameters, or null if --quiet is enabled.</returns>
        private ProgressBar? CreateProgressBar(int maxTicks, string message, ConsoleColor color)
        {
            ProgressBarOptions options = new() { ForegroundColor = color, ProgressCharacter = '─' };
            return HideProgressBars ? null : new ProgressBar(maxTicks, message, options);
        }

        /// <summary>
        /// Moves the progress bar by one tick and displays a message.
        /// </summary>
        private static void UpdateProgress(ProgressBar? pbar, string message)
        {
            pbar?.Tick($"({pbar.CurrentTick + 1})/({pbar.MaxTicks}) {message}");
        }
    }
}
