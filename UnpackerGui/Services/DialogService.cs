using Avalonia.Controls;
using System;
using System.Threading.Tasks;
using UnpackerGui.ViewModels;
using UnpackerGui.Views;

namespace UnpackerGui.Services;

public class DialogService(Window window) : IDialogService
{
    private readonly Window _owner = window;

    public async Task ShowDialog(Window window, bool terminal = false) => await ShowDialog(_owner, window, terminal);

    public async Task ShowDialog(Window owner, Window window, bool terminal = false)
    {
        await window.ShowDialog(owner);

        if (terminal)
        {
            owner.Close();
        }
    }

    public async Task ShowErrorDialog(Exception exception, bool terminal = false, bool unhandled = false)
        => await ShowErrorDialog(_owner, exception, terminal, unhandled);

    public async Task ShowErrorDialog(Window owner, Exception exception, bool terminal = false, bool unhandled = false)
    {
        ErrorWindow errorWindow = new()
        {
            DataContext = new ErrorViewModel(exception, !unhandled)
        };
        await ShowDialog(errorWindow, terminal);
    }
}
