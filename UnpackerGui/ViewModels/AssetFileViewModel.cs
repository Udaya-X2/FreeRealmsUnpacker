using AssetIO;
using DynamicData;
using DynamicData.Binding;
using ReactiveUI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Reactive.Linq;
using UnpackerGui.Collections;
using UnpackerGui.Models;

namespace UnpackerGui.ViewModels;

public class AssetFileViewModel : ViewModelBase, IList<AssetInfo>
{
    public List<AssetInfo> Assets { get; }
    public ReactiveList<string> DataFilePaths { get; }
    public long Size { get; }

    private readonly ReadOnlyObservableCollection<DataFileViewModel>? _dataFiles;
    private readonly AssetFile _assetFile;

    private bool _isChecked;

    public AssetFileViewModel(string path, AssetType assetType)
    {
        Assets = new List<AssetInfo>();
        DataFilePaths = new ReactiveList<string>();
        _assetFile = new AssetFile(path, assetType, DataFilePaths);

        foreach (Asset asset in _assetFile)
        {
            Assets.Add(new AssetInfo(asset, _assetFile));
            Size += asset.Size;
        }

        if (_assetFile.FileType == AssetType.Dat)
        {
            DataFilePaths.AddRange(ClientDirectory.EnumerateDataFiles(_assetFile.Info));
            DataFilePaths.ToObservableChangeSet()
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
        DataFilePaths = assetFile.DataFilePaths;
        _assetFile = assetFile._assetFile;
    }

    public bool IsChecked
    {
        get => _isChecked;
        set => this.RaiseAndSetIfChanged(ref _isChecked, value);
    }

    public ReadOnlyObservableCollection<DataFileViewModel>? DataFiles => _dataFiles;

    public virtual string Name => _assetFile.Name;

    public virtual string FullName => _assetFile.FullName;

    public FileInfo Info => _assetFile.Info;

    public AssetType FileType => _assetFile.FileType;

    public AssetReader OpenRead() => _assetFile.OpenRead();

    public bool Contains(AssetInfo asset) => _assetFile == asset.AssetFile;

    public int Count => Assets.Count;

    public bool IsReadOnly => ((IList<AssetInfo>)Assets).IsReadOnly;

    public AssetInfo this[int index] { get => Assets[index]; set => Assets[index] = value; }

    public int IndexOf(AssetInfo item) => Assets.IndexOf(item);

    public void Insert(int index, AssetInfo item) => Assets.Insert(index, item);

    public void RemoveAt(int index) => Assets.RemoveAt(index);

    public void Add(AssetInfo item) => Assets.Add(item);

    public void Clear() => Assets.Clear();

    public void CopyTo(AssetInfo[] array, int arrayIndex) => Assets.CopyTo(array, arrayIndex);

    public bool Remove(AssetInfo item) => Assets.Remove(item);

    public IEnumerator<AssetInfo> GetEnumerator() => Assets.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
