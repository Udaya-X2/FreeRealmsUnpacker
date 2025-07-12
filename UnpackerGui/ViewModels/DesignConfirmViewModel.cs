using FluentIcons.Common;
using ReactiveUI;
using System.Reactive;

namespace UnpackerGui.ViewModels;

public class DesignConfirmViewModel : ConfirmViewModel
{
    public override string Title { get; init; } = "Confirmation";
    public override string Message { get; init; } = """
        Are you sure you want to permanently delete this file?

        Assets_manifest.dat
        Type: Game, Dat
        Count: 42979
        Size: 626.88 MB
        Location: C:\Program Files\Sony Online Entertainment\Installed Games\Free Realms
        """;
    //public override string Message { get; init; } = """
    //    Are you sure you want to permanently delete this file?

    //    Assets_000.pack
    //    Type: Game, Pack
    //    Count: 6120
    //    Size: 48.35 MB
    //    Location: C:\Program Files\Sony Online Entertainment\Installed Games\Free Realms
    //    """;
    //public override string Message { get; init; } = """
    //    Are you sure you want to permanently delete this file?

    //    Assets_000.dat
    //    Size: 200 MB
    //    Location: C:\Program Files\Sony Online Entertainment\Installed Games\Free Realms
    //    """;
    public override Icon Icon { get; init; } = Icon.QuestionCircle;
    public override ReactiveCommand<Unit, bool>? CheckBoxCommand { get; init; }
    public override string CheckBoxMessage { get; init; } = "Delete asset .dat files";
    public override bool IsChecked { get; init; } = true;
    public override bool ShowCheckBox { get; init; } = true;
}
