using AssetIO;
using System;
using System.Collections.Generic;
using System.Threading;

namespace UnpackerGui.ViewModels;

public class WriterViewModel(AssetFileViewModel assetFile, List<string> files) : ProgressViewModel
{
    private readonly AssetFileViewModel _assetFile = assetFile ?? throw new ArgumentNullException(nameof(assetFile));
    private readonly List<string> _files = files ?? throw new ArgumentNullException(nameof(files));

    /// <inheritdoc/>
    public override int Maximum => _files.Count;

    /// <inheritdoc/>
    public override string Title => "Writing";

    /// <inheritdoc/>
    protected override void CommandAction(CancellationToken token) => WriteAssets(token);

    /// <summary>
    /// Writes the assets from the list of files to the asset file.
    /// </summary>
    private void WriteAssets(CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        using AssetWriter writer = _assetFile.OpenAppend();
        Message = $"Writing {_assetFile.Name}";

        foreach (string file in _files)
        {
            token.ThrowIfCancellationRequested();
            writer.Write(file);
            Tick();
        }
    }
}
