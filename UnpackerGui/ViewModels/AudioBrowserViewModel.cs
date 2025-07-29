using AssetIO;
using LibVLCSharp.Shared;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Reactive;
using System.Threading;
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
    private readonly Lazy<LibVLC> _libVLC;
    private readonly Lazy<Media> _media;
    private readonly Lock _lock;

    private MediaPlayer? _mediaPlayer;
    private bool _canPlay;
    private bool _isPlaying;
    private int _volume;
    private int _mutedVolume;
    private long _time;
    private long _length;
    public bool _loop;
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
        _libVLC = new Lazy<LibVLC>(() => new LibVLC());
        _media = new Lazy<Media>(() => new Media(LibVLC, new StreamMediaInput(_audioStream)));
        _lock = new Lock();
        _volume = 100;
        _mutedVolume = 100;
        _rate = 1f;
        _length = 1;

        // Play the selected asset in the media player.
        this.WhenAnyValue(x => x.SelectedAsset)
            .Subscribe(PlayMedia);

        // Update media player properties when requested.
        this.WhenAnyValue(x => x.Volume)
            .Subscribe(SetMediaVolume);
        this.WhenAnyValue(x => x.Rate)
            .Subscribe(x => _mediaPlayer?.SetRate(x));
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
        set => _mediaPlayer!.Time = this.RaiseAndSetIfChanged(ref _time, value);
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
    /// Gets or sets whether to loop audio playback.
    /// </summary>
    public bool Loop
    {
        get => _loop;
        set => this.RaiseAndSetIfChanged(ref _loop, value);
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
    /// Sets the displayed current playback time.
    /// </summary>
    private long DisplayTime
    {
        set => this.RaiseAndSetIfChanged(ref _time, value, nameof(Time));
    }

    /// <summary>
    /// Gets the LibVLC instance.
    /// </summary>
    private LibVLC LibVLC => _libVLC.Value;

    /// <summary>
    /// Gets the playable media.
    /// </summary>
    private Media Media => _media.Value;

    /// <summary>
    /// Initializes the media player.
    /// </summary>
    [MemberNotNull(nameof(_mediaPlayer))]
    private void InitializeMediaPlayer(bool play)
    {
        // Initialize the new media player.
        _mediaPlayer = new MediaPlayer(LibVLC) { Media = Media };
        MediaPlayer mediaPlayer = _mediaPlayer;

        if (_volume != 100) _mediaPlayer.Volume = _volume;
        if (_rate != 1f) _mediaPlayer.SetRate(_rate);

        // Update display properties based on the media player status.
        _mediaPlayer.EndReached += OnEndReached;
        _mediaPlayer.TimeChanged += OnTimeChanged;
        _mediaPlayer.LengthChanged += OnLengthChanged;
        _mediaPlayer.EncounteredError += OnEncounteredError;
        CanPlay = true;

        // Play the media, if requested.
        if (play) IsPlaying = _mediaPlayer.Play();
    }

    /// <summary>
    /// Disposes of the current media player.
    /// </summary>
    private void DisposeMediaPlayer(bool reset = false)
    {
        // Prevent concurrent dispose from VLC callback/UI thread.
        lock (_lock)
        {
            if (_mediaPlayer == null) return;

            // Reset display properties.
            IsPlaying = reset && Loop;
            DisplayTime = 0;

            if (!reset)
            {
                CanPlay = false;
                Length = 1;
            }

            // Dispose on a different thread to avoid hanging on the UI thread.
            MediaPlayer mediaPlayer = _mediaPlayer;
            mediaPlayer.EndReached -= OnEndReached;
            mediaPlayer.TimeChanged -= OnTimeChanged;
            mediaPlayer.LengthChanged -= OnLengthChanged;
            mediaPlayer.EncounteredError -= OnEncounteredError;
            Task.Run(mediaPlayer.Dispose);

            // Reset the media player with the same media, if requested.
            if (reset)
            {
                InitializeMediaPlayer(play: Loop);
            }
            else
            {
                _mediaPlayer = null;
            }
        }
    }

    /// <summary>
    /// Plays the specified asset in the media player.
    /// </summary>
    private void PlayMedia(AssetInfo? asset)
    {
        DisposeMediaPlayer();

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

        InitializeMediaPlayer(play: true);
    }

    /// <summary>
    /// Toggles whether to play/pause the media.
    /// </summary>
    private void TogglePlayPause()
    {
        if (IsPlaying ^= true)
        {
            _mediaPlayer!.Play();
        }
        else
        {
            _mediaPlayer!.Pause();
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

    /// <summary>
    /// Sets the media player volume to the specified value.
    /// </summary>
    private void SetMediaVolume(int value)
    {
        if (_mediaPlayer != null)
        {
            _mediaPlayer.Volume = value;
        }
    }

    /// <summary>
    /// Event handler for <see cref="MediaPlayer.EndReached"/>.
    /// </summary>
    private void OnEndReached(object? s, EventArgs e) => DisposeMediaPlayer(reset: true);

    /// <summary>
    /// Event handler for <see cref="MediaPlayer.TimeChanged"/>.
    /// </summary>
    private void OnTimeChanged(object? s, MediaPlayerTimeChangedEventArgs e) => DisplayTime = e.Time;

    /// <summary>
    /// Event handler for <see cref="MediaPlayer.LengthChanged"/>.
    /// </summary>
    private void OnLengthChanged(object? s, MediaPlayerLengthChangedEventArgs e) => Length = e.Length;

    /// <summary>
    /// Event handler for <see cref="MediaPlayer.EncounteredError"/>.
    /// </summary>
    private void OnEncounteredError(object? s, EventArgs e) => DisposeMediaPlayer();
}
