using ReactiveUI;
using System;
using System.Windows.Input;

namespace UnpackerGui.ViewModels;

public class ErrorViewModel : ViewModelBase
{
    public Exception Exception { get; }
    public bool Handled { get; }

    public ICommand ShowDetailsCommand { get; }

    private bool _showDetails;

    public ErrorViewModel(Exception exception, bool handled)
    {
        Exception = exception;
        Handled = handled;
        ShowDetailsCommand = ReactiveCommand.Create(() => ShowDetails ^= true);
    }

    public bool ShowDetails
    {
        get => _showDetails;
        set => this.RaiseAndSetIfChanged(ref _showDetails, value);
    }

    public string Message => Handled
        ? $"A handled exception has occurred.{Environment.NewLine}{Environment.NewLine}{Exception.Message}"
        : $"An unhandled exception has occurred.{Environment.NewLine}{Environment.NewLine}{Exception.Message}";

    public string DetailsMessage => $"{Exception}{Environment.NewLine}";
}
