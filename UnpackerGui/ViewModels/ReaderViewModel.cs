using AssetIO;
using DynamicData;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using UnpackerGui.Collections;
using UnpackerGui.Extensions;

namespace UnpackerGui.ViewModels;

public class ReaderViewModel(ISourceList<AssetFileViewModel> sourceAssetFiles,
                             IList<AssetFile> inputAssetFiles,
                             bool updateRecentFiles = false) : ProgressViewModel
{
    private readonly ISourceList<AssetFileViewModel> _sourceAssetFiles = sourceAssetFiles
        ?? throw new ArgumentNullException(nameof(sourceAssetFiles));
    private readonly IList<AssetFile> _inputAssetFiles = inputAssetFiles
        ?? throw new ArgumentNullException(nameof(inputAssetFiles));
    private readonly RecentItemCollection<string>? _recentFiles = updateRecentFiles
        ? App.GetSettings().RecentFiles : null;

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
        AssetFile? assetFile = null;
        using var _ = _recentFiles?.SuspendNotifications();

        try
        {
            while (Value < Maximum)
            {
                token.ThrowIfCancellationRequested();
                assetFile = _inputAssetFiles[Value];
                Message = $"Reading {assetFile.Name}";
                assetFileViewModels[Value] = new AssetFileViewModel(assetFile, token);
                _recentFiles?.Add(assetFile.FullName);
                Tick();
            }
        }
        catch when (!token.IsCancellationRequested && _recentFiles != null && assetFile != null)
        {
            _recentFiles.Remove(x => TryGetFullPath(x) == assetFile.FullName);
            throw;
        }

        _sourceAssetFiles.AddRange(assetFileViewModels);
    }

    /// <summary>
    /// <inheritdoc cref="Path.GetFullPath(string)"/>
    /// </summary>
    /// <param name="path"><inheritdoc cref="Path.GetFullPath(string)"/></param>
    /// <returns><inheritdoc cref="Path.GetFullPath(string)"/></returns>
    private static string TryGetFullPath(string path)
    {
        try
        {
            return Path.GetFullPath(path);
        }
        catch
        {
            return path;
        }
    }
}
