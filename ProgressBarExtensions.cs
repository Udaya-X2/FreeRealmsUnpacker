using ShellProgressBar;

namespace FreeRealmsUnpacker
{
    internal static class ProgressBarExtensions
    {
        /// <summary>
        /// Moves the progress bar one tick and displays the specified message.
        /// </summary>
        public static void UpdateProgress(this ProgressBar progressBar, string message)
        {
            progressBar.Tick($"({progressBar.CurrentTick + 1}/{progressBar.MaxTicks}) {message}");
        }
    }
}
