using Avalonia.Controls;
using System;
using System.Threading.Tasks;
using UnpackerGui.ViewModels;
using UnpackerGui.Views;

namespace UnpackerGui.Services;

public class DialogService : IDialogService
{
    private readonly Window _owner;

    public DialogService(Window window)
    {
        _owner = window;
    }

    public async Task ShowDialog(Window window, bool terminal = false) => await ShowDialog(_owner, window, terminal);

    public async Task ShowDialog(Window owner, Window window, bool terminal = false)
    {
        owner.IsEnabled = false;
        await window.ShowDialog(owner);

        if (terminal)
        {
            owner.Close();
        }
        else
        {
            owner.IsEnabled = true;
        }
    }

    public async Task ShowErrorDialog(Exception exception, bool terminal = false)
        => await ShowErrorDialog(_owner, exception, terminal);

    public async Task ShowErrorDialog(Window owner, Exception exception, bool terminal = false)
    {
        ErrorWindow errorWindow = new()
        {
            DataContext = new ErrorViewModel(exception, true)
        };
        await ShowDialog(errorWindow, terminal);
    }
}
