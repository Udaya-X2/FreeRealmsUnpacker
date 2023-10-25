using AssetIO;
using ReactiveUI;

namespace UnpackerGui.ViewModels;

public class AssetFileViewModel : ViewModelBase
{
    private readonly AssetFile _assetFile;

    private bool _isChecked;

    public Asset[] Assets { get; }

    public AssetFileViewModel(AssetFile assetFile)
    {
        _assetFile = assetFile;
        Assets = assetFile.Assets;
    }

    public bool IsChecked
    {
        get => _isChecked;
        set => this.RaiseAndSetIfChanged(ref _isChecked, value);
    }

    public string Name => _assetFile.Name;

    public string FullName => _assetFile.FullName;
}
