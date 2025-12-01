using System;

namespace UnpackerGui.ViewModels;

public class InputViewModel : ViewModelBase
{
    public string Title { get; init; } = "Input";
    public string Prompt { get; init; } = "Enter Value:";
    public string Value { get; init; } = "";
    public Predicate<string?> IsValid { get; init; } = static _ => true;
}
