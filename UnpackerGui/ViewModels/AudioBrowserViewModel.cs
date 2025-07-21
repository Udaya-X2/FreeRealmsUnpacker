using System.IO;
using UnpackerGui.Collections;
using UnpackerGui.Extensions;
using UnpackerGui.Models;

namespace UnpackerGui.ViewModels;

public class AudioBrowserViewModel : AssetBrowserViewModel
{
    /// <inheritdoc/>
    public override FilteredReactiveCollection<AssetInfo> Assets { get; }

    /// <summary>
    /// Gets the media player.
    /// </summary>
    //public MediaPlayer MediaPlayer { get; }

    //private readonly LibVLC _libVlc;
    private readonly MemoryStream _audioStream;

    /// <summary>
    /// Initializes a new instance of the <see cref="AudioBrowserViewModel"/> class.
    /// </summary>
    public AudioBrowserViewModel(ReadOnlyReactiveCollection<AssetInfo> assets)
    {
        // Filter the image assets from the asset browser.
        Assets = assets.Filter(x => x.IsAudio);
        OnAssetsInitialized(x => x.ShowAudioBrowser);

        // Initialize media player resources.
        //_libVlc = new LibVLC();
        _audioStream = new MemoryStream();
        //MediaPlayer = new MediaPlayer(_libVlc);
    }

    //public void Play()
    //{
    //    AssetInfo asset = SelectedAsset!;
    //    using AssetReader reader = asset.AssetFile.OpenRead();
    //    _audioStream.SetLength(0);
    //    reader.CopyTo(asset, _audioStream);
    //    _audioStream.Position = 0;
    //    using Media media = new(_libVlc, new StreamMediaInput(_audioStream));
    //    MediaPlayer.Play(media);
    //}

    //public void Stop() => MediaPlayer.Stop();
}
