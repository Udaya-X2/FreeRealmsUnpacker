using AssetIO;
using DynamicData;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
using UnpackerGui.Extensions;

namespace UnpackerGui.ViewModels;

public class ReaderViewModel : ProgressViewModel
{
    private readonly ISourceList<AssetFileViewModel> _sourceAssetFiles;
    private readonly List<string> _inputFilePaths;
    private readonly AssetType? _assetType;

    /// <summary>
    /// Initializes a new instance of the <see cref="ReaderViewModel"/> class from the
    /// specified source asset file list, input file paths, and optional asset type.
    /// </summary>
    public ReaderViewModel(ISourceList<AssetFileViewModel> sourceAssetFiles,
                           IEnumerable<string> inputAssetFiles,
                           AssetType? assetType = null)
    {
        _sourceAssetFiles = sourceAssetFiles ?? throw new ArgumentNullException(nameof(sourceAssetFiles));
        _inputFilePaths = new(inputAssetFiles ?? throw new ArgumentNullException(nameof(inputAssetFiles)));
        _assetType = assetType;
        Maximum = _inputFilePaths.Count;
    }

    /// <inheritdoc/>
    public override int Maximum { get; }

    /// <inheritdoc/>
    protected override Task CommandTask(CancellationToken token)
    {
        token.Register(() =>
        {
            if (!Status.IsCompleted())
            {
                Message = "Stopping Reading...";
            }
        });
        return Task.Run(() => ReadAssets(token), token)
                   .ContinueWith(x =>
                   {
                       Status = x.Status;

                       if (x.IsFaulted)
                       {
                           ExceptionDispatchInfo.Throw(x.Exception!.InnerException!);
                       }
                       if (x.IsCompletedSuccessfully)
                       {
                           Message = "Reading Complete";
                       }
                   }, CancellationToken.None);
    }

    /// <summary>
    /// Reads the assets from the input file paths and adds them to the source asset file list.
    /// </summary>
    private void ReadAssets(CancellationToken token)
    {
        if (token.IsCancellationRequested) token.ThrowIfCancellationRequested();

        using (Timer())
        {
            List<AssetFileViewModel> inputAssetFiles = new();

            foreach (string path in _inputFilePaths)
            {
                if (token.IsCancellationRequested) token.ThrowIfCancellationRequested();

                Message = $"Reading {Path.GetFileName(path)}";
                AssetType assetType = _assetType ?? (AssetType.Game | ClientFile.InferAssetType(path));

                if (!assetType.IsValid())
                {
                    Tick();
                    continue;
                }

                inputAssetFiles.Add(new(path, assetType, token));
                Tick();
            }

            _sourceAssetFiles.AddRange(inputAssetFiles);
        }
    }
}
