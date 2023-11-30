using Avalonia.Threading;
using ReactiveUI;
using System;
using System.Diagnostics;
using System.Reactive;
using System.Reactive.Disposables;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
using UnpackerGui.Extensions;

namespace UnpackerGui.ViewModels;

public abstract class ProgressViewModel : ViewModelBase
{
    private int _value;
    private string _message = "";
    private string _elapsedTime = $@"{TimeSpan.Zero:hh\:mm\:ss}";
    private TaskStatus _taskStatus;

    /// <summary>
    /// Initializes a new instance of the <see cref="ProgressViewModel"/> class.
    /// </summary>
    public ProgressViewModel()
    {
        Command = ReactiveCommand.CreateFromTask(CommandTask);
    }

    /// <summary>
    /// Gets the maximum progress value.
    /// </summary>
    public abstract int Maximum { get; }

    /// <summary>
    /// Gets the title of the task.
    /// </summary>
    public abstract string Title { get; }

    /// <summary>
    /// Gets the command, which encapsulates the progress-based task.
    /// </summary>
    public ReactiveCommand<Unit, Unit> Command { get; }

    /// <summary>
    /// Gets or sets the progress value.
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
    /// Gets or sets the status of the task.
    /// </summary>
    public TaskStatus Status
    {
        get => _taskStatus;
        set => this.RaiseAndSetIfChanged(ref _taskStatus, value);
    }

    /// <summary>
    /// Increments the progress value.
    /// </summary>
    protected void Tick() => Value++;

    /// <summary>
    /// Represents the operation that will be performed upon command invocation.
    /// </summary>
    /// <returns>An operation that will be performed upon command invocation.</returns>
    protected abstract void CommandAction(CancellationToken token);

    /// <summary>
    /// Represents the asynchronous operation that will be performed upon command invocation.
    /// </summary>
    /// <returns>An asynchronous operation that will be performed upon command invocation.</returns>
    private Task CommandTask(CancellationToken token)
    {
        token.Register(() =>
        {
            if (!Status.IsCompleted())
            {
                Message = $"Stopping {Title}...";
            }
        });
        return Task.Run(() => TimedCommandAction(token), token)
                   .ContinueWith(x =>
                   {
                       Status = x.Status;

                       if (x.IsFaulted)
                       {
                           ExceptionDispatchInfo.Throw(x.Exception!.InnerException!);
                       }
                       if (x.IsCompletedSuccessfully)
                       {
                           Message = $"{Title} Complete";
                       }
                   }, CancellationToken.None);
    }

    /// <summary>
    /// Wraps the operation performed upon command invocation with a timer.
    /// </summary>
    private void TimedCommandAction(CancellationToken token)
    {
        if (token.IsCancellationRequested) token.ThrowIfCancellationRequested();

        using (Timer())
        {
            CommandAction(token);
        }
    }

    /// <summary>
    /// Creates a timer that updates the elapsed time.
    /// </summary>
    /// <returns>A disposable that, when disposed, stops the timer.</returns>
    private IDisposable Timer()
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
}
