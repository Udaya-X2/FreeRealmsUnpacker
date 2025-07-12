using ReactiveUI;
using System;
using System.Reactive;

namespace UnpackerGui.ViewModels;

public class ErrorViewModel : ViewModelBase
{
    public required Exception Exception { get; init; }
    public required bool Handled { get; init; }

    public ReactiveCommand<Unit, bool> ShowDetailsCommand { get; }

    private bool _showDetails;

    public ErrorViewModel()
    {
        ShowDetailsCommand = ReactiveCommand.Create(() => ShowDetails ^= true);
    }

    public bool ShowDetails
    {
        get => _showDetails;
        set => this.RaiseAndSetIfChanged(ref _showDetails, value);
    }

    public string Message => Handled
        ? $"An error has occurred.\n\n{Exception.Message}"
        : $"A fatal error has occurred.\n\n{Exception.Message}";

    public string DetailsMessage => $"{Exception}\n";
}
