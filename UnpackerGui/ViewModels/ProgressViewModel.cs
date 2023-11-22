using Avalonia.Threading;
using ReactiveUI;
using System;
using System.Diagnostics;
using System.Reactive;
using System.Reactive.Disposables;
using System.Threading;
using System.Threading.Tasks;

namespace UnpackerGui.ViewModels;

public abstract class ProgressViewModel : ViewModelBase
{
    private int _value;
    private string _message = "";
    private string _elapsedTime = $@"{TimeSpan.Zero:hh\:mm\:ss}";
    private bool _isComplete;

    public ProgressViewModel()
    {
        Command = ReactiveCommand.CreateFromTask(CommandTask);
    }

    /// <summary>
    /// Gets the maximum value of the progress bar.
    /// </summary>
    public abstract int Maximum { get; }

    /// <summary>
    /// Gets the command, which encapsulates the progress bar task.
    /// </summary>
    public ReactiveCommand<Unit, Unit> Command { get; }

    /// <summary>
    /// Gets or sets the current value of the progress bar.
    /// </summary>
    public int Value
    {
        get => _value;
        set => this.RaiseAndSetIfChanged(ref _value, value);
    }

    /// <summary>
    /// Gets or sets the progress message.
    /// </summary>
    public string Message
    {
        get => _message;
        set => this.RaiseAndSetIfChanged(ref _message, value);
    }

    /// <summary>
    /// Gets or sets the time that has passed since the start of the task.
    /// </summary>
    public string ElapsedTime
    {
        get => _elapsedTime;
        set => this.RaiseAndSetIfChanged(ref _elapsedTime, value);
    }

    /// <summary>
    /// Gets or sets whether the task is complete.
    /// </summary>
    public bool IsComplete
    {
        get => _isComplete;
        set => this.RaiseAndSetIfChanged(ref _isComplete, value);
    }

    /// <summary>
    /// Moves the progress bar by one tick.
    /// </summary>
    protected void Tick() => Value++;

    /// <summary>
    /// Creates a timer that updates the elapsed time.
    /// </summary>
    /// <returns>A disposable that, when disposed, stops the timer.</returns>
    protected IDisposable Timer()
    {
        Stopwatch stopwatch = Stopwatch.StartNew();
        DispatcherTimer dispatchTimer = new(DispatcherPriority.Normal);
        dispatchTimer.Tick += (s, e) => ElapsedTime = $@"{stopwatch.Elapsed:hh\:mm\:ss}";
        dispatchTimer.Interval = TimeSpan.FromMilliseconds(500);
        dispatchTimer.Start();
        return Disposable.Create(() =>
        {
            stopwatch.Stop();
            dispatchTimer.Stop();
        });
    }

    /// <summary>
    /// Represents the asynchronous operation that will be performed upon command invocation.
    /// </summary>
    /// <returns>An asynchronous operation that will be performed upon command invocation.</returns>
    protected abstract Task CommandTask(CancellationToken token);
}
