using FluentIcons.Common;

namespace UnpackerGui.ViewModels;

public class ConfirmViewModel(string message, Icon icon) : ViewModelBase
{
    public string Message { get; } = message;
    public Icon Icon { get; } = icon;
}
