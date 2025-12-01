using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using System;
using System.Reactive.Disposables;
using System.Threading.Tasks;
using UnpackerGui.Extensions;
using UnpackerGui.ViewModels;
using UnpackerGui.Views;

namespace UnpackerGui.Services;

public class DialogService(Window window) : IDialogService
{
    private readonly Window _owner = window;

    public Task ShowDialog(Window window, bool terminal = false)
        => ShowDialog(_owner, window, terminal);

    public static Task ShowDialog(Window owner, Window window, bool terminal = false)
        => ShowDialog<object>(owner, window, terminal);

    public Task<T> ShowDialog<T>(Window window, bool terminal = false)
        => ShowDialog<T>(_owner, window, terminal);

    public static async Task<T> ShowDialog<T>(Window owner, Window window, bool terminal = false)
    {
        using CompositeDisposable _ =
        [
            owner.Disable(),
            window.AddDisposableHandler(InputElement.KeyDownEvent, (s, e) =>
            {
                if (e.Key == Key.Escape)
                {
                    window.Close();
                }
            })
        ];
        T result = await window.ShowDialog<T>(owner);

        if (terminal)
        {
            owner.Close();
        }

        return result;
    }

    public Task ShowErrorDialog(Exception exception, bool terminal = false, bool unhandled = false)
        => ShowErrorDialog(_owner, exception, terminal, unhandled);

    public static Task ShowErrorDialog(Window owner,
                                       Exception exception,
                                       bool terminal = false,
                                       bool unhandled = false)
    {
        ErrorWindow errorWindow = new()
        {
            DataContext = new ErrorViewModel()
            {
                Exception = exception,
                Handled = !unhandled
            }
        };
        return ShowDialog(owner, errorWindow, terminal);
    }

    public Task<bool> ShowConfirmDialog(ConfirmViewModel confirm, bool terminal = false)
        => ShowConfirmDialog(_owner, confirm, terminal);

    public static Task<bool> ShowConfirmDialog(Window owner, ConfirmViewModel confirm, bool terminal = false)
    {
        ConfirmWindow window = new() { DataContext = confirm };
        return ShowDialog<bool>(owner, window, terminal);
    }

    public Task<string> ShowInputDialog(InputViewModel input, bool terminal = false)
        => ShowInputDialog(_owner, input, terminal);

    public static Task<string> ShowInputDialog(Window owner, InputViewModel input, bool terminal = false)
    {
        InputWindow window = new() { DataContext = input };
        return ShowDialog<string>(owner, window, terminal);
    }
}
