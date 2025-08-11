using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using System;

namespace UnpackerGui.Controls;

/// <inheritdoc/>
/// <remarks>
/// Provides events to notify when the user has started/stopped seeking.
/// </remarks>
public class SeekSlider : Slider
{
    // Smallest value such that 1.0 + DoubleEpsilon != 1.0.
    private const double DoubleEpsilon = 2.2204460492503131e-16;

    private bool _isSeeking;
    private Track? _track;
    private Button? _decreaseButton;
    private Button? _increaseButton;
    private IDisposable? _decreaseButtonPressDispose;
    private IDisposable? _decreaseButtonReleaseDispose;
    private IDisposable? _increaseButtonSubscription;
    private IDisposable? _increaseButtonReleaseDispose;
    private IDisposable? _pointerMovedDispose;

    /// <summary>
    /// Defines the <see cref="SeekStarted"/> event.
    /// </summary>
    public static readonly RoutedEvent<RoutedEventArgs> SeekStartedEvent =
        RoutedEvent.Register<SeekSlider, RoutedEventArgs>(nameof(SeekStarted), RoutingStrategies.Bubble);

    /// <summary>
    /// Defines the <see cref="SeekCompleted"/> event.
    /// </summary>
    public static readonly RoutedEvent<RoutedEventArgs> SeekCompletedEvent =
        RoutedEvent.Register<SeekSlider, RoutedEventArgs>(nameof(SeekCompleted), RoutingStrategies.Bubble);

    /// <summary>
    /// Occurs when the user starts seeking the slider.
    /// </summary>
    public event EventHandler<RoutedEventArgs>? SeekStarted
    {
        add => AddHandler(SeekStartedEvent, value);
        remove => RemoveHandler(SeekStartedEvent, value);
    }

    /// <summary>
    /// Occurs when the user stops seeking the slider.
    /// </summary>
    public event EventHandler<RoutedEventArgs>? SeekCompleted
    {
        add => AddHandler(SeekCompletedEvent, value);
        remove => RemoveHandler(SeekCompletedEvent, value);
    }

    /// <inheritdoc/>
    protected override Type StyleKeyOverride => typeof(Slider);

    /// <inheritdoc/>
    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        _decreaseButtonPressDispose?.Dispose();
        _decreaseButtonReleaseDispose?.Dispose();
        _increaseButtonSubscription?.Dispose();
        _increaseButtonReleaseDispose?.Dispose();
        _pointerMovedDispose?.Dispose();

        _track = e.NameScope.Find<Track>("PART_Track");

        if (_track != null)
        {
            _track.IgnoreThumbDrag = true;

            _decreaseButton = e.NameScope.Find<Button>("PART_DecreaseButton");
            _increaseButton = e.NameScope.Find<Button>("PART_IncreaseButton");

            if (_decreaseButton != null)
            {
                _decreaseButtonPressDispose = _decreaseButton.AddDisposableHandler(PointerPressedEvent,
                                                                                   TrackPressed,
                                                                                   RoutingStrategies.Tunnel);
                _decreaseButtonReleaseDispose = _decreaseButton.AddDisposableHandler(PointerReleasedEvent,
                                                                                     TrackReleased,
                                                                                     RoutingStrategies.Tunnel);
            }

            if (_increaseButton != null)
            {
                _increaseButtonSubscription = _increaseButton.AddDisposableHandler(PointerPressedEvent,
                                                                                   TrackPressed,
                                                                                   RoutingStrategies.Tunnel);
                _increaseButtonReleaseDispose = _increaseButton.AddDisposableHandler(PointerReleasedEvent,
                                                                                     TrackReleased,
                                                                                     RoutingStrategies.Tunnel);
            }
        }

