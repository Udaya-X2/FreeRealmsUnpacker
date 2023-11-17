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
    /// <summary>
    /// Gets the number of assets to extract.
    /// </summary>
    public int AssetCount { get; }

    private readonly IEnumerable<ExtractionAssetFile> _extractionAssetFiles;
    private readonly string _outputDir;

    private string _extractionMessage = "";
    private string _elapsedTime = $@"{TimeSpan.Zero:hh\:mm\:ss}";
    private int _assetIndex;

    /// <summary>
    /// Initializes a new instance of the <see cref="ExtractionViewModel"/>
    /// class from the specified output directory and asset files.
    /// </summary>
    public ExtractionViewModel(string outputDir, IEnumerable<AssetFileViewModel> assetFiles)
    {
        AssetCount = assetFiles.Sum(x => x.Count);
        _outputDir = outputDir;
        _extractionAssetFiles = assetFiles.Select(x => new ExtractionAssetFile(x.Info, x.OpenRead, x.Assets));
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ExtractionViewModel"/>
    /// class from the specified output directory and assets.
    /// </summary>
    public ExtractionViewModel(string outputDir, IEnumerable<AssetInfo> assets, int? count = null)
    {
        AssetCount = count ?? assets.Count();
        _outputDir = outputDir;
        _extractionAssetFiles = assets.GroupBy(x => x.AssetFile)
                                      .Select(x => new ExtractionAssetFile(x.Key.Info, x.Key.OpenRead, x));
    }

    /// <summary>
    /// Gets or sets the extraction message.
    /// </summary>
    public string ExtractionMessage
    {
        get => _extractionMessage;
        set => this.RaiseAndSetIfChanged(ref _extractionMessage, value);
    }

    /// <summary>
    /// Gets or sets the time that has passed since the start of the extraction.
    /// </summary>
    public string ElapsedTime
    {
        get => _elapsedTime;
        set => this.RaiseAndSetIfChanged(ref _elapsedTime, value);
    }

    /// <summary>
    /// Gets or sets the number of assets that have been extracted.
    /// </summary>
    public int AssetIndex
    {
        get => _assetIndex;
        set => this.RaiseAndSetIfChanged(ref _assetIndex, value);
    }

    /// <summary>
    /// Extracts the assets from each extraction asset file to the output directory.
    /// </summary>
    public void ExtractAssets(CancellationToken token)
    {
        using IDisposable _ = Timer();

        foreach (ExtractionAssetFile assetFile in _extractionAssetFiles)
        {
            ExtractionMessage = $"Extracting {assetFile.Info.Name}";
            using AssetReader reader = assetFile.OpenRead();

            foreach (AssetInfo asset in assetFile.Assets)
            {
                if (token.IsCancellationRequested) token.ThrowIfCancellationRequested();

                FileInfo file = new(Path.Combine(_outputDir, asset.Name));
                file.Directory?.Create();
                using FileStream fs = file.Open(FileMode.Create, FileAccess.Write, FileShare.Read);
                reader.CopyTo(asset, fs);
                AssetIndex++;
            }
        }

        ExtractionMessage = "Extraction Complete";
    }

    /// <summary>
    /// Creates a timer that updates the elapsed time every half-second.
    /// </summary>
    /// <returns>A disposable that, when disposed, stops the timer.</returns>
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
