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

    public async Task ShowDialog(Window window) => await window.ShowDialog(_owner);
}