        _pointerMovedDispose = this.AddDisposableHandler(PointerMovedEvent, TrackMoved, RoutingStrategies.Tunnel);
    }

    private void TrackMoved(object? sender, PointerEventArgs e)
    {
        if (!IsEnabled)
        {
            OnSeekCompleted();
            return;
        }

        if (_isSeeking)
        {
            MoveToPoint(e.GetCurrentPoint(_track));
        }
    }

    private void TrackReleased(object? sender, PointerReleasedEventArgs e) => OnSeekCompleted();

    private void TrackPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            MoveToPoint(e.GetCurrentPoint(_track));
            OnSeekStarted();
        }
    }

    private void MoveToPoint(PointerPoint posOnTrack)
    {
        if (_track == null) return;

        bool orient = Orientation == Orientation.Horizontal;
        double thumbLength = (orient
            ? _track.Thumb?.Bounds.Width ?? 0.0
            : _track.Thumb?.Bounds.Height ?? 0.0) + double.Epsilon;
        double trackLength = (orient
            ? _track.Bounds.Width
            : _track.Bounds.Height) - thumbLength;
        double trackPos = orient ? posOnTrack.Position.X : posOnTrack.Position.Y;
        double logicalPos = double.Clamp((trackPos - thumbLength * 0.5) / trackLength, 0.0, 1.0);
        int invert = orient ?
            IsDirectionReversed ? 1 : 0 :
            IsDirectionReversed ? 0 : 1;
        double calcVal = Math.Abs(invert - logicalPos);
        double range = Maximum - Minimum;
        double finalValue = calcVal * range + Minimum;

        SetCurrentValue(ValueProperty, IsSnapToTickEnabled ? SnapToTick(finalValue) : finalValue);
    }

    /// <inheritdoc/>
    protected override void OnThumbDragStarted(VectorEventArgs e) => OnSeekStarted();

    /// <inheritdoc/>
    protected override void OnThumbDragCompleted(VectorEventArgs e) => OnSeekCompleted();

    /// <summary>
    /// Called when the user starts seeking the slider.
    /// </summary>
    protected void OnSeekStarted()
    {
        _isSeeking = true;
        RaiseEvent(new RoutedEventArgs(SeekStartedEvent));
    }

    /// <summary>
    /// Called when the user stops seeking the slider.
    /// </summary>
    protected void OnSeekCompleted()
    {
        _isSeeking = false;
        RaiseEvent(new RoutedEventArgs(SeekCompletedEvent));
    }

    /// <summary>
    /// Snap the input 'value' to the closest tick.
    /// </summary>
    /// <param name="value">Value that want to snap to closest Tick.</param>
    private double SnapToTick(double value)
    {
        if (IsSnapToTickEnabled)
        {
            double previous = Minimum;
            double next = Maximum;

            // This property is rarely set so let's try to avoid the GetValue
            AvaloniaList<double>? ticks = Ticks;

            // If ticks collection is available, use it.
            // Note that ticks may be unsorted.
            if (ticks != null && ticks.Count > 0)
            {
                foreach (double tick in ticks)
                {
                    if (AreClose(tick, value))
                    {
                        return value;
                    }

                    if (LessThan(tick, value) && GreaterThan(tick, previous))
                    {
                        previous = tick;
                    }
                    else if (GreaterThan(tick, value) && LessThan(tick, next))
                    {
                        next = tick;
                    }
                }
            }
            else if (GreaterThan(TickFrequency, 0.0))
            {
                previous = Minimum + Math.Round((value - Minimum) / TickFrequency) * TickFrequency;
                next = Math.Min(Maximum, previous + TickFrequency);
            }

            // Choose the closest value between previous and next. If tie, snap to 'next'.
            value = GreaterThanOrClose(value, (previous + next) * 0.5) ? next : previous;
        }

        return value;
    }

    /// <summary>
    /// Returns whether or not the first double is less than the second double. That is, whether
    /// or not the first is strictly less than *and* not within epsilon of the other number.
    /// </summary>
    /// <param name="x">The first double to compare.</param>
    /// <param name="y">The second double to compare.</param>
    private static bool LessThan(double x, double y) => (x < y) && !AreClose(x, y);

    /// <summary>
    /// Returns whether or not the first double is greater than the second double. That is, whether
    /// or not the first is strictly greater than *and* not within epsilon of the other number.
    /// </summary>
    /// <param name="x">The first double to compare.</param>
    /// <param name="y">The second double to compare.</param>
    private static bool GreaterThan(double x, double y) => (x > y) && !AreClose(x, y);

    /// <summary>
    /// Returns whether or not the first double is greater than or close to the second double. That
    /// is, whether or not the first is strictly greater than or within epsilon of the other number.
    /// </summary>
    /// <param name="x">The first double to compare.</param>
    /// <param name="y">The second double to compare.</param>
    private static bool GreaterThanOrClose(double x, double y) => (x > y) || AreClose(x, y);

    /// <summary>
    /// Returns whether or not two doubles are "close". That is, whether or not they are within epsilon of each other.
    /// </summary> 
    /// <param name="x">The first double to compare.</param>
    /// <param name="y">The second double to compare.</param>
    private static bool AreClose(double x, double y)
    {
        // In case they are Infinities (then epsilon check does not work).
        if (x == y) return true;

        double eps = (Math.Abs(x) + Math.Abs(y) + 10.0) * DoubleEpsilon;
        double delta = x - y;
        return -eps < delta && eps > delta;
    }
}
