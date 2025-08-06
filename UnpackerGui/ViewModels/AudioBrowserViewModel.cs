using AssetIO;
using Avalonia.Threading;
using ManagedBass;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Reactive;
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

    private readonly DispatcherTimer _dispatcherTimer;

    private byte[] _buffer;
    private int _handle;
    private bool _isPlaying;
    private int _volume;
    private int _mutedVolume;
    private double _position;
    private double _length;
    public bool _loop;
    private float _rate;
    private BassFlags _bassFlags;

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
        _buffer = [];
        _volume = 100;
        _mutedVolume = 100;
        _rate = 1f;
        _length = double.Epsilon;

        // Create a timer to update the media player.
        _dispatcherTimer = new DispatcherTimer(DispatcherPriority.Normal);
        _dispatcherTimer.Tick += UpdateMediaPlayer;
        _dispatcherTimer.Interval = TimeSpan.FromMilliseconds(100);

        if (!Bass.Init()) return;

        // Enable/disable the media player based on the validity of the stream handle.
        this.WhenAnyValue(x => x.Handle)
            .ToProperty(this, nameof(CanPlay));

        // Play the selected asset in the media player.
        this.WhenAnyValue(x => x.SelectedAsset)
            .Subscribe(PlayMedia);

        // Update the media player periodically while it is playing.
        this.WhenAnyValue(x => x.IsPlaying)
            .Subscribe(ToggleDispatcherTimer);

        // Update media player properties when requested.
        this.WhenAnyValue(x => x.Volume)
            .Subscribe(x => Bass.GlobalStreamVolume = x * 100);
        this.WhenAnyValue(x => x.Loop)
            .Subscribe(x => SetBassFlag(BassFlags.Loop, x));
    }

    /// <summary>
    /// Gets whether audio can be played.
    /// </summary>
    public bool CanPlay => Handle != 0;

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
    /// Gets or sets the playback position.
    /// </summary>
    public double Position
    {
        get => _position;
        set => ChannelPosition = this.RaiseAndSetIfChanged(ref _position, value);
    }

    /// <summary>
    /// Gets or sets the playback length.
    /// </summary>
    public double Length
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
    /// Sets the displayed playback position.
    /// </summary>
    private double DisplayPosition { set => this.RaiseAndSetIfChanged(ref _position, value, nameof(Position)); }

    /// <summary>
    /// Gets or sets the actual playback position.
    /// </summary>
    private double ChannelPosition
    {
        get => Bass.ChannelBytes2Seconds(Handle, Bass.ChannelGetPosition(Handle));
        set => Bass.ChannelSetPosition(Handle, Bass.ChannelSeconds2Bytes(Handle, value));
    }

    /// <summary>
    /// Gets or sets the audio stream handle.
    /// </summary>
    private int Handle
    {
        get => _handle;
        set => this.RaiseAndSetIfChanged(ref _handle, value);
    }

    /// <summary>
    /// Plays the specified asset in the media player.
    /// </summary>
    private void PlayMedia(AssetInfo? asset)
    {
        // Dispose of the previous audio stream, if necessary.
        if (CanPlay)
        {
            Bass.StreamFree(Handle);
            Handle = 0;
            DisplayPosition = 0.0;
            Length = double.Epsilon;
            IsPlaying = false;
        }

        if (asset == null) return;

        // Read the asset into the buffer.
        try
        {
            EnsureCapacity(asset.Size);
            using AssetReader reader = asset.AssetFile.OpenRead();
            reader.Read(asset, _buffer);
        }
        catch
        {
            return;
        }

        // Initialize the audio stream handle from the buffer data.
        Handle = Bass.CreateStream(_buffer, 0, asset.Size, _bassFlags);

        if (!CanPlay) return;

        // Play the audio in the media player.
        DisplayPosition = 0.0;
        Length = Bass.ChannelBytes2Seconds(Handle, Bass.ChannelGetLength(Handle));
        IsPlaying = true;
        Bass.ChannelPlay(Handle);
    }

    /// <summary>
    /// Ensures the buffer is at least the specified length.
    /// </summary>
    private void EnsureCapacity(uint value)
    {
        if (value <= _buffer.Length) return;
        if (value > Array.MaxLength) throw new OutOfMemoryException(SR.OutOfMemory_ArrayLength);

        _buffer = new byte[Math.Min((int)BitOperations.RoundUpToPowerOf2(value), Array.MaxLength)];
    }

    /// <summary>
    /// Callback function to update the media player at every interval.
    /// </summary>
    private void UpdateMediaPlayer(object? s, EventArgs e)
    {
        DisplayPosition = ChannelPosition;

        // Stop playing once the channel is no longer active.
        if (Bass.ChannelIsActive(Handle) == PlaybackState.Stopped)
        {
            DisplayPosition = Length;
            IsPlaying = false;
        }
    }

    /// <summary>
    /// Toggles whether to start/stop the dispatcher timer.
    /// </summary>
    private void ToggleDispatcherTimer(bool start)
    {
        if (start)
        {
            _dispatcherTimer.Start();
        }
        else
        {
            _dispatcherTimer.Stop();
        }
    }

    /// <summary>
    /// Toggles whether to play/pause the media.
    /// </summary>
    private void TogglePlayPause()
    {
        if (IsPlaying ^= true)
        {
            Bass.ChannelPlay(Handle);
        }
        else
        {
            Bass.ChannelPause(Handle);
        }

        DisplayPosition = ChannelPosition;
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
    /// Adds/removes the specified BASS flag.
    /// </summary>
    private void SetBassFlag(BassFlags flag, bool addFlag)
    {
        if (addFlag)
        {
            _bassFlags |= flag;
            Bass.ChannelAddFlag(Handle, flag);
        }
        else
        {
            _bassFlags &= ~flag;
            Bass.ChannelRemoveFlag(Handle, flag);
        }
    }
}
