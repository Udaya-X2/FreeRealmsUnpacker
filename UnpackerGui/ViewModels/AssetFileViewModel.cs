using AssetIO;
using ReactiveUI;
using System.Collections.Generic;
using UnpackerGui.Models;

namespace UnpackerGui.ViewModels;

public class AssetFileViewModel : ViewModelBase
{
    /// <summary>
    /// The assets in the asset file.
    /// </summary>
    public List<AssetInfo> Assets { get; }

    /// <summary>
    /// The total size of all assets in the asset file.
    /// </summary>
    public ulong Size { get; }

    private readonly AssetFile _assetFile;

    private bool _isChecked;

    public AssetFileViewModel(AssetFile assetFile)
    {
        _assetFile = assetFile;
        Assets = new List<AssetInfo>();

        foreach (Asset asset in _assetFile)
        {
            Assets.Add(new AssetInfo(asset, _assetFile));
            Size += asset.Size;
        }
    }

    public bool IsChecked
    {
        get => _isChecked;
        set => this.RaiseAndSetIfChanged(ref _isChecked, value);
    }

    public string Name => _assetFile.Name;

    public string FullName => _assetFile.FullName;

    public int Count => Assets.Count;

    public AssetReader OpenRead() => _assetFile.OpenRead();

    public bool Contains(AssetInfo asset) => _assetFile == asset.AssetFile;
}
