using AssetIO;
using LibVLCSharp.Shared;
using ReactiveUI;
using System;
using System.IO;
using System.Reactive;
using System.Threading.Tasks;
using UnpackerGui.Collections;
using UnpackerGui.Extensions;
using UnpackerGui.Models;

namespace UnpackerGui.ViewModels;

public class AudioBrowserViewModel : AssetBrowserViewModel
{
    /// <inheritdoc/>
    public override FilteredReactiveCollection<AssetInfo> Assets { get; }

    public ReactiveCommand<Unit, Unit> TogglePlayPauseCommand { get; }
    public ReactiveCommand<Unit, Unit> ToggleMuteCommand { get; }
    public ReactiveCommand<string, Unit> SetRateCommand { get; }

    private readonly MemoryStream _audioStream;
    private readonly LibVLC _libVlc;
    private readonly MediaPlayer _mediaPlayer;

    /// <summary>
    /// Initializes a new instance of the <see cref="AudioBrowserViewModel"/> class.
    /// </summary>
    public AudioBrowserViewModel(ReadOnlyReactiveCollection<AssetInfo> assets)
    {
        // Initialize each command.
        TogglePlayPauseCommand = ReactiveCommand.Create(TogglePlayPause);
        ToggleMuteCommand = ReactiveCommand.Create(ToggleMute);
        SetRateCommand = ReactiveCommand.Create<string>(SetRate);

        // Filter the image assets from the asset browser.
        Assets = assets.Filter(x => x.IsAudio);
        OnAssetsInitialized(x => x.ShowAudioBrowser);

        // Initialize media player resources.
        _audioStream = new MemoryStream();
        _libVlc = new LibVLC();
        _mediaPlayer = new MediaPlayer(_libVlc);
        _mediaPlayer.Paused += (s, e) => this.RaisePropertyChanged(nameof(IsPlaying));
        _mediaPlayer.Stopped += (s, e) => this.RaisePropertyChanged(nameof(IsPlaying));
        _mediaPlayer.Playing += (s, e) => this.RaisePropertyChanged(nameof(IsPlaying));
        _mediaPlayer.LengthChanged += (s, e) => this.RaisePropertyChanged(nameof(Length));
        _mediaPlayer.TimeChanged += (s, e) => this.RaisePropertyChanged(nameof(Time));
        _mediaPlayer.VolumeChanged += (s, e) =>
        {
            this.RaisePropertyChanged(nameof(Volume));
            this.RaisePropertyChanged(nameof(IsMuted));
        };
        _mediaPlayer.EndReached += (s, e) => Task.Run(() =>
        {
            _mediaPlayer.Stop();
            this.RaisePropertyChanged(nameof(Time));
        });

        // Update the media to the selected asset.
        this.WhenAnyValue(x => x.SelectedAsset)
            .Subscribe(CreateMedia);
    }

    public bool IsPlaying => _mediaPlayer.IsPlaying;

    public bool IsMuted
    {
        get => _mediaPlayer.Volume == 0;
        set => _mediaPlayer.Volume = value ? 0 : 100;
    }

    public int Volume
    {
        get => _mediaPlayer.Volume;
        set => _mediaPlayer.Volume = value;
    }

    public long Time
    {
        get => _mediaPlayer.Time;
        set => _mediaPlayer.Time = value;
    }

    public long Length => Math.Max(1, _mediaPlayer.Length);

    public float Rate
    {
        get => _mediaPlayer.Rate;
        set => _mediaPlayer.SetRate(value);
    }

    private void CreateMedia(AssetInfo? asset)
    {
        _mediaPlayer.Media = null;
        this.RaisePropertyChanged(nameof(IsPlaying));
        this.RaisePropertyChanged(nameof(Length));
        this.RaisePropertyChanged(nameof(Time));

        if (asset == null) return;

        using AssetReader reader = asset.AssetFile.OpenRead();
        _audioStream.SetLength(0);
        reader.CopyTo(asset, _audioStream);
        _audioStream.Position = 0;
        using Media media = new(_libVlc, new StreamMediaInput(_audioStream));
        _mediaPlayer.Media = media;
        //_mediaPlayer.Play();
    }

    private void TogglePlayPause()
    {
        if (IsPlaying)
        {
            _mediaPlayer.Pause();
        }
        else
        {
            _mediaPlayer.Play();
        }
    }

    private void ToggleMute() => IsMuted ^= true;

    private void SetRate(string value) => _mediaPlayer.SetRate(float.Parse(value));
}
