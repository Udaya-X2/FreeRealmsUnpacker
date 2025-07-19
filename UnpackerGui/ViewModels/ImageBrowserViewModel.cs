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

public class ImageBrowserViewModel : ViewModelBase
{
    /// <summary>
    /// Gets the image assets shown to the user.
    /// </summary>
    public FilteredReactiveCollection<AssetInfo> ImageAssets { get; }

    /// <summary>
    /// Gets the options to filter the image assets shown.
    /// </summary>
    public ImageOptionsViewModel<AssetInfo> ImageOptions { get; }

    /// <summary>
    /// Gets the application's settings.
    /// </summary>
    public SettingsViewModel Settings { get; }

    public ReactiveCommand<Unit, Unit> CopyImageCommand { get; }

    private readonly MemoryStream _imageStream;
    private readonly PfimConfig _imageConfig;

    private AssetInfo? _selectedImageAsset;
    private Bitmap? _displayedImage;

    /// <summary>
    /// Initializes a new instance of the <see cref="ImageBrowserViewModel"/> class.
    /// </summary>
    public ImageBrowserViewModel(MainViewModel mainViewModel)
    {
        // Initialize each command.
        CopyImageCommand = ReactiveCommand.CreateFromTask(CopyImageAsync);

        // Filter the image assets from the asset browser.
        Settings = mainViewModel.Settings;
        ImageOptions = new ImageOptionsViewModel<AssetInfo>(x => x.Type);
        ImageAssets = mainViewModel.Assets.Filter(ImageOptions);

        // Initialize 
        _imageStream = new MemoryStream();
        _imageConfig = new PfimConfig(allocator: new ImageAllocator());

        // Display the selected image asset.
        this.WhenAnyValue(x => x.SelectedImageAsset)
            .Subscribe(x => DisplayedImage = CreateBitmap(x));
    }

    /// <summary>
    /// Gets or sets the selected image asset.
    /// </summary>
    public AssetInfo? SelectedImageAsset
    {
        get => _selectedImageAsset;
        set => this.RaiseAndSetIfChanged(ref _selectedImageAsset, value);
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
    /// <remarks>
    /// Source: <see href="https://github.com/nickbabcock/Pfim/blob/master/src/Pfim.Skia/Program.cs"/>
    /// </remarks>
    private Bitmap? CreateBitmap(AssetInfo? asset)
    {
        DisplayedImage?.Dispose();

        if (asset == null) return null;

        try
        {
            using AssetReader reader = asset.AssetFile.OpenRead();
            _imageStream.SetLength(0);
            reader.CopyTo(asset, _imageStream);

            if (asset.Type is "DDS" or "TGA")
            {
                _imageStream.Position = 0;
                using IImage image = asset.Type switch
                {
                    "DDS" => Dds.Create(_imageStream, _imageConfig),
                    _ => Targa.Create(_imageStream, _imageConfig)
                };
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
                        return null;
                }

                SKImageInfo imageInfo = new(image.Width, image.Height, colorType);
                GCHandle handle = GCHandle.Alloc(newData, GCHandleType.Pinned);
                nint ptr = Marshal.UnsafeAddrOfPinnedArrayElement(newData, 0);
                using SKData data = SKData.Create(ptr, newDataLen, (address, context) => handle.Free());
                using SKImage skImage = SKImage.FromPixels(imageInfo, data, stride);
                using SKBitmap bitmap = SKBitmap.FromImage(skImage);
                _imageStream.SetLength(0);
                using SKManagedWStream wstream = new(_imageStream);

                if (!bitmap.Encode(wstream, SKEncodedImageFormat.Png, 100)) return null;
            }

            _imageStream.Position = 0;
            return new Bitmap(_imageStream);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Copies the displayed image to the clipboard.
    /// </summary>
    /// <remarks>
    /// Sources:
    /// <list type="bullet">
    /// <item><see href="https://github.com/AvaloniaUI/Avalonia/issues/3588#issuecomment-1272505415"/></item>
    /// <item><see href="https://github.com/AvaloniaUI/Avalonia/issues/3588#issuecomment-2571770220"/></item>
    /// </list>
    /// </remarks>
    private async Task CopyImageAsync()
    {
        if (_displayedImage == null) return;

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
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
