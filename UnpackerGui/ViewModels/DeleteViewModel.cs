using AssetIO;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using UnpackerGui.Models;

namespace UnpackerGui.ViewModels;

public class DeleteViewModel(IEnumerable<AssetInfo> assets) : ProgressViewModel
{
    private readonly List<AssetFileSelection> _assetFiles = [.. assets.GroupBy(x => x.AssetFile)
                                                                      .Select(x => new AssetFileSelection(x.Key, x))];

    /// <summary>
    /// Gets the asset files that were modified while deleting assets.
    /// </summary>
    public List<AssetFile> ModifiedFiles { get; } = [];

    /// <inheritdoc/>
    public override int Maximum => _assetFiles.Count;

    /// <inheritdoc/>
    public override string Title => "Deletion";

    /// <inheritdoc/>
    protected override void CommandAction(CancellationToken token) => DeleteAssets(token);

    /// <summary>
    /// Deletes the assets from each asset file.
    /// </summary>
    private void DeleteAssets(CancellationToken token)
    {
        token.ThrowIfCancellationRequested();

        foreach (AssetFileSelection assetFile in _assetFiles)
        {
            token.ThrowIfCancellationRequested();
            Message = $"Deleting from {assetFile.File.Name}";
            ModifiedFiles.Add(assetFile.File);
            assetFile.File.RemoveAssets(assetFile.SelectedAssets.ToHashSet(AssetInfo.Comparer));
            Tick();
        }
    }
}
