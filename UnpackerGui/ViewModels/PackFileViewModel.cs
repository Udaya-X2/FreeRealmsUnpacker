using AssetIO;
using ReactiveUI;

namespace UnpackerGui.ViewModels;

public class PackFileViewModel : ViewModelBase
{
    private bool _isChecked;

    public PackFileViewModel(string path)
    {
        Path = path;
        Assets = ClientFile.GetPackAssets(path);
    }

    public string Path { get; }

    public Asset[] Assets { get; }

    public string Name => System.IO.Path.GetFileName(Path);

    public bool IsChecked
    {
        get => _isChecked;
        set => this.RaiseAndSetIfChanged(ref _isChecked, value);
    }
}
