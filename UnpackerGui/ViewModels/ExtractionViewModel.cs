using AssetIO;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
using UnpackerGui.Models;

namespace UnpackerGui.ViewModels;

public class ExtractionViewModel : ProgressViewModel
{
    private readonly IEnumerable<ExtractionAssetFile> _extractionAssetFiles;
    private readonly string _outputDir;

    /// <summary>
    /// Initializes a new instance of the <see cref="ExtractionViewModel"/>
    /// class from the specified output directory and asset files.
    /// </summary>
    public ExtractionViewModel(string outputDir, IEnumerable<AssetFileViewModel> assetFiles)
    {
        Maximum = assetFiles.Sum(x => x.Count);
        _outputDir = outputDir;
        _extractionAssetFiles = assetFiles.Select(x => new ExtractionAssetFile(x.Info, x.OpenRead, x.Assets));
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ExtractionViewModel"/>
    /// class from the specified output directory and assets.
    /// </summary>
    public ExtractionViewModel(string outputDir, IEnumerable<AssetInfo> assets, int? count = null)
    {
        Maximum = count ?? assets.Count();
        _outputDir = outputDir;
        _extractionAssetFiles = assets.GroupBy(x => x.AssetFile)
                                      .Select(x => new ExtractionAssetFile(x.Key.Info, x.Key.OpenRead, x));
    }

    /// <inheritdoc/>
    public override int Maximum { get; }

    /// <inheritdoc/>
    protected override Task CommandTask(CancellationToken token)
    {
        token.Register(() =>
        {
            if (!IsComplete)
            {
                Message = "Stopping Extraction...";
            }
        });
        return Task.Run(() => ExtractAssets(token), token)
                   .ContinueWith(x =>
                   {
                       IsComplete = true;

                       if (x.IsFaulted)
                       {
                           ExceptionDispatchInfo.Throw(x.Exception!.InnerException!);
                       }
                       if (x.IsCompletedSuccessfully)
                       {
                           Message = "Extraction Complete";
                       }
                   }, CancellationToken.None);
    }

    /// <summary>
    /// Extracts the assets from each extraction asset file to the output directory.
    /// </summary>
    private void ExtractAssets(CancellationToken token)
    {
        if (token.IsCancellationRequested) token.ThrowIfCancellationRequested();

        using (Timer())
        {
            foreach (ExtractionAssetFile assetFile in _extractionAssetFiles)
            {
                Message = $"Extracting {assetFile.Info.Name}";
                using AssetReader reader = assetFile.OpenRead();

                foreach (AssetInfo asset in assetFile.Assets)
                {
                    if (token.IsCancellationRequested) token.ThrowIfCancellationRequested();

                    FileInfo file = new(Path.Combine(_outputDir, asset.Name));
                    file.Directory?.Create();
                    using FileStream fs = file.Open(FileMode.Create, FileAccess.Write, FileShare.Read);
                    reader.CopyTo(asset, fs);
                    Tick();
                }
            }
        }
    }
}
