using AssetIO;
using AvaloniaHex.Document;
using ReactiveUI;
using System;
using UnpackerGui.Collections;
using UnpackerGui.Extensions;
using UnpackerGui.Models;

namespace UnpackerGui.ViewModels;

public class HexBrowserViewModel : AssetBrowserViewModel
{
    /// <inheritdoc/>
    public override FilteredReactiveCollection<AssetInfo> Assets { get; }

    private MemoryBinaryDocument? _displayedDocument;

    /// Initializes a new instance of the <see cref="HexBrowserViewModel"/> class.
    /// </summary>
    public HexBrowserViewModel(ReadOnlyReactiveCollection<AssetInfo> assets)
    {
        // Filter all assets from the asset browser.
        Assets = assets.Filter();
        OnAssetsInitialized();

        // Display the selected asset in the hex viewer.
        this.WhenAnyValue(x => x.SelectedAsset)
            .Subscribe(x => DisplayedDocument = ReadDocument(x));
    }

    /// <summary>
    /// Gets or sets the displayed document.
    /// </summary>
    public MemoryBinaryDocument? DisplayedDocument
    {
        get => _displayedDocument;
        set => this.RaiseAndSetIfChanged(ref _displayedDocument, value);
    }

    /// <summary>
    /// Reads the bytes of the specified asset into a <see cref="MemoryBinaryDocument"/>.
    /// </summary>
    private static MemoryBinaryDocument? ReadDocument(AssetInfo? asset)
    {
        if (asset == null) return null;

        try
        {
            // Read the asset data into the binary document.
            using AssetReader reader = asset.AssetFile.OpenRead();
            return new MemoryBinaryDocument(reader.Read(asset));
        }
        catch
        {
            return null;
        }
    }
}
