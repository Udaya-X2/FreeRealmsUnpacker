using Avalonia.Controls;
using Avalonia.Input;
using System;
using System.Threading.Tasks;
using UnpackerGui.ViewModels;
using UnpackerGui.Views;

namespace UnpackerGui.Services;

public class DialogService(Window window) : IDialogService
{
    private readonly Window _owner = window;

    public async Task ShowDialog(Window window, bool terminal = false)
        => await ShowDialog(_owner, window, terminal);

    public static async Task ShowDialog(Window owner, Window window, bool terminal = false)
    {
        window.KeyDown += (s, e) =>
        {
            if (e.Key == Key.Escape)
            {
                window.Close();
            }
        };
        await window.ShowDialog(owner);

        if (terminal)
        {
            owner.Close();
        }
    }

    public async Task ShowErrorDialog(Exception exception, bool terminal = false, bool unhandled = false)
        => await ShowErrorDialog(_owner, exception, terminal, unhandled);

    public static async Task ShowErrorDialog(Window owner, Exception exception, bool terminal = false, bool unhandled = false)
    {
        ErrorWindow errorWindow = new()
        {
            DataContext = new ErrorViewModel()
            {
                Exception = exception,
                Handled = !unhandled
            }
        };
        await ShowDialog(owner, errorWindow, terminal);
    }

    public async Task<bool> ShowConfirmDialog(ConfirmViewModel confirm, bool terminal = false)
        => await ShowConfirmDialog(_owner, confirm, terminal);

    public static async Task<bool> ShowConfirmDialog(Window owner, ConfirmViewModel confirm, bool terminal = false)
    {
        ConfirmWindow window = new() { DataContext = confirm };
        await ShowDialog(owner, window, terminal);
        return window.Confirmed;
    }
}
