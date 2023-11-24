using ReactiveUI;
using System.IO;

namespace UnpackerGui.ViewModels;

public class DataFileViewModel : ViewModelBase
{
    public AssetFileViewModel Parent { get; }
    public string FullName { get; }
    public string Name { get; }

    private bool _isChecked;

    public DataFileViewModel(string path, AssetFileViewModel assetFile)
    {
        Parent = assetFile;
        FullName = path;
        Name = Path.GetFileName(path);
    }

    public bool IsChecked
    {
        get => _isChecked;
        set => this.RaiseAndSetIfChanged(ref _isChecked, value);
    }
}
