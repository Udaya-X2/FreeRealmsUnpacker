using McMaster.Extensions.CommandLineUtils;
using System.ComponentModel.DataAnnotations;

namespace FreeRealmsUnpacker
{
    partial class Unpacker
    {
        [Argument(0, Description = "The Free Realms client directory."), Required]
        public string InputDirectory { get; } = "";

        [Argument(1, Description = "The destination for extracted assets.")]
        public string OutputDirectory { get; } = "./assets";

        [Option(ShortName = "g", Description = "Extract game assets only (in 'Free Realms/')")]
        public bool ExtractGame { get; }

        [Option(ShortName = "t", Description = "Extract TCG assets only (in 'Free Realms/assets/')")]
        public bool ExtractTCG { get; }

        [Option(ShortName = "r", Description = "Extract resource assets only (in 'Free Realms/tcg/')")]
        public bool ExtractResource { get; }

        [Option(ShortName = "l", Description = "List the assets without extracting them.")]
        public bool ListAssets { get; }

        [Option(ShortName = "s", Description = "Skip extracting assets that already exist.")]
        public bool SkipExisting { get; }

        [Option(ShortName = "n", Description = "Don't show progress bars.")]
        public bool NoProgressBars { get; }

        [Option(ShortName = "y", Description = "Automatically answer yes to any question.")]
        public bool AnswerYes { get; }

        [Option(ShortName = "d", Description = "Show complete exception stack traces.")]
        public bool Debug { get; }

        public static int Main(string[] args) => CommandLineApplication.Execute<Unpacker>(args);
    }
}
