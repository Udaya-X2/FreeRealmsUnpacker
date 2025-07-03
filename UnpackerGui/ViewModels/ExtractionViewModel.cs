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
    private readonly IEnumerable<AssetFileSelection> _assetFiles;
    private readonly SettingsViewModel _settings;

    /// <summary>
    /// Initializes a new instance of the <see cref="ExtractionViewModel"/> class from the specified asset files.
    /// </summary>
    public ExtractionViewModel(IEnumerable<AssetFileViewModel> assetFiles)
    {
        Maximum = assetFiles.Sum(x => x.Count);
        _assetFiles = assetFiles.Select(x => new AssetFileSelection(x, x));
        _settings = App.GetSettings();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ExtractionViewModel"/> class from the specified assets.
    /// </summary>
    public ExtractionViewModel(IEnumerable<AssetInfo> assets, int? count = null)
    {
        Maximum = count ?? assets.Count();
        _assetFiles = assets.GroupBy(x => x.AssetFile)
                            .Select(x => new AssetFileSelection(x.Key, x));
        _settings = App.GetSettings();
    }

    /// <inheritdoc/>
    public override int Maximum { get; }

    /// <inheritdoc/>
    public override string Title => "Extraction";

    /// <inheritdoc/>
    protected override void CommandAction(CancellationToken token) => ExtractAssets(token);

    /// <summary>
    /// Extracts the selected assets from each asset file to the output directory.
    /// </summary>
    private void ExtractAssets(CancellationToken token)
    {
        token.ThrowIfCancellationRequested();

        foreach (AssetFileSelection assetFile in _assetFiles)
        {
            Message = $"Extracting {assetFile.File.Name}";
            using AssetReader reader = assetFile.File.OpenRead();

            foreach (AssetInfo asset in assetFile.SelectedAssets)
            {
                token.ThrowIfCancellationRequested();
                reader.ExtractTo(asset, _settings.OutputDirectory, _settings.ConflictOptions);
                Tick();
            }
        }
    }
}
