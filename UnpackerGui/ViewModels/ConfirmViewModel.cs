using FluentIcons.Common;
using ReactiveUI;
using System.Reactive;

namespace UnpackerGui.ViewModels;

public class ConfirmViewModel() : ViewModelBase
{
    public string Title { get; init; } = "Confirmation";
    public string Message { get; init; } = "";
    public Icon Icon { get; init; } = Icon.QuestionCircle;
    public ReactiveCommand<Unit, bool>? CheckBoxCommand { get; init; }
    public string CheckBoxMessage { get; init; } = "";
    public bool IsChecked { get; init; } = false;
    public bool ShowCheckBox { get; init; } = false;
}
