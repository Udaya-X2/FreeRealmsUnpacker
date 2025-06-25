using AssetIO;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using UnpackerGui.Models;

namespace UnpackerGui.ViewModels;

public class ExtractionViewModel : ProgressViewModel
{
    private readonly IEnumerable<ExtractionAssetFile> _extractionAssetFiles;
    private readonly string _outputDir;
    private readonly FileConflictOptions _conflictOptions;

    /// <summary>
    /// Initializes a new instance of the <see cref="ExtractionViewModel"/>
    /// class from the specified output directory and asset files.
    /// </summary>
    public ExtractionViewModel(string outputDir,
                               IEnumerable<AssetFileViewModel> assetFiles,
                               FileConflictOptions conflictOptions = FileConflictOptions.Overwrite)
    {
        Maximum = assetFiles.Sum(x => x.Count);
        _outputDir = outputDir;
        _extractionAssetFiles = assetFiles.Select(x => new ExtractionAssetFile(x.Name, x.OpenRead, x));
        _conflictOptions = conflictOptions;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ExtractionViewModel"/>
    /// class from the specified output directory and assets.
    /// </summary>
    public ExtractionViewModel(string outputDir,
                               IEnumerable<AssetInfo> assets,
                               int? count = null,
                               FileConflictOptions conflictOptions = FileConflictOptions.Overwrite)
    {
        Maximum = count ?? assets.Count();
        _outputDir = outputDir;
        _extractionAssetFiles = assets.GroupBy(x => x.AssetFile)
                                      .Select(x => new ExtractionAssetFile(x.Key.Name, x.Key.OpenRead, x));
        _conflictOptions = conflictOptions;
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
        token.ThrowIfCancellationRequested();

        foreach (ExtractionAssetFile assetFile in _extractionAssetFiles)
        {
            Message = $"Extracting {assetFile.Name}";
            using AssetReader reader = assetFile.OpenRead();

            foreach (AssetInfo asset in assetFile.Assets)
            {
                token.ThrowIfCancellationRequested();
                reader.ExtractTo(asset, _outputDir, _conflictOptions);
                Tick();
            }
        }
    }
}
