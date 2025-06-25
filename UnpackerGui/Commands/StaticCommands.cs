using ReactiveUI;
using System.Diagnostics;
using System.Reactive;

namespace UnpackerGui.Commands;

public class StaticCommands
{
    public static ReactiveCommand<string, Unit> CopyCommand { get; }
    public static ReactiveCommand<string, Unit> OpenFileCommand { get; }

    static StaticCommands()
    {
        CopyCommand = ReactiveCommand.CreateFromTask<string>(App.SetClipboardText);
        OpenFileCommand = ReactiveCommand.Create<string>(OpenFile);
    }

    private static void OpenFile(string path) => Process.Start(new ProcessStartInfo
    {
        UseShellExecute = true,
        FileName = path
    });
}
