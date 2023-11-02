using AssetIO;
using DynamicData;
using DynamicData.Binding;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reactive.Linq;
using UnpackerGui.Collections;
using UnpackerGui.Models;

namespace UnpackerGui.ViewModels;

public class AssetFileViewModel : ViewModelBase
{
    public List<AssetInfo> Assets { get; }
    public ulong Size { get; }

    private readonly ReadOnlyObservableCollection<DataFileViewModel>? _dataFiles;
    private readonly ReactiveList<string> _dataFilePaths;
    private readonly AssetFile _assetFile;

    private bool _isChecked;

    public AssetFileViewModel(string path, AssetType assetType)
    {
        Assets = new List<AssetInfo>();
        _dataFilePaths = new ReactiveList<string>();
        _assetFile = new AssetFile(path, assetType, _dataFilePaths);

        foreach (Asset asset in _assetFile)
        {
            Assets.Add(new AssetInfo(asset, _assetFile));
            Size += asset.Size;
        }

        if (_assetFile.FileType == AssetType.Dat)
        {
            _dataFilePaths.AddRange(ClientDirectory.EnumerateDataFiles(_assetFile.Info));
            _dataFilePaths.ToObservableChangeSet()
                          .Transform(x => new DataFileViewModel(x, this))
                          .Bind(out _dataFiles)
                          .Subscribe();
        }
    }

    protected AssetFileViewModel(AssetFileViewModel assetFile)
    {
        Assets = assetFile.Assets;
        Size = assetFile.Size;
        _dataFiles = null;
        _dataFilePaths = assetFile._dataFilePaths;
        _assetFile = assetFile._assetFile;
    }

    public bool IsChecked
    {
        get => _isChecked;
        set => this.RaiseAndSetIfChanged(ref _isChecked, value);
    }

    public virtual string Name => _assetFile.Name;

    public virtual string FullName => _assetFile.FullName;

    public ReadOnlyObservableCollection<DataFileViewModel>? DataFiles => _dataFiles;

    public int Count => Assets.Count;

    public AssetReader OpenRead() => _assetFile.OpenRead();

    public bool Contains(AssetInfo asset) => _assetFile == asset.AssetFile;
}
