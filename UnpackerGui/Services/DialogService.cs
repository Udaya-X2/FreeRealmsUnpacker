using Avalonia.Controls;
using System.Threading.Tasks;

namespace UnpackerGui.Services;

public class DialogService : IDialogService
{
    private readonly Window _owner;

    public DialogService(Window window)
    {
        _owner = window;
    }

    public async Task ShowDialog(Window window) => await ShowDialog(_owner, window);

    public async Task ShowDialog(Window owner, Window window)
    {
        _owner.IsEnabled = false;
        await window.ShowDialog(owner);
        _owner.IsEnabled = true;
    }

    public async Task ShowTerminalDialog(Window window) => await ShowTerminalDialog(_owner, window);

    public async Task ShowTerminalDialog(Window owner, Window window)
    {
        _owner.IsEnabled = false;
        await window.ShowDialog(owner);
        _owner.Close();
    }
}
