using AssetIO;
using ReactiveUI;

namespace UnpackerGui.ViewModels;

public class AssetFileViewModel : ViewModelBase
{
    public AssetFile AssetFile { get; }
    public Asset[] Assets { get; }

    private bool _isChecked;

    public AssetFileViewModel(AssetFile assetFile)
    {
        AssetFile = assetFile;
        Assets = assetFile.Assets;
    }

    public bool IsChecked
    {
        get => _isChecked;
        set => this.RaiseAndSetIfChanged(ref _isChecked, value);
    }

    public string Name => AssetFile.Name;

    public string FullName => AssetFile.FullName;
}
