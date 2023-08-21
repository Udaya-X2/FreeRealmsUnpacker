using McMaster.Extensions.CommandLineUtils;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace FreeRealmsUnpacker
{
    partial class Unpacker
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

        [Option(ShortName = "q", LongName = "quiet", Description = "Don't show progress bars.")]
        public bool HideProgressBars { get; }

        [Option(ShortName = "y", Description = "Automatically answer yes to any question.")]
        public bool AnswerYes { get; }

        public static int Main(string[] args) => CommandLineApplication.Execute<Unpacker>(args);
    }
}
