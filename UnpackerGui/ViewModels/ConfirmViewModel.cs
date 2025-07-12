using FluentIcons.Common;
using ReactiveUI;
using System.Reactive;

namespace UnpackerGui.ViewModels;

public class ConfirmViewModel() : ViewModelBase
{
    public virtual string Title { get; init; } = "Confirmation";
    public virtual string Message { get; init; } = "";
    public virtual Icon Icon { get; init; } = Icon.QuestionCircle;
    public virtual ReactiveCommand<Unit, bool>? CheckBoxCommand { get; init; }
    public virtual string CheckBoxMessage { get; init; } = "";
    public virtual bool IsChecked { get; init; } = false;
    public virtual bool ShowCheckBox { get; init; } = false;
}
