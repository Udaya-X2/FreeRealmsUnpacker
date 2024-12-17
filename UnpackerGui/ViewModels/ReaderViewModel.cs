using AssetIO;
using DynamicData;
using System;
using System.Collections.Generic;
using System.Threading;

namespace UnpackerGui.ViewModels;

public class ReaderViewModel(ISourceList<AssetFileViewModel> sourceAssetFiles,
                             IList<AssetFile> inputAssetFiles) : ProgressViewModel
{
    private readonly ISourceList<AssetFileViewModel> _sourceAssetFiles = sourceAssetFiles
        ?? throw new ArgumentNullException(nameof(sourceAssetFiles));
    private readonly IList<AssetFile> _inputAssetFiles = inputAssetFiles
        ?? throw new ArgumentNullException(nameof(inputAssetFiles));

    /// <inheritdoc/>
    public override int Maximum => _inputAssetFiles.Count;

    /// <inheritdoc/>
    public override string Title => "Reading";

    /// <inheritdoc/>
    protected override void CommandAction(CancellationToken token) => ReadAssets(token);

    /// <summary>
    /// Reads the assets from the input file paths and adds them to the source asset file list.
    /// </summary>
    private void ReadAssets(CancellationToken token)
    {
        token.ThrowIfCancellationRequested();

        AssetFileViewModel[] assetFileViewModels = new AssetFileViewModel[Maximum];

        for (int i = 0; i < _inputAssetFiles.Count; i++)
        {
            token.ThrowIfCancellationRequested();
            AssetFile assetFile = _inputAssetFiles[i];
            Message = $"Reading {assetFile.Name}";
            assetFileViewModels[i] = new AssetFileViewModel(assetFile, token);
            Tick();
        }

        _sourceAssetFiles.AddRange(assetFileViewModels);
    }
}
