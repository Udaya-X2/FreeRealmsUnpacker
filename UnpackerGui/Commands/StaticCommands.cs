using ReactiveUI;
using System.Diagnostics;
using System.Reactive;
using UnpackerGui.Models;

namespace UnpackerGui.Commands;

public static class StaticCommands
{
    public static ReactiveCommand<string, Unit> CopyCommand { get; }
    public static ReactiveCommand<string, Unit> OpenFileCommand { get; }
    public static ReactiveCommand<AssetInfo, Unit> OpenAssetCommand { get; }

    static StaticCommands()
    {
        CopyCommand = ReactiveCommand.CreateFromTask<string>(App.SetClipboardText);
        OpenFileCommand = ReactiveCommand.Create<string>(OpenFile);
        OpenAssetCommand = ReactiveCommand.Create<AssetInfo>(static x => x.Open());
    }

    private static void OpenFile(string path) => Process.Start(new ProcessStartInfo
    {
        UseShellExecute = true,
        FileName = path
    });
}
