using AssetIO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UnpackerGui.Models;

namespace UnpackerGui.ViewModels;

public class ValidationViewModel : ProgressViewModel
{
    private readonly IEnumerable<AssetFileViewModel> _assetFiles;

    public ValidationViewModel(IEnumerable<AssetFileViewModel> assetFiles)
    {
        _assetFiles = assetFiles ?? throw new ArgumentNullException(nameof(assetFiles));
    }

    /// <inheritdoc/>
    public override int Maximum => _assetFiles.Sum(x => x.Count);

    /// <inheritdoc/>
    public override string Title => "Validation";

    /// <inheritdoc/>
    protected override void CommandAction(CancellationToken token) => ValidateAssets(token);

    /// <summary>
    /// Validates the CRC-32 values of the assets in each asset file.
    /// </summary>
    private void ValidateAssets(CancellationToken token)
    {
        token.ThrowIfCancellationRequested();

        foreach (AssetFileViewModel assetFile in _assetFiles)
        {
            using AssetReader reader = assetFile.OpenRead();
            Message = $"Validating {assetFile.Name}";

            foreach (AssetInfo asset in assetFile)
            {
                token.ThrowIfCancellationRequested();
                asset.FileCrc32 = reader.GetCrc32(asset);
                Tick();
            }

            assetFile.IsValidated = true;
        }
    }
}
