using FluentIcons.Common;

namespace UnpackerGui.ViewModels;

public class ConfirmViewModel() : ViewModelBase
{
    public string Title { get; init; } = "Confirmation";
    public string Message { get; init; } = "";
    public Icon Icon { get; init; } = Icon.QuestionCircle;
}
