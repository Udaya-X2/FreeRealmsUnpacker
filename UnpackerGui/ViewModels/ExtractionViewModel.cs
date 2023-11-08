using AssetIO;
using Avalonia.Threading;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reactive.Disposables;
using System.Threading;
using UnpackerGui.Models;

namespace UnpackerGui.ViewModels;

public class ExtractionViewModel : ViewModelBase
{
    public int AssetCount { get; }

    private readonly IEnumerable<ExtractionAssetFile> _extractionAssetFiles;
    private readonly string _outputDir;

    private string _extractionMessage = "";
    private string _elapsedTime = $@"{TimeSpan.Zero:hh\:mm\:ss}";
    private int _assetIndex;

    public ExtractionViewModel(string outputDir, IEnumerable<AssetFileViewModel> assetFiles)
    {
        AssetCount = assetFiles.Sum(x => x.Count);
        _outputDir = outputDir;
        _extractionAssetFiles = assetFiles.Select(x => new ExtractionAssetFile(x.Info, x.OpenRead, x.Assets));
    }

    public ExtractionViewModel(string outputDir, IEnumerable<AssetInfo> assets, int? count = null)
    {
        AssetCount = count ?? assets.Count();
        _outputDir = outputDir;
        _extractionAssetFiles = assets.GroupBy(x => x.AssetFile)
                                      .Select(x => new ExtractionAssetFile(x.Key.Info, x.Key.OpenRead, x));
    }

    public string ExtractionMessage
    {
        get => _extractionMessage;
        set => this.RaiseAndSetIfChanged(ref _extractionMessage, value);
    }

    public string ElapsedTime
    {
        get => _elapsedTime;
        set => this.RaiseAndSetIfChanged(ref _elapsedTime, value);
    }

    public int AssetIndex
    {
        get => _assetIndex;
        set => this.RaiseAndSetIfChanged(ref _assetIndex, value);
    }

    public void ExtractAssets(CancellationToken cancellationToken)
    {
        using (Timer())
        {
            foreach (ExtractionAssetFile assetFile in _extractionAssetFiles)
            {
                ExtractionMessage = $"Extracting {assetFile.Info.Name}";
                using AssetReader reader = assetFile.OpenRead();
                InternalExtractAssets(reader, assetFile.Assets, cancellationToken);
            }

            ExtractionMessage = "Extraction Complete";
        }
    }

    private void InternalExtractAssets(AssetReader reader,
                                       IEnumerable<AssetInfo> assets,
                                       CancellationToken cancellationToken)
    {
        foreach (AssetInfo asset in assets)
        {
            if (cancellationToken.IsCancellationRequested) cancellationToken.ThrowIfCancellationRequested();

            FileInfo file = new(Path.Combine(_outputDir, asset.Name));
            file.Directory?.Create();
            using FileStream fs = file.Open(FileMode.Create, FileAccess.Write, FileShare.Read);
            reader.CopyTo(asset, fs);
            AssetIndex++;
        }
    }

    private IDisposable Timer()
    {
        Stopwatch stopwatch = Stopwatch.StartNew();
        DispatcherTimer dispatchTimer = new(DispatcherPriority.Normal);
        dispatchTimer.Tick += (s, e) => ElapsedTime = $@"{stopwatch.Elapsed:hh\:mm\:ss}";
        dispatchTimer.Interval = TimeSpan.FromMilliseconds(500);
        dispatchTimer.Start();
        return Disposable.Create(() =>
        {
            stopwatch.Stop();
            dispatchTimer.Stop();
        });
    }
}
