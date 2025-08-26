using AssetIO;
using Avalonia.Input;
using Avalonia.Media.Imaging;
using Clowd.Clipboard;
using Pfim;
using ReactiveUI;
using SkiaSharp;
using System;
using System.IO;
using System.Reactive;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using UnpackerGui.Collections;
using UnpackerGui.Extensions;
using UnpackerGui.Images;
using UnpackerGui.Models;

namespace UnpackerGui.ViewModels;

public class ImageBrowserViewModel : AssetBrowserViewModel
{
    /// <inheritdoc/>
    public override FilteredReactiveCollection<AssetInfo> Assets { get; }

    public ReactiveCommand<Unit, Unit> CopyImageCommand { get; }

    private readonly MemoryStream _imageStream;
    private readonly PfimConfig _imageConfig;

    private Bitmap? _displayedImage;

    /// <summary>
    /// Initializes a new instance of the <see cref="ImageBrowserViewModel"/> class.
    /// </summary>
    public ImageBrowserViewModel(ReadOnlyReactiveCollection<AssetInfo> assets)
    {
        // Initialize each command.
        CopyImageCommand = ReactiveCommand.CreateFromTask(CopyImage);

        // Filter the image assets from the asset browser.
        Assets = assets.Filter(x => x.IsImage);
        OnAssetsInitialized(x => x.ShowImageBrowser);

        // Initialize bitmap creation resources.
        _imageStream = new MemoryStream();
        _imageConfig = new PfimConfig(allocator: new ImageAllocator());

        // Display the selected image asset.
        this.WhenAnyValue(x => x.SelectedAsset)
            .Subscribe(x => DisplayedImage = CreateBitmap(x));
    }

    /// <summary>
    /// Gets or sets the displayed bitmap image.
    /// </summary>
    public Bitmap? DisplayedImage
    {
        get => _displayedImage;
        set => this.RaiseAndSetIfChanged(ref _displayedImage, value);
    }

    /// <summary>
    /// Creates a bitmap image from the specified asset.
    /// </summary>
    private Bitmap? CreateBitmap(AssetInfo? asset)
    {
        // Dispose of the previous image, if necessary.
        DisplayedImage?.Dispose();

        if (asset == null) return null;

        try
        {
            // Read the asset as a stream.
            using AssetReader reader = asset.AssetFile.OpenRead();
            Stream stream = reader.ReadStream(asset);

            // Convert the stream from DDS/TGA to PNG, if necessary.
            if (asset.Type is "DDS" && !ConvertDdsImage(ref stream)) return null;
            if (asset.Type is "TGA" && !ConvertTargaImage(ref stream)) return null;

            // Create the bitmap image from the stream.
            return new Bitmap(stream);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Converts the specified stream from DDS to PNG.
    /// </summary>
    /// <returns><inheritdoc cref="ConvertImage(IImage)"/></returns>
    private bool ConvertDdsImage(ref Stream stream)
    {
        using IImage image = Dds.Create(stream, _imageConfig);
        stream = _imageStream;
        return ConvertImage(image);
    }

    /// <summary>
    /// Converts the specified stream from TARGA to PNG.
    /// </summary>
    /// <returns><inheritdoc cref="ConvertImage(IImage)"/></returns>
    private bool ConvertTargaImage(ref Stream stream)
    {
        using IImage image = Targa.Create(stream, _imageConfig);
        stream = _imageStream;
        return ConvertImage(image);
    }

    /// <summary>
    /// Converts the specified image to a PNG and stores it in the current image stream.
    /// </summary>
    /// <remarks>
    /// Source: <see href="https://github.com/nickbabcock/Pfim/blob/master/src/Pfim.Skia/Program.cs"/>
    /// </remarks>
    /// <returns>
    /// <see langword="true"/> if the image was successfully converted; otherwise, <see langword="false"/>.
    /// </returns>
    private bool ConvertImage(IImage image)
    {
        byte[] newData = image.Data;
        int newDataLen = image.DataLen;
        int stride = image.Stride;
        SKColorType colorType;

        switch (image.Format)
        {
            case ImageFormat.Rgb8:
                colorType = SKColorType.Gray8;
                break;
            // Color channels still need to be swapped.
            case ImageFormat.R5g6b5:
                colorType = SKColorType.Rgb565;
                break;
            // Color channels still need to be swapped.
            case ImageFormat.Rgba16:
                colorType = SKColorType.Argb4444;
                break;
            // Skia has no 24-bit pixels, so we upscale to 32-bit.
            case ImageFormat.Rgb24:
                int pixels = image.DataLen / 3;
                newDataLen = pixels * 4;
                newData = new byte[newDataLen];

                for (int i = 0; i < pixels; i++)
                {
                    newData[i * 4] = image.Data[i * 3];
                    newData[i * 4 + 1] = image.Data[i * 3 + 1];
                    newData[i * 4 + 2] = image.Data[i * 3 + 2];
                    newData[i * 4 + 3] = 255;
                }

                stride = image.Width * 4;
                colorType = SKColorType.Bgra8888;
                break;
            case ImageFormat.Rgba32:
                colorType = SKColorType.Bgra8888;
                break;
            default:
                return false;
        }

        SKImageInfo imageInfo = new(image.Width, image.Height, colorType);
        GCHandle handle = GCHandle.Alloc(newData, GCHandleType.Pinned);
        nint ptr = Marshal.UnsafeAddrOfPinnedArrayElement(newData, 0);
        using SKData data = SKData.Create(ptr, newDataLen, (address, context) => handle.Free());
        using SKImage skImage = SKImage.FromPixels(imageInfo, data, stride);
        using SKBitmap bitmap = SKBitmap.FromImage(skImage);
        _imageStream.SetLength(0);
        using SKManagedWStream wstream = new(_imageStream);

        if (bitmap.Encode(wstream, SKEncodedImageFormat.Png, 100))
        {
            _imageStream.Position = 0;
            return true;
        }

        return false;
    }

    /// <summary>
    /// Copies the displayed image to the clipboard.
    /// </summary>
    /// <remarks>
    /// Source:
    /// <list type="bullet">
    /// <item><see href="https://github.com/AvaloniaUI/Avalonia/issues/3588#issuecomment-1272505415"/></item>
    /// <item><see href="https://github.com/AvaloniaUI/Avalonia/issues/3588#issuecomment-2571770220"/></item>
    /// </list>
    /// </remarks>
    private async Task CopyImage()
    {
        if (_displayedImage == null) return;

        if (OperatingSystem.IsWindows())
        {
            ClipboardAvalonia.SetImage(_displayedImage);
        }
        else
        {
            _imageStream.SetLength(0);
            _displayedImage.Save(_imageStream);
            DataObject dataObject = new();
            dataObject.Set("image/png", _imageStream.ToArray());
            await App.SetClipboardData(dataObject);
        }
    }
}
