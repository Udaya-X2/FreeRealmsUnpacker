using AssetIO;
using ReactiveUI;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading;
using UnpackerGui.Collections;
using UnpackerGui.Models;

namespace UnpackerGui.ViewModels;

public class AssetFileViewModel : ViewModelBase, IList<AssetInfo>
{
    public List<AssetInfo> Assets { get; }
    public ReactiveList<DataFileViewModel>? DataFiles { get; }
    public ReactiveList<DataFileViewModel>? SelectedDataFiles { get; }
    public long Size { get; }

    public ReactiveCommand<Unit, bool>? ShowDataFilesCommand { get; }
    public ReactiveCommand<Unit, Unit>? RemoveDataFilesCommand { get; }

    private readonly AssetFile _assetFile;

    private bool _isChecked;
    private bool _showDataFiles;
    private bool _isValidated;
    private DataFileViewModel? _selectedDataFile;

    public AssetFileViewModel(AssetFile assetFile, CancellationToken token = default)
    {
        Assets = [];
        _assetFile = assetFile;

        foreach (Asset asset in _assetFile)
        {
            token.ThrowIfCancellationRequested();
            Assets.Add(new AssetInfo(asset, _assetFile));
            Size += asset.Size;
        }

        if (FileType == AssetType.Dat)
        {
            DataFiles = [.. assetFile.DataFiles.Select(x => new DataFileViewModel(x, this))];
            assetFile.DataFiles = DataFiles.Select(x => x.FullName);
            SelectedDataFiles = [];
            ShowDataFilesCommand = ReactiveCommand.Create(() => ShowDataFiles ^= true);
            RemoveDataFilesCommand = ReactiveCommand.Create(RemoveDataFiles);
        }
    }

    public bool IsChecked
    {
        get => _isChecked;
        set => this.RaiseAndSetIfChanged(ref _isChecked, value);
    }

    public bool ShowDataFiles
    {
        get => _showDataFiles;
        set => this.RaiseAndSetIfChanged(ref _showDataFiles, value);
    }

    public bool IsValidated
    {
        get => _isValidated;
        set => this.RaiseAndSetIfChanged(ref _isValidated, value);
    }

    public DataFileViewModel? SelectedDataFile
    {
        get => _selectedDataFile;
        set => this.RaiseAndSetIfChanged(ref _selectedDataFile, value);
    }

    public string Name => _assetFile.Name;

    public string FullName => _assetFile.FullName;

    public FileInfo Info => _assetFile.Info;

    public AssetType FileType => _assetFile.FileType;

    public AssetType DirectoryType => _assetFile.DirectoryType;

    public int Count => Assets.Count;

    public bool IsReadOnly => ((IList<AssetInfo>)Assets).IsReadOnly;

    public AssetInfo this[int index]
    {
        get => Assets[index];
        set => Assets[index] = value;
    }

    public AssetReader OpenRead() => _assetFile.OpenRead();

    public int IndexOf(AssetInfo item) => Assets.IndexOf(item);

    public void Insert(int index, AssetInfo item) => Assets.Insert(index, item);

    public void RemoveAt(int index) => Assets.RemoveAt(index);

    public void Add(AssetInfo item) => Assets.Add(item);

    public void Clear() => Assets.Clear();

    public bool Contains(AssetInfo item) => Assets.Contains(item);

    public void CopyTo(AssetInfo[] array, int arrayIndex) => Assets.CopyTo(array, arrayIndex);

    public bool Remove(AssetInfo item) => Assets.Remove(item);

    public IEnumerator<AssetInfo> GetEnumerator() => Assets.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => Assets.GetEnumerator();

    /// <summary>
    /// Removes the selected data files from the asset file.
    /// </summary>
    private void RemoveDataFiles()
    {
        DataFileViewModel[] dataFiles = [.. SelectedDataFiles];
        SelectedDataFiles!.Clear();
        DataFiles!.RemoveMany(dataFiles);
    }
}
