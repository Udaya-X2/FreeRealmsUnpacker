using AssetIO;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
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
    public override string Title => "Extraction";

    /// <inheritdoc/>
    protected override void CommandAction(CancellationToken token) => ExtractAssets(token);

    /// <summary>
    /// Extracts the assets from each extraction asset file to the output directory.
    /// </summary>
    private void ExtractAssets(CancellationToken token)
    {
        if (token.IsCancellationRequested) token.ThrowIfCancellationRequested();

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
