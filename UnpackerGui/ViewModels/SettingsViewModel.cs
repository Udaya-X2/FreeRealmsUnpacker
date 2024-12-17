using ReactiveUI;

namespace UnpackerGui.ViewModels;

public class SettingsViewModel(string name, string description) : ViewModelBase
{
    public string Name
    {
        get => name;
        set => this.RaiseAndSetIfChanged(ref name, value);
    }

    public string Description
    {
        get => description;
        set => this.RaiseAndSetIfChanged(ref description, value);
    }
}
