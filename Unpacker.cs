using McMaster.Extensions.CommandLineUtils;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace FreeRealmsUnpacker
{
    public class Unpacker
    {
        [Argument(0, Description = "The Free Realms client directory."), Required, NotNull]
        public string? InputDirectory { get; }

        [Argument(1, Description = "The destination for extracted assets."), Required, NotNull]
        public string? OutputDirectory { get; }

        [Option(ShortName = "g", Description = "Extract game assets only (in 'Free Realms/')")]
        public bool ExtractGame { get; }

        [Option(ShortName = "t", Description = "Extract TCG assets only (in 'Free Realms/assets/')")]
        public bool ExtractTCG { get; }

        [Option(ShortName = "r", Description = "Extract resource assets only (in 'Free Realms/tcg/')")]
        public bool ExtractResource { get; }

        [Option(ShortName = "y", Description = "Automatically answer yes to any question.")]
        public bool AnswerYes { get; }

        public static int Main(string[] args) => CommandLineApplication.Execute<Unpacker>(args);

        public int OnExecute()
        {
            // Set up the input and output directories.
            if (!SetupInputDirectory() || !SetupOutputDirectory()) return 1;

            // By default, extract all assets.
            bool extractAll = !ExtractGame && !ExtractTCG && !ExtractResource;

            // Extract the asset type(s) specified to the output directory.
            try
            {
                if (extractAll || ExtractGame) ExtractAssets(AssetType.Game);
                if (extractAll || ExtractTCG) ExtractAssets(AssetType.TCG);
                if (extractAll || ExtractResource) ExtractAssets(AssetType.Resource);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex.Message);
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
            byte[] buffer = new byte[clientAssets.Select(x => x.Size).Max()];
            using AssetPackReader reader = new(InputDirectory, assetType);
            CreateDirectoryStructure(clientAssets, assetType);

            foreach (Asset asset in clientAssets)
            {
                reader.Read(asset, buffer);
                using FileStream assetWriter = File.OpenWrite($"{OutputDirectory}/{asset.Name}");
                assetWriter.Write(buffer, 0, asset.Size);
            }
        }

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
        /// Prompts the user to create the output directory if it does not exist, or
        /// to remove any files/directories from it if the directory is not empty.
        /// </summary>
        /// <returns>True if the output directory was set up successfully.</returns>
        private bool SetupOutputDirectory()
        {
            DirectoryInfo dir = new(OutputDirectory);

            try
            {
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
                    foreach (FileInfo file in dir.EnumerateFiles())
                    {
                        file.Delete();
                    }
                    foreach (DirectoryInfo subdir in dir.EnumerateDirectories())
                    {
                        subdir.Delete(true);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex.Message);
                return false;
            }

            return true;
        }

        /// <summary>
        /// Gets a yes/no response from the console after displaying a <paramref name="prompt"/>.
        /// </summary>
        /// <returns>True if --answer-yes is enabled, or the answer is 'yes'.</returns>
        private bool PromptUser(string prompt) => AnswerYes || Prompt.GetYesNo(prompt, false);
    }
}
