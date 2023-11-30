using AssetIO;
using DynamicData;
using System;
using System.Collections.Generic;
using System.Threading;

namespace UnpackerGui.ViewModels;

public class ReaderViewModel : ProgressViewModel
{
    private readonly ISourceList<AssetFileViewModel> _sourceAssetFiles;
    private readonly IList<AssetFile> _inputAssetFiles;

    /// <summary>
    /// Initializes a new instance of the <see cref="ReaderViewModel"/>
    /// class from the specified source asset files and input asset files.
    /// </summary>
    public ReaderViewModel(ISourceList<AssetFileViewModel> sourceAssetFiles,
                           IList<AssetFile> inputAssetFiles)
    {
        _sourceAssetFiles = sourceAssetFiles ?? throw new ArgumentNullException(nameof(sourceAssetFiles));
        _inputAssetFiles = inputAssetFiles ?? throw new ArgumentNullException(nameof(inputAssetFiles));
    }

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
        if (token.IsCancellationRequested) token.ThrowIfCancellationRequested();

        AssetFileViewModel[] assetFileViewModels = new AssetFileViewModel[Maximum];

        for (int i = 0; i < _inputAssetFiles.Count; i++)
        {
            if (token.IsCancellationRequested) token.ThrowIfCancellationRequested();

            AssetFile assetFile = _inputAssetFiles[i];
            Message = $"Reading {assetFile.Name}";
            assetFileViewModels[i] = new AssetFileViewModel(assetFile, token);
            Tick();
        }

        _sourceAssetFiles.AddRange(assetFileViewModels);
    }
}
