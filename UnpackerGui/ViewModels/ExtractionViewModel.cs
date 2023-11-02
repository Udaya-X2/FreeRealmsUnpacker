using AssetIO;
using Avalonia.Threading;
using DynamicData.Kernel;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reactive.Disposables;
using UnpackerGui.Models;

namespace UnpackerGui.ViewModels;

public class ExtractionViewModel : ViewModelBase
{
    public int AssetCount { get; }

    private readonly Action _extractAssetsAction;
    private readonly string _outputDir;

    private string _extractionMessage = "";
    private string _elapsedTime = $@"{TimeSpan.Zero:hh\:mm\:ss}";
    private int _assetIndex;

    public ExtractionViewModel(string outputDir, IEnumerable<AssetFileViewModel> assetFiles)
    {
        AssetCount = assetFiles.Sum(x => x.Count);
        _outputDir = outputDir;
        _extractAssetsAction = () =>
        {
            using (Timer())
            {
                foreach (AssetFileViewModel assetFile in assetFiles)
                {
                    ExtractionMessage = $"Extracting {assetFile.Name}";
                    using AssetReader reader = assetFile.OpenRead();
                    InternalExtractAssets(reader, assetFile.Assets);
                }
            }

            ExtractionMessage = "Extraction Complete";
        };
    }

    public ExtractionViewModel(string outputDir, IList<AssetInfo> assets)
    {
        AssetCount = assets.Count;
        _outputDir = outputDir;
        _extractAssetsAction = () =>
        {
            using (Timer())
            {
                foreach (var group in assets.GroupBy(x => x.AssetFile))
                {
                    ExtractionMessage = $"Extracting {group.Key.Name}";
                    using AssetReader reader = group.Key.OpenRead();
                    InternalExtractAssets(reader, group.AsList());
                }
            }

            ExtractionMessage = "Extraction Complete";
        };
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

    public void ExtractAssets() => _extractAssetsAction();

    private void InternalExtractAssets(AssetReader reader, IEnumerable<AssetInfo> assets)
    {
        foreach (AssetInfo asset in assets)
        {
            string assetPath = Path.Combine(_outputDir, asset.Name);
            Directory.CreateDirectory(Path.GetDirectoryName(assetPath) ?? _outputDir);
            using FileStream fs = new(assetPath, FileMode.Create, FileAccess.Write, FileShare.Read);
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
