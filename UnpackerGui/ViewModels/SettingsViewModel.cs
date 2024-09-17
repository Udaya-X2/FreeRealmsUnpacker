using ReactiveUI;

namespace UnpackerGui.ViewModels;

public class SettingsViewModel : ViewModelBase
{
    private string _name;
    private string _description;

    public SettingsViewModel(string name, string description)
    {
        _name = name;
        _description = description;
    }

    public string Name
    {
        get => _name;
        set => this.RaiseAndSetIfChanged(ref _name, value);
    }

    public string Description
    {
        get => _description;
        set => this.RaiseAndSetIfChanged(ref _description, value);
    }
}
