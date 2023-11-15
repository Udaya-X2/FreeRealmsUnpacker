using ReactiveUI;
using System;
using System.Windows.Input;

namespace UnpackerGui.ViewModels;

public class ErrorViewModel : ViewModelBase
{
    public Exception Exception { get; }
    public ICommand ShowDetailsCommand { get; }

    private bool _showDetails;

    public ErrorViewModel(Exception exception)
    {
        Exception = exception;
        ShowDetailsCommand = ReactiveCommand.Create(() => ShowDetails ^= true);
    }

    public bool ShowDetails
    {
        get => _showDetails;
        set => this.RaiseAndSetIfChanged(ref _showDetails, value);
    }
}
