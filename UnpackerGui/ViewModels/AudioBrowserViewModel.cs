using AssetIO;
using LibVLCSharp.Shared;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Reactive;
using System.Threading.Tasks;
using UnpackerGui.Collections;
using UnpackerGui.Extensions;
using UnpackerGui.Models;

namespace UnpackerGui.ViewModels;

public class AudioBrowserViewModel : AssetBrowserViewModel
{
    /// <summary>
    /// Gets the available audio playback speeds.
    /// </summary>
    public static IReadOnlyList<float> Speeds { get; } = [0.25f, 0.5f, 0.75f, 1f, 1.25f, 1.5f, 1.75f, 2.0f];

    /// <inheritdoc/>
    public override FilteredReactiveCollection<AssetInfo> Assets { get; }

    public ReactiveCommand<Unit, Unit> TogglePlayPauseCommand { get; }
    public ReactiveCommand<Unit, Unit> ToggleMuteCommand { get; }
    public ReactiveCommand<float, Unit> SetRateCommand { get; }

    private readonly MemoryStream _audioStream;
    private readonly LibVLC _libVlc;

    private MediaPlayer _mediaPlayer;
    private bool _canPlay;
    private bool _isPlaying;
    private int _volume;
    private int _mutedVolume;
    private long _time;
    private long _length;
    private float _rate;

    /// <summary>
    /// Initializes a new instance of the <see cref="AudioBrowserViewModel"/> class.
    /// </summary>
    public AudioBrowserViewModel(ReadOnlyReactiveCollection<AssetInfo> assets)
    {
        // Initialize each command.
        TogglePlayPauseCommand = ReactiveCommand.Create(TogglePlayPause);
        ToggleMuteCommand = ReactiveCommand.Create(ToggleMute);
        SetRateCommand = ReactiveCommand.Create<float>(x => Rate = x);

        // Filter the audio assets from the asset browser.
        Assets = assets.Filter(x => x.IsAudio);
        OnAssetsInitialized(x => x.ShowAudioBrowser);

        // Initialize media player resources.
        _audioStream = new MemoryStream();
        _libVlc = new LibVLC();
        _volume = 100;
        _mutedVolume = 100;
        _rate = 1f;
        ResetMediaPlayer();

        // Play the selected asset in the media player.
        this.WhenAnyValue(x => x.SelectedAsset)
            .Subscribe(PlayMedia);

        // Update media player properties when requested.
        this.WhenAnyValue(x => x.Volume)
            .Subscribe(x => _mediaPlayer.Volume = x);
        this.WhenAnyValue(x => x.Rate)
            .Subscribe(x => _mediaPlayer.SetRate(x));
    }

    /// <summary>
    /// Gets or sets whether audio can be played.
    /// </summary>
    public bool CanPlay
    {
        get => _canPlay;
        set => this.RaiseAndSetIfChanged(ref _canPlay, value);
    }

    /// <summary>
    /// Gets or sets whether the audio is playing.
    /// </summary>
    public bool IsPlaying
    {
        get => _isPlaying;
        set => this.RaiseAndSetIfChanged(ref _isPlaying, value);
    }

    /// <summary>
    /// Gets or sets the volume.
    /// </summary>
    public int Volume
    {
        get => _volume;
        set => this.RaiseAndSetIfChanged(ref _volume, value);
    }

    /// <summary>
    /// Gets or sets the current playback time.
    /// </summary>
    public long Time
    {
        get => _time;
        set => _mediaPlayer.Time = this.RaiseAndSetIfChanged(ref _time, value);
    }

    /// <summary>
    /// Sets the displayed current playback time.
    /// </summary>
    private long DisplayTime
    {
        set => this.RaiseAndSetIfChanged(ref _time, value, nameof(Time));
    }

    /// <summary>
    /// Gets the playback duration (in ms).
    /// </summary>
    public long Length
    {
        get => _length;
        set => this.RaiseAndSetIfChanged(ref _length, value);
    }

    /// <summary>
    /// Gets or sets the playback speed.
    /// </summary>
    public float Rate
    {
        get => _rate;
        set => this.RaiseAndSetIfChanged(ref _rate, value);
    }

    /// <summary>
    /// Resets the state of the media player.
    /// </summary>
    [MemberNotNull(nameof(_mediaPlayer))]
    private void ResetMediaPlayer(long? length = null)
    {
        // Dispose of the current media player.
        Task.Run(() => _mediaPlayer?.Dispose());

        // Initialize the new media player.
        _mediaPlayer = new MediaPlayer(_libVlc) { Media = new Media(_libVlc, new StreamMediaInput(_audioStream)) };

        if (_volume != 100) _mediaPlayer.Volume = _volume;
        if (_rate != 1f) _mediaPlayer.SetRate(_rate);

        // Update display properties based on the media player status.
        _mediaPlayer.TimeChanged += (s, e) => DisplayTime = e.Time;
        _mediaPlayer.EndReached += (s, e) => ResetMediaPlayer(_length);
        _mediaPlayer.LengthChanged += (s, e) => Length = e.Length;
        _mediaPlayer.EncounteredError += (s, e) => CanPlay = IsPlaying = false;

        // Reset display properties.
        CanPlay = length != null;
        IsPlaying = false;
        DisplayTime = 0;
        Length = length ?? 1;
    }

    /// <summary>
    /// Plays the specified asset in the media player.
    /// </summary>
    private void PlayMedia(AssetInfo? asset)
    {
        ResetMediaPlayer();

        if (asset == null) return;

        try
        {
            using AssetReader reader = asset.AssetFile.OpenRead();
            _audioStream.SetLength(0);
            reader.CopyTo(asset, _audioStream);
        }
        catch
        {
            return;
        }

        CanPlay = IsPlaying = _mediaPlayer.Play();
    }

    /// <summary>
    /// Toggles whether to play/pause the media.
    /// </summary>
    private void TogglePlayPause()
    {
        if (IsPlaying ^= true)
        {
            _mediaPlayer.Play();
        }
        else
        {
            _mediaPlayer.Pause();
        }
    }

    /// <summary>
    /// Toggles whether to mute/unmute the media.
    /// </summary>
    private void ToggleMute()
    {
        if (Volume == 0)
        {
            Volume = _mutedVolume;
        }
        else
        {
            _mutedVolume = Volume;
            Volume = 0;
        }
    }
}
