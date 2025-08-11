using AssetIO;
using Avalonia.Controls;
using Avalonia.Threading;
using ManagedBass;
using ManagedBass.Fx;
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
    public ReactiveCommand<float, Unit> SetSpeedCommand { get; }
    public ReactiveCommand<double, Unit> SeekCommand { get; }

    private readonly DispatcherTimer _dispatcherTimer;

    private bool _isPlaying;
    private bool _isSeeking;
    private bool _wasPlaying;
    private double _position;
    private double _length;
    private float _speed;
    private int _handle;
    private int _mutedVolume;
    private BassFlags _bassFlags;
    private byte[] _buffer;

    /// <summary>
    /// Initializes a new instance of the <see cref="AudioBrowserViewModel"/> class.
    /// </summary>
    public AudioBrowserViewModel(ReadOnlyReactiveCollection<AssetInfo> assets)
    {
        // Initialize each command.
        TogglePlayPauseCommand = ReactiveCommand.Create(TogglePlayPause);
        ToggleMuteCommand = ReactiveCommand.Create(ToggleMute);
        SetSpeedCommand = ReactiveCommand.Create<float>(x => Speed = x);
        SeekCommand = ReactiveCommand.Create<double>(x => Position = ChannelPosition + x);

        // Filter the audio assets from the asset browser.
        Assets = assets.Filter(x => x.IsAudio);
        OnAssetsInitialized(x => x.ShowAudioBrowser);

        // Initialize media player resources.
        _isPlaying = false;
        _isSeeking = false;
        _wasPlaying = false;
        _position = 0.0;
        _length = double.Epsilon;
        _speed = 1f;
        _handle = 0;
        _mutedVolume = Settings.Volume;
        _bassFlags = BassFlags.FxFreeSource;
        _buffer = [];

        // Create a timer to update the media player.
        _dispatcherTimer = new DispatcherTimer(DispatcherPriority.Normal);
        _dispatcherTimer.Tick += UpdateMediaPlayer;
        _dispatcherTimer.Interval = TimeSpan.FromMilliseconds(100);

        // Initialize the output device and load BASS libraries.
        if (!Bass.Init() && !Design.IsDesignMode) return;
        _ = BassFx.Version;

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
        Settings.WhenAnyValue(x => x.Volume)
                .Subscribe(x => Bass.GlobalStreamVolume = 100 * x);
        Settings.WhenAnyValue(x => x.Loop)
                .Subscribe(x => SetBassFlag(BassFlags.Loop, x));
        this.WhenAnyValue(x => x.IsSeeking)
            .Subscribe(x => PauseWhenSeeking());
        this.WhenAnyValue(x => x.Speed)
            .Subscribe(x => Bass.ChannelSetAttribute(Handle, ChannelAttribute.Tempo, Tempo));
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
    /// Gets or sets whether the media player is seeking.
    /// </summary>
    public bool IsSeeking
    {
        get => _isSeeking;
        set => this.RaiseAndSetIfChanged(ref _isSeeking, value);
    }

    /// <summary>
    /// Gets or sets the playback position.
    /// </summary>
    /// <remarks>
    /// Source: <see href="https://www.un4seen.com/forum/?msg=96966"/>
    /// </remarks>
    public double Position
    {
        get => _position;
        set
        {
            ChannelPosition = value;

            // If an error occurred due to seeking out of bounds, clamp the seek.
            if (Bass.LastError == Errors.Position)
            {
                // If the position is negative, seek to the start.
                if (value < 0.0)
                {
                    ChannelPosition = 0.0;
                    value = 0.0;
                }
                // If the position is positive, seek to the end.
                else
                {
                    Bass.ChannelSetPosition(Handle, Bass.ChannelGetLength(Handle), PositionFlags.DecodeTo);
                    value = Length;
                }
            }

            this.RaiseAndSetIfChanged(ref _position, value);
        }
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
    /// Gets or sets the playback speed.
    /// </summary>
    public float Speed
    {
        get => _speed;
        set => this.RaiseAndSetIfChanged(ref _speed, value);
    }

    /// <summary>
    /// Gets the playback tempo.
    /// </summary>
    public float Tempo => 100f * (Speed - 1f);

    /// <summary>
    /// Gets or sets the audio stream handle.
    /// </summary>
    private int Handle
    {
        get => _handle;
        set => this.RaiseAndSetIfChanged(ref _handle, value);
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
    /// Plays the specified asset in the media player.
    /// </summary>
    private void PlayMedia(AssetInfo? asset)
    {
        // Dispose of the previous audio stream handle, if necessary.
        DisposeHandle();

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
        if (!CreateHandle(asset.Size)) return;

        // Update the playback length.
        Length = Bass.ChannelBytes2Seconds(Handle, Bass.ChannelGetLength(Handle));

        // Play the audio in the media player.
        if (IsPlaying = Settings.Autoplay)
        {
            Bass.ChannelPlay(Handle);
        }
    }

    /// <summary>
    /// Frees the audio stream handle and resets the media player.
    /// </summary>
    private void DisposeHandle()
    {
        if (!CanPlay) return;

        Bass.StreamFree(Handle);
        Handle = 0;
        DisplayPosition = 0.0;
        Length = double.Epsilon;
        IsPlaying = false;
        IsSeeking = false;
        _wasPlaying = false;
    }

    /// <summary>
    /// Creates a new audio stream handle from the buffer data.
    /// </summary>
    private bool CreateHandle(long length)
    {
        int handle;

        if ((handle = Bass.CreateStream(_buffer, 0, length, BassFlags.Decode)) == 0) return false;
        if ((handle = BassFx.TempoCreate(handle, _bassFlags)) == 0) return false;

        Bass.ChannelSetAttribute(handle, ChannelAttribute.Tempo, Tempo);
        Handle = handle;
        return true;
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
        if (!CanPlay || IsSeeking) return;

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
    /// Toggles whether to mute/unmute the volume.
    /// </summary>
    private void ToggleMute()
    {
        if (Settings.Volume == 0)
        {
            Settings.Volume = _mutedVolume;
        }
        else
        {
            _mutedVolume = Settings.Volume;
            Settings.Volume = 0;
        }
    }

    /// <summary>
    /// Pauses the media while seeking and resumes when no longer seeking.
    /// </summary>
    private void PauseWhenSeeking()
    {
        if (!CanPlay) return;

        if (IsPlaying && IsSeeking)
        {
            IsPlaying = false;
            Bass.ChannelPause(Handle);
            _wasPlaying = true;
        }
        else if (_wasPlaying && !IsPlaying && !IsSeeking)
        {
            IsPlaying = true;
            Bass.ChannelPlay(Handle);
            _wasPlaying = false;
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
